using MediatR;
using Microsoft.Extensions.Logging;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Interfaces;

namespace FileManagementSystem.Application.Handlers;

public class SetTagsCommandHandler : IRequestHandler<SetTagsCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SetTagsCommandHandler> _logger;
    
    public SetTagsCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<SetTagsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    public async Task<bool> Handle(SetTagsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting tags for file {FileId}: {Tags}", 
            request.FileId, string.Join(", ", request.Tags));
        
        var file = await _unitOfWork.Files.GetByIdAsync(request.FileId, cancellationToken);
        if (file == null)
        {
            _logger.LogWarning("File not found for setting tags: {FileId}", request.FileId);
            return false;
        }
        
        // Replace existing tags with new list
        file.Tags = request.Tags ?? new List<string>();
        
        await _unitOfWork.Files.UpdateAsync(file, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Successfully set {Count} tags for file {FileId}", file.Tags.Count, request.FileId);
        
        return true;
    }
}
