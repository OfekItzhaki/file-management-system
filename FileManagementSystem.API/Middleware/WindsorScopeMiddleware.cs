using Castle.Windsor;
using Castle.MicroKernel.Lifestyle;

namespace FileManagementSystem.API.Middleware;

/// <summary>
/// Middleware to manage Castle Windsor scopes per HTTP request
/// </summary>
public class WindsorScopeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWindsorContainer _container;

    public WindsorScopeMiddleware(RequestDelegate next, IWindsorContainer container)
    {
        _next = next;
        _container = container;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Begin a Windsor scope for this HTTP request
        // PerWebRequest lifestyle requires an active scope
        // Using the kernel's scope accessor to create a scope
        using (var scope = _container.Kernel.BeginScope())
        {
            await _next(context);
        }
    }
}
