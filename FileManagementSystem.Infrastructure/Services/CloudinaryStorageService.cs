using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FileManagementSystem.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FileManagementSystem.Infrastructure.Services;

public class CloudinaryStorageService : IStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly string _rootFolder;
    private readonly ILogger<CloudinaryStorageService> _logger;

    public CloudinaryStorageService(IConfiguration configuration, ILogger<CloudinaryStorageService> logger)
    {
        _logger = logger;
        _rootFolder = configuration["CloudinarySettings:RootFolder"] ?? "Horizon_FMS";

        var cloudName = configuration["CloudinarySettings:CloudName"];
        var apiKey = configuration["CloudinarySettings:ApiKey"];
        var apiSecret = configuration["CloudinarySettings:ApiSecret"];

        if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            _logger.LogWarning("Cloudinary settings are missing. Service might fail if used.");
            
            // Initialise with empty account to avoid null refs if resolving but not using
            _cloudinary = new Cloudinary();
        }
        else
        {
            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }
    }

    public async Task<string> SaveFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading to Cloudinary: {SourcePath}", sourcePath);
        
        var folderPath = Path.GetDirectoryName(destinationPath)?.Replace("\\", "/") ?? "";
        var finalFolder = string.IsNullOrEmpty(_rootFolder) 
            ? folderPath 
            : string.IsNullOrEmpty(folderPath) ? _rootFolder : $"{_rootFolder}/{folderPath}";

        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(sourcePath),
            PublicId = Path.GetFileNameWithoutExtension(destinationPath),
            Folder = finalFolder,
            UseFilename = true,
            UniqueFilename = true
        };

        // Use RawUpload to avoid type mismatch issues with generic UploadAsync in some SDK versions
        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            _logger.LogError("Cloudinary upload failed: {Error}", uploadResult.Error.Message);
            throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");
        }

        _logger.LogInformation("Cloudinary upload successful: {Url}", uploadResult.SecureUrl);
        return uploadResult.SecureUrl.ToString();
    }

    public async Task<FileData> ReadFileAsync(string filePath, bool isCompressed, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Downloading from Cloudinary: {Url}", filePath);
        
        using var client = new HttpClient();
        var response = await client.GetAsync(filePath, cancellationToken);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        
        return new FileData(content, Path.GetFileName(filePath), false);
    }

    public async Task<bool> DeleteFileAsync(string filePath, bool moveToRecycleBin = true, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting from Cloudinary: {Url}", filePath);
        
        var publicId = ExtractPublicIdFromUrl(filePath);
        var delParams = new DeletionParams(publicId) { ResourceType = ResourceType.Raw };
        var result = await _cloudinary.DestroyAsync(delParams);
        
        return result.Result == "ok";
    }

    public async Task<string> GenerateThumbnailAsync(string imagePath, int maxWidth, int maxHeight, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        _logger.LogDebug("Generating Cloudinary thumbnail URL for: {Url}", imagePath);
        
        var publicId = ExtractPublicIdFromUrl(imagePath);
        
        // Use Cloudinary image transformations
        var url = _cloudinary.Api.UrlImgUp
            .Transform(new Transformation().Width(maxWidth).Height(maxHeight).Crop("limit"))
            .BuildUrl(publicId);
            
        return url;
    }

    public async Task<byte[]> ComputeHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // Check if this is a local file path or a Cloudinary URL
        if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri) && 
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            // It's a URL - download and compute hash
            var fileData = await ReadFileAsync(filePath, false, cancellationToken);
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            return sha256.ComputeHash(fileData.Content);
        }
        else
        {
            // It's a local file path - read directly from disk with shared access
            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            return await sha256.ComputeHashAsync(fileStream, cancellationToken);
        }
    }

    private string ExtractPublicIdFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;
        if (!url.Contains("cloudinary.com")) return url;
        
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            
            // Path looks like /cloudname/raw/upload/v12345/folder/publicid.ext
            // We want everything after /upload/v[number]/
            
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var uploadIdx = -1;
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == "upload")
                {
                    uploadIdx = i;
                    break;
                }
            }
            
            if (uploadIdx == -1 || uploadIdx + 2 >= segments.Length) return url;
            
            // Skip 'upload' and the version segment (e.g. v1)
            var publicIdWithExt = string.Join("/", segments[(uploadIdx + 2)..]);
            
            // Cloudinary public ID for raw files often includes the extension
            return publicIdWithExt;
        }
        catch
        {
            return url;
        }
    }
}
