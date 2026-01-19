using System.Collections.Generic;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Infrastructure.Repositories;
using FileManagementSystem.Infrastructure.Services;
using FileManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FluentValidation;
using MediatR;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Behaviors;
using Serilog;

namespace FileManagementSystem.API.Installers;

public class WindsorInstaller : IWindsorInstaller
{
    private readonly IConfiguration _configuration;

    public WindsorInstaller(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Install(IWindsorContainer container, IConfigurationStore store)
    {
        // Register the container itself first so it can be resolved
        container.Register(
            Component.For<Castle.Windsor.IWindsorContainer>()
                .Instance(container)
                .LifestyleSingleton()
        );
        
        // DbContext - Scoped (managed by middleware)
        container.Register(
            Component.For<AppDbContext>()
                .UsingFactoryMethod(() =>
                {
                    var connectionString = _configuration.GetConnectionString("DefaultConnection")
                        ?? "Data Source=filemanager.db";
                    var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                    optionsBuilder.UseSqlite(connectionString)
                        .EnableSensitiveDataLogging(false)
                        .EnableServiceProviderCaching();
                    return new AppDbContext(optionsBuilder.Options);
                })
                .LifestyleScoped()
        );

        // Memory Cache - Singleton
        container.Register(
            Component.For<IMemoryCache>()
                .ImplementedBy<MemoryCache>()
                .UsingFactoryMethod(() => new MemoryCache(new MemoryCacheOptions()))
                .LifestyleSingleton()
        );

        // Repositories - Scoped (managed by middleware)
        container.Register(
            Component.For<IUnitOfWork>()
                .ImplementedBy<UnitOfWork>()
                .LifestyleScoped(),
            Component.For<IFileRepository>()
                .ImplementedBy<FileRepository>()
                .LifestyleScoped(),
            Component.For<IFolderRepository>()
                .ImplementedBy<CachedFolderRepository>()
                .LifestyleScoped()
        );

        // Services - Scoped (managed by middleware)
        container.Register(
            Component.For<IMetadataService>()
                .ImplementedBy<MetadataService>()
                .LifestyleScoped(),
            Component.For<IStorageService>()
                .ImplementedBy<StorageService>()
                .DependsOn(
                    Dependency.OnValue<IConfiguration>(_configuration),
                    Dependency.OnComponent<ILogger<StorageService>, ILogger<StorageService>>()
                )
                .LifestyleScoped()
        );

        // Register ILoggerFactory (needed to create loggers)
        container.Register(
            Component.For<ILoggerFactory>()
                .UsingFactoryMethod(() =>
                {
                    return Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
                        builder.AddSerilog(Log.Logger));
                })
                .LifestyleSingleton()
        );

        // Register ILogger<T> factory
        container.Register(
            Component.For(typeof(ILogger<>))
                .UsingFactoryMethod((kernel, model, context) =>
                {
                    // Get the generic type argument (T) from ILogger<T>
                    var loggerType = context.RequestedType.GetGenericArguments()[0];
                    
                    // Resolve ILoggerFactory from container
                    var loggerFactory = kernel.Resolve<ILoggerFactory>();
                    
                    // Create logger for the requested type
                    return loggerFactory.CreateLogger(loggerType);
                })
                .LifestyleTransient()
        );

        // Register IServiceScopeFactory for Authentication/Authorization services
        container.Register(
            Component.For<IServiceScopeFactory>()
                .UsingFactoryMethod(() => new ServiceScopeFactoryAdapter(container))
                .LifestyleSingleton()
        );

        // Authentication/Authorization - Singleton (used by pipeline behaviors)
        container.Register(
            Component.For<IAuthenticationService>()
                .ImplementedBy<AuthenticationService>()
                .DependsOn(
                    Dependency.OnValue<IConfiguration>(_configuration),
                    Dependency.OnComponent<ILogger<AuthenticationService>, ILogger<AuthenticationService>>()
                )
                .LifestyleSingleton(),
            Component.For<IAuthorizationService>()
                .ImplementedBy<AuthorizationService>()
                .DependsOn(
                    Dependency.OnComponent<ILogger<AuthorizationService>, ILogger<AuthorizationService>>()
                )
                .LifestyleSingleton()
        );

        // MediatR - register handlers from assembly
        var assembly = typeof(ScanDirectoryCommand).Assembly;
        container.Register(
            Classes.FromAssembly(assembly)
                .BasedOn(typeof(IRequestHandler<,>))
                .WithServiceAllInterfaces()
                .LifestyleScoped()
        );

        // MediatR pipeline behaviors
        container.Register(
            Component.For(typeof(IPipelineBehavior<,>))
                .ImplementedBy(typeof(LoggingBehavior<,>))
                .LifestyleScoped(),
            Component.For(typeof(IPipelineBehavior<,>))
                .ImplementedBy(typeof(AuthorizationBehavior<,>))
                .LifestyleScoped(),
            Component.For(typeof(IPipelineBehavior<,>))
                .ImplementedBy(typeof(ValidationBehavior<,>))
                .LifestyleScoped(),
            Component.For(typeof(IPipelineBehavior<,>))
                .ImplementedBy(typeof(ExceptionHandlingBehavior<,>))
                .LifestyleScoped()
        );

        // FluentValidation validators
        container.Register(
            Classes.FromAssembly(assembly)
                .BasedOn(typeof(IValidator<>))
                .WithServiceAllInterfaces()
                .LifestyleScoped()
        );

        // MediatR service factory - MediatR 12.x uses IServiceProvider
        container.Register(
            Component.For<IMediator>()
                .UsingFactoryMethod(kernel =>
                {
                    // Create a service provider adapter for MediatR
                    // Resolve the container (now registered above)
                    var windsorContainer = kernel.Resolve<Castle.Windsor.IWindsorContainer>();
                    var serviceProvider = new WindsorServiceProvider(windsorContainer);
                    return new MediatR.Mediator(serviceProvider);
                })
                .LifestyleScoped()
        );
    }
}

// IServiceScopeFactory adapter for Castle Windsor
internal class ServiceScopeFactoryAdapter : IServiceScopeFactory
{
    private readonly Castle.Windsor.IWindsorContainer _container;

    public ServiceScopeFactoryAdapter(Castle.Windsor.IWindsorContainer container)
    {
        _container = container;
    }

    public IServiceScope CreateScope()
    {
        return new WindsorServiceScope(_container);
    }
}

// IServiceScope adapter for Castle Windsor
internal class WindsorServiceScope : IServiceScope
{
    private readonly Castle.Windsor.IWindsorContainer _container;
    private readonly IDisposable? _scope;

    public WindsorServiceScope(Castle.Windsor.IWindsorContainer container)
    {
        _container = container;
        // Castle Windsor doesn't have BeginScope - we'll manage scope differently
        // For now, just store the container reference
        _scope = null; // No scope management needed for this adapter
    }

    public IServiceProvider ServiceProvider => new WindsorServiceProvider(_container);

    public void Dispose()
    {
        _scope?.Dispose();
    }
}

// IServiceProvider adapter for Castle Windsor
internal class WindsorServiceProvider : IServiceProvider
{
    private readonly Castle.Windsor.IWindsorContainer _container;

    public WindsorServiceProvider(Castle.Windsor.IWindsorContainer container)
    {
        _container = container;
    }

    public object? GetService(Type serviceType)
    {
        // Handle IEnumerable<T> - MediatR needs this for pipeline behaviors
        if (serviceType.IsGenericType && 
            serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var elementType = serviceType.GetGenericArguments()[0];
            
            if (_container.Kernel.HasComponent(elementType))
            {
                // Resolve all instances of the element type
                var allInstances = _container.ResolveAll(elementType);
                // Convert to array (arrays implement IEnumerable<T>)
                var array = Array.CreateInstance(elementType, allInstances.Length);
                Array.Copy(allInstances, array, allInstances.Length);
                return array;
            }
            
            // Return empty array if no components found (MediatR expects IEnumerable, not null)
            return Array.CreateInstance(elementType, 0);
        }
        
        // Handle single service resolution
        if (_container.Kernel.HasComponent(serviceType))
        {
            return _container.Resolve(serviceType);
        }
        
        return null;
    }
}
