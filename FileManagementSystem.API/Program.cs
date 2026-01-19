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

// Configure DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=filemanager.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString)
        .EnableSensitiveDataLogging(false)
        .EnableServiceProviderCaching());

// Add Memory Cache
builder.Services.AddMemoryCache();

// Repositories and UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IFolderRepository, CachedFolderRepository>();

// Services
builder.Services.AddScoped<IMetadataService, MetadataService>();
builder.Services.AddScoped<IStorageService, StorageService>();
// PathValidationService doesn't have an interface, register it directly if needed later
// builder.Services.AddScoped<PathValidationService>();
// Authentication/Authorization services must be Singleton because they're used by Singleton pipeline behaviors
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
builder.Services.AddSingleton<IAuthorizationService, AuthorizationService>();

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ScanDirectoryCommand).Assembly);
    
    // Add pipeline behaviors (order matters)
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(ExceptionHandlingBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(ScanDirectoryCommand).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    if (dbContext.Database.EnsureCreated())
    {
        Log.Logger.Information("Database created successfully");
        
        // Seed initial data
        await FileManagementSystem.Infrastructure.Data.SeedData.SeedAsync(dbContext);
        Log.Logger.Information("Database seeded with initial data");
    }
    else
    {
        // Try to apply migrations if they exist
        try
        {
            await dbContext.Database.MigrateAsync();
            Log.Logger.Information("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "No migrations found or migration failed, using existing database");
        }
        
        // Seed data if users don't exist
        if (!dbContext.Set<FileManagementSystem.Domain.Entities.User>().Any())
        {
            await FileManagementSystem.Infrastructure.Data.SeedData.SeedAsync(dbContext);
            Log.Logger.Information("Database seeded with initial data");
        }
    }
}

Log.Logger.Information("File Management System API starting...");
app.Run();
