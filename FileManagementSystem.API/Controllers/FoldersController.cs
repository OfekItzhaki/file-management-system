using Microsoft.AspNetCore.Mvc;
using MediatR;
using FileManagementSystem.Application.Queries;

namespace FileManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FoldersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FoldersController> _logger;

    public FoldersController(IMediator mediator, ILogger<FoldersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all folders (tree structure)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<GetFoldersResult>> GetFolders(CancellationToken cancellationToken = default)
    {
        var query = new GetFoldersQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
