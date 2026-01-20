using MediatR;
using Microsoft.Extensions.Logging;
using FileManagementSystem.Application.Commands;

namespace FileManagementSystem.Presentation.Services;

public class FileUploadService
{
    private readonly IMediator _mediator;
    private readonly ILogger<FileUploadService> _logger;

    public FileUploadService(IMediator mediator, ILogger<FileUploadService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<UploadFileResult> UploadFileAsync(string filePath)
    {
        try
        {
            var originalFileName = System.IO.Path.GetFileName(filePath);
            var command = new UploadFileCommand(filePath, originalFileName);
            var result = await _mediator.Send(command);
            
            _logger.LogInformation("File uploaded successfully: {FilePath}", result.FilePath);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<List<UploadFileResult>> UploadFilesAsync(IEnumerable<string> filePaths)
    {
        var results = new List<UploadFileResult>();
        
        foreach (var filePath in filePaths)
        {
            try
            {
                var result = await UploadFileAsync(filePath);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file via batch: {FilePath}", filePath);
                // Continue with other files even if one fails
            }
        }
        
        return results;
    }
}
