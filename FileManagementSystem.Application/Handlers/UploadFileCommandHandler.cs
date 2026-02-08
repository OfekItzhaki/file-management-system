using MediatR;
using Microsoft.Extensions.Logging;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Application.Services;
using FileManagementSystem.Application.Utilities;
using FileManagementSystem.Domain.Entities;
using FileManagementSystem.Domain.Exceptions;

namespace FileManagementSystem.Application.Handlers;

public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, UploadFileResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;
    private readonly IMetadataService _metadataService;
    private readonly UploadDestinationResolver _destinationResolver;
    private readonly ILogger<UploadFileCommandHandler> _logger;
    
    public UploadFileCommandHandler(
        IUnitOfWork unitOfWork,
        IStorageService storageService,
        IMetadataService metadataService,
        UploadDestinationResolver destinationResolver,
        ILogger<UploadFileCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _metadataService = metadataService;
        _destinationResolver = destinationResolver;
        _logger = logger;
    }
    
    public async Task<UploadFileResult> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Uploading file: {SourcePath}", request.SourcePath);
        
        // Validate and normalize source path
        var normalizedSourcePath = Path.GetFullPath(request.SourcePath);
        
        // Basic path validation
        if (request.SourcePath.Contains("..") || request.SourcePath.Contains("~"))
        {
            _logger.LogWarning("Path traversal attempt detected: {Path}", request.SourcePath);
            throw new UnauthorizedAccessException($"Invalid file path: {request.SourcePath}");
        }
        
        if (!File.Exists(normalizedSourcePath))
        {
            throw new FileNotFoundException($"Source file not found: {normalizedSourcePath}");
        }
        
        // Check if file already exists in database
        var existingFile = await _unitOfWork.Files.FindAsync(
            f => f.Path == normalizedSourcePath, cancellationToken);
        
        if (existingFile.Any())
        {
            _logger.LogWarning("File already exists in database: {FilePath}", normalizedSourcePath);
            return new UploadFileResult(existingFile.First().Id, true, normalizedSourcePath);
        }
        
        // Compute hash to check for duplicates
        var hash = await _storageService.ComputeHashAsync(normalizedSourcePath, cancellationToken);
        var hashHex = Convert.ToHexString(hash);
        var duplicate = await _unitOfWork.Files.GetByHashAsync(hash, cancellationToken);
        
        if (duplicate != null)
        {
            _logger.LogWarning("Duplicate file detected by hash: {FilePath} (existing: {ExistingPath})", 
                normalizedSourcePath, duplicate.Path);
            throw new FileDuplicateException(normalizedSourcePath, hash);
        }
        
        // Resolve destination folder
        var targetFolder = await _destinationResolver.ResolveDestinationFolderAsync(request.DestinationFolderId, cancellationToken);
        
        // Build destination path using the target folder
        var originalFileName = request.OriginalFileName;
        var fileName = Path.GetFileName(originalFileName);
        var destinationPath = Path.Combine(targetFolder.Path, fileName);
        
        // Ensure the directory exists
        Directory.CreateDirectory(targetFolder.Path);
        
        // Always copy file to managed storage location
        var finalPath = await _storageService.SaveFileAsync(normalizedSourcePath, destinationPath, cancellationToken);
        
        // Determine file size and compression state
        long actualSize;
        bool isActuallyCompressed;

        if (Uri.TryCreate(finalPath, UriKind.Absolute, out var uriResult) && 
            (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
        {
            // Cloud storage - use original size
            actualSize = new FileInfo(normalizedSourcePath).Length;
            isActuallyCompressed = false; 
        }
        else
        {
            // Local storage - use the actual stored file size
            var storedFileInfo = new FileInfo(finalPath);
            actualSize = storedFileInfo.Length;
            isActuallyCompressed = finalPath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
        }
        
        // Extract metadata for photos (need to decompress temporarily or read from original)
        // For now, we'll extract metadata from the original source file before compression
        var isPhoto = await _metadataService.IsPhotoFileAsync(normalizedSourcePath, cancellationToken);
        PhotoMetadata? photoMetadata = null;
        
        if (isPhoto)
        {
            photoMetadata = await _metadataService.ExtractPhotoMetadataAsync(normalizedSourcePath, cancellationToken);
        }
        
        // Create file item
        var fileItem = new FileItem
        {
            Path = finalPath, // Store actual storage path or URL
            FileName = originalFileName, // Store original filename for display
            Hash = hash,
            HashHex = hashHex,
            Size = actualSize, // Store actual disk/cloud usage
            IsCompressed = isActuallyCompressed,
            MimeType = MimeTypeHelper.GetMimeType(originalFileName), // Use original filename for MIME type
            IsPhoto = isPhoto,
            FolderId = targetFolder.Id,
            CreatedDate = DateTime.UtcNow,
            PhotoDateTaken = photoMetadata?.DateTaken,
            CameraMake = photoMetadata?.CameraMake,
            CameraModel = photoMetadata?.CameraModel,
            Latitude = photoMetadata?.Latitude,
            Longitude = photoMetadata?.Longitude
        };
        
        await _unitOfWork.Files.AddAsync(fileItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("File uploaded successfully: {FilePath} (ID: {FileId})", 
            finalPath, fileItem.Id);
        
        return new UploadFileResult(fileItem.Id, false, finalPath);
    }
    
}
