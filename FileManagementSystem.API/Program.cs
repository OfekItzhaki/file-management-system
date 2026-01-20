using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Serilog;
using FluentValidation;
using MediatR;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Services;
using FileManagementSystem.Infrastructure.Data;
using FileManagementSystem.Infrastructure.Repositories;
using FileManagementSystem.Infrastructure.Services;
using FileManagementSystem.Application.Behaviors;
using Castle.Windsor;
using Castle.MicroKernel.Registration;
using FileManagementSystem.API.Installers;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.File("logs/api.log", rollingInterval: RollingInterval.Day)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "File Management System API", 
        Version = "v1" 
    });
});

// Configure CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // React dev servers
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Create Windsor container and install components
var container = new WindsorContainer();
container.Install(new WindsorInstaller(builder.Configuration));

// Register Windsor container with ASP.NET Core DI
builder.Services.AddSingleton<IWindsorContainer>(container);

// Register business services from Windsor into ASP.NET Core DI
// Use factory that resolves from active Windsor scope (created by middleware)
builder.Services.AddScoped<IUnitOfWork>(sp => 
{
    var windsorContainer = sp.GetRequiredService<IWindsorContainer>();
    return windsorContainer.Resolve<IUnitOfWork>();
});
builder.Services.AddScoped<IFileRepository>(sp => 
{
    var windsorContainer = sp.GetRequiredService<IWindsorContainer>();
    return windsorContainer.Resolve<IFileRepository>();
});
builder.Services.AddScoped<IFolderRepository>(sp => 
{
    var windsorContainer = sp.GetRequiredService<IWindsorContainer>();
    return windsorContainer.Resolve<IFolderRepository>();
});
builder.Services.AddScoped<IMetadataService>(sp => 
{
    var windsorContainer = sp.GetRequiredService<IWindsorContainer>();
    return windsorContainer.Resolve<IMetadataService>();
});
builder.Services.AddScoped<IStorageService>(sp => 
{
    var windsorContainer = sp.GetRequiredService<IWindsorContainer>();
    return windsorContainer.Resolve<IStorageService>();
});
builder.Services.AddSingleton<IAuthenticationService>(sp => 
{
    var windsorContainer = sp.GetRequiredService<IWindsorContainer>();
    return windsorContainer.Resolve<IAuthenticationService>();
});
builder.Services.AddSingleton<IAuthorizationService>(sp => 
{
    var windsorContainer = sp.GetRequiredService<IWindsorContainer>();
    return windsorContainer.Resolve<IAuthorizationService>();
});

// MediatR - use ASP.NET Core DI for handlers and pipeline behaviors
// This is more reliable than Castle Windsor integration
var assembly = typeof(ScanDirectoryCommand).Assembly;
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(assembly);
    
    // Add pipeline behaviors (order matters - they execute in this order)
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(ExceptionHandlingBehavior<,>));
});

// FluentValidation validators
builder.Services.AddValidatorsFromAssembly(assembly);

// DbContext - register directly for EF Core compatibility
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=filemanager.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString)
        .EnableSensitiveDataLogging(false)
        .EnableServiceProviderCaching());

// Memory Cache
builder.Services.AddMemoryCache();

// Application services
builder.Services.AddScoped<FileManagementSystem.API.Services.FilePathResolver>();
builder.Services.AddScoped<FileManagementSystem.Application.Services.UploadDestinationResolver>();

var app = builder.Build();

// Register IServiceProvider in Windsor so it can resolve ASP.NET Core services (like ILogger<>)
// This must be done AFTER the app is built so we have the final service provider
container.Register(
    Component.For<IServiceProvider>()
        .Instance(app.Services)
        .LifestyleSingleton()
);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");

app.UseHttpsRedirection();

// Windsor scope middleware - must be early in pipeline to create scope for request
app.UseMiddleware<FileManagementSystem.API.Middleware.WindsorScopeMiddleware>();

// Global exception handler middleware
app.UseMiddleware<FileManagementSystem.API.Middleware.GlobalExceptionHandlerMiddleware>();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

// Ensure database is created and initialized
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<FileManagementSystem.Infrastructure.Data.DatabaseInitializer>>();
    var initializer = new FileManagementSystem.Infrastructure.Data.DatabaseInitializer(dbContext, logger);
    await initializer.InitializeAsync();
}

Log.Logger.Information("File Management System API starting...");

try
{
    app.Run();
}
finally
{
    // Dispose Windsor container on shutdown
    container?.Dispose();
    Log.CloseAndFlush();
}
