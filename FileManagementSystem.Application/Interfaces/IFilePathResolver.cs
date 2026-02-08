namespace FileManagementSystem.Application.Interfaces;

public interface IFilePathResolver
{
    string StorageRootPath { get; }
    string? ResolveFilePath(string storedPath, bool isCompressed);
}
