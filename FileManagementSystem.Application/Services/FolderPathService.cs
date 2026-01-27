using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Domain.Entities;

namespace FileManagementSystem.Application.Services;

public class FolderPathService
{
    private readonly IUnitOfWork _unitOfWork;

    public FolderPathService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task UpdateSubFolderPathsAsync(Folder parentFolder, string oldParentPath, string newParentPath, CancellationToken cancellationToken)
    {
        var subFolders = await _unitOfWork.Folders.GetByParentIdAsync(parentFolder.Id, cancellationToken);
        
        foreach (var subFolder in subFolders)
        {
            // Replace the old parent path with the new parent path
            if (subFolder.Path.StartsWith(oldParentPath))
            {
                var remainingPath = subFolder.Path.Substring(oldParentPath.Length).TrimStart('/', '\\');
                subFolder.Path = newParentPath.TrimEnd('/', '\\') + Path.DirectorySeparatorChar + remainingPath;
                await _unitOfWork.Folders.UpdateAsync(subFolder, cancellationToken);
                
                // Recursively update subfolders
                var oldSubPath = oldParentPath.TrimEnd('/', '\\') + Path.DirectorySeparatorChar + subFolder.Name;
                await UpdateSubFolderPathsAsync(subFolder, oldSubPath, subFolder.Path, cancellationToken);
            }
        }
    }

    public async Task UpdateFilePathsAsync(Folder folder, string oldFolderPath, string newFolderPath, CancellationToken cancellationToken)
    {
        var files = await _unitOfWork.Files.FindAsync(f => f.FolderId == folder.Id, cancellationToken);
        
        foreach (var file in files)
        {
            // Update file path if it references the folder path
            if (file.Path.StartsWith(oldFolderPath))
            {
                var remainingPath = file.Path.Substring(oldFolderPath.Length).TrimStart('/', '\\');
                file.Path = newFolderPath.TrimEnd('/', '\\') + Path.DirectorySeparatorChar + remainingPath;
                await _unitOfWork.Files.UpdateAsync(file, cancellationToken);
            }
        }
    }
}
