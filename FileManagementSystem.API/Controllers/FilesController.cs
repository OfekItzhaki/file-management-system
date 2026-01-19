using Microsoft.AspNetCore.Mvc;
using MediatR;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Queries;

namespace FileManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IMediator mediator, ILogger<FilesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get list of files with optional filters
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<SearchFilesResult>> GetFiles(
        [FromQuery] string? searchTerm = null,
        [FromQuery] List<string>? tags = null,
        [FromQuery] bool? isPhoto = null,
        [FromQuery] Guid? folderId = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchFilesQuery(
            SearchTerm: searchTerm,
            Tags: tags,
            IsPhoto: isPhoto,
            FolderId: folderId,
            Skip: skip,
            Take: take
        );

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get file by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetFile(Guid id, CancellationToken cancellationToken = default)
    {
        var query = new GetFileQuery(id);
        var file = await _mediator.Send(query, cancellationToken);
        
        if (file == null)
        {
            return NotFound();
        }
        
        return Ok(file);
    }

    /// <summary>
    /// Upload a file
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<UploadFileResult>> UploadFile(
        IFormFile file,
        [FromQuery] string? destinationFolder = null,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided");
        }

        // Save uploaded file to temp location
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Path.GetExtension(file.FileName));
        
        using (var stream = System.IO.File.Create(tempPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        try
        {
            var command = new UploadFileCommand(tempPath, destinationFolder);
            var result = await _mediator.Send(command, cancellationToken);
            
            return Ok(result);
        }
        finally
        {
            // Clean up temp file if still exists
            if (System.IO.File.Exists(tempPath))
            {
                try { System.IO.File.Delete(tempPath); } catch { }
            }
        }
    }

    /// <summary>
    /// Delete a file
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteFile(
        Guid id,
        [FromQuery] bool moveToRecycleBin = true,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteFileCommand(id, moveToRecycleBin);
        var result = await _mediator.Send(command, cancellationToken);
        
        if (!result.Success)
        {
            return NotFound();
        }
        
        return NoContent();
    }

    /// <summary>
    /// Rename a file
    /// </summary>
    [HttpPut("{id}/rename")]
    public async Task<ActionResult<RenameFileResult>> RenameFile(
        Guid id,
        [FromBody] RenameFileRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new RenameFileCommand(id, request.NewName);
        var result = await _mediator.Send(command, cancellationToken);
        
        if (!result.Success)
        {
            return NotFound();
        }
        
        return Ok(result);
    }

    /// <summary>
    /// Add tags to a file
    /// </summary>
    [HttpPost("{id}/tags")]
    public async Task<ActionResult> AddTags(
        Guid id,
        [FromBody] AddTagsRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new AddTagsCommand(id, request.Tags);
        await _mediator.Send(command, cancellationToken);
        
        return NoContent();
    }
}

public record RenameFileRequest(string NewName);
public record AddTagsRequest(List<string> Tags);
