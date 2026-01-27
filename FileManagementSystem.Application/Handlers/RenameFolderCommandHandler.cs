using MediatR;
using Microsoft.Extensions.Logging;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Application.Mappings;
using FileManagementSystem.Application.Services;
using FileManagementSystem.Domain.Entities;

namespace FileManagementSystem.Application.Handlers;

public class RenameFolderCommandHandler : IRequestHandler<RenameFolderCommand, RenameFolderResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly FolderPathService _folderPathService;
    private readonly ILogger<RenameFolderCommandHandler> _logger;
    
    public RenameFolderCommandHandler(
        IUnitOfWork unitOfWork,
        FolderPathService folderPathService,
        ILogger<RenameFolderCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _folderPathService = folderPathService;
        _logger = logger;
    }
    
    public async Task<RenameFolderResult> Handle(RenameFolderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Renaming folder {FolderId} to {NewName}", request.FolderId, request.NewName);
        
        // Validate new name
        if (string.IsNullOrWhiteSpace(request.NewName))
        {
            return new RenameFolderResult(false, null, "Folder name cannot be empty");
        }
        
        var folder = await _unitOfWork.Folders.GetByIdAsync(request.FolderId, cancellationToken);
        if (folder == null)
        {
            _logger.LogWarning("Folder not found for rename: {FolderId}", request.FolderId);
            return new RenameFolderResult(false, null, "Folder not found");
        }
        
        // Check if a folder with the new name already exists under the same parent
        var siblingFolders = await _unitOfWork.Folders.GetByParentIdAsync(folder.ParentFolderId, cancellationToken);
        if (siblingFolders.Any(f => f.Id != request.FolderId && f.Name.Equals(request.NewName, StringComparison.OrdinalIgnoreCase)))
        {
            return new RenameFolderResult(false, null, $"A folder with the name '{request.NewName}' already exists in this location");
        }
        
        var oldPath = folder.Path;
        var oldName = folder.Name;
        
        // Update the folder name and path
        folder.Name = request.NewName;
        
        // Update the path
        if (folder.ParentFolderId.HasValue)
        {
            var parentFolder = await _unitOfWork.Folders.GetByIdAsync(folder.ParentFolderId.Value, cancellationToken);
            if (parentFolder != null)
            {
                var parentPath = parentFolder.Path.TrimEnd('/', '\\');
                folder.Path = parentPath + Path.DirectorySeparatorChar + request.NewName;
            }
        }
        else
        {
            folder.Path = request.NewName;
        }
        
        // Update all subfolder paths (recursive path update)
        await _folderPathService.UpdateSubFolderPathsAsync(folder, oldPath, folder.Path, cancellationToken);
        
        // Update file paths that reference this folder
        await _folderPathService.UpdateFilePathsAsync(folder, oldPath, folder.Path, cancellationToken);
        
        await _unitOfWork.Folders.UpdateAsync(folder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Folder renamed successfully: {FolderId}, Old: {OldName}, New: {NewName}", 
            folder.Id, oldName, request.NewName);
        
        var subFolders = await _unitOfWork.Folders.GetByParentIdAsync(folder.Id, cancellationToken);
        var filesInFolder = await _unitOfWork.Files.FindAsync(f => f.FolderId == folder.Id, cancellationToken);
        
        var folderDto = folder.ToDto(filesInFolder.Count(), subFolders.Count);
        return new RenameFolderResult(true, folderDto);
    }
    
}
