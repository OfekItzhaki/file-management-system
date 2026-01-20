using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace FileManagementSystem.Application.Services;

public class UploadDestinationResolver
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadDestinationResolver> _logger;

    public UploadDestinationResolver(IUnitOfWork unitOfWork, ILogger<UploadDestinationResolver> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Folder> ResolveDestinationFolderAsync(Guid? destinationFolderId, CancellationToken cancellationToken)
    {
        Folder? targetFolder = null;

        // If a folder ID is provided, use that folder
        if (destinationFolderId.HasValue)
        {
            targetFolder = await _unitOfWork.Folders.GetByIdAsync(destinationFolderId.Value, cancellationToken);
            if (targetFolder == null)
            {
                _logger.LogWarning("Destination folder not found: {FolderId}, using default folder", destinationFolderId.Value);
            }
        }

        // If no folder specified or folder not found, use/create "Default" folder
        if (targetFolder == null)
        {
            targetFolder = await GetOrCreateDefaultFolderAsync(cancellationToken);
        }

        return targetFolder;
    }

    private async Task<Folder> GetOrCreateDefaultFolderAsync(CancellationToken cancellationToken)
    {
        var storageBasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FileManagementSystem",
            "Storage"
        );
        var defaultFolderPath = Path.Combine(storageBasePath, "Default");

        try
        {
            var folder = await _unitOfWork.Folders.GetOrCreateByPathAsync(defaultFolderPath, cancellationToken);
            _logger.LogInformation("Using default folder for upload: {FolderPath}", defaultFolderPath);
            return folder;
        }
        catch (InvalidOperationException)
        {
            // If default folder creation fails, create it manually
            var folder = new Folder
            {
                Name = "Default",
                Path = defaultFolderPath,
                ParentFolderId = null,
                CreatedDate = DateTime.UtcNow
            };
            await _unitOfWork.Folders.AddAsync(folder, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created default folder: {FolderPath}", defaultFolderPath);
            return folder;
        }
    }
}
