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
        // DbContext - Scoped
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

        // Repositories - Scoped
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

        // Services - Scoped
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

        // Register ILogger<T> factory
        container.Register(
            Component.For(typeof(ILogger<>))
                .ImplementedBy(typeof(Logger<>))
                .UsingFactoryMethod((kernel, model, context) =>
                {
                    var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
                        builder.AddSerilog(Log.Logger));
                    var loggerType = typeof(ILogger<>).MakeGenericType(context.RequestedType.GetGenericArguments()[0]);
                    return loggerFactory.CreateLogger(context.RequestedType.GetGenericArguments()[0]);
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

        // MediatR service factory
        container.Register(
            Component.For<IMediator>()
                .UsingFactoryMethod(kernel =>
                {
                    var serviceFactory = new MediatRServiceFactory(kernel);
                    return new MediatR.Mediator(serviceFactory);
                })
                .LifestyleScoped()
        );
    }
}

// MediatR service factory adapter for Castle Windsor
internal class MediatRServiceFactory : MediatR.ServiceFactory
{
    private readonly Castle.Windsor.IWindsorContainer _container;

    public MediatRServiceFactory(Castle.Windsor.IWindsorContainer container)
    {
        _container = container;
    }

    public override object? GetInstance(Type serviceType)
    {
        return _container.Kernel.HasComponent(serviceType)
            ? _container.Resolve(serviceType)
            : null;
    }

    public override object GetInstance(Type serviceType, object? serviceKey)
    {
        return _container.Kernel.HasComponent(serviceType)
            ? _container.Resolve(serviceType)
            : GetInstance(serviceType) ?? throw new InvalidOperationException($"Service {serviceType} not registered");
    }

    public override IEnumerable<object> GetInstances(Type serviceType)
    {
        return _container.ResolveAll(serviceType);
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
    private readonly IDisposable _scope;

    public WindsorServiceScope(Castle.Windsor.IWindsorContainer container)
    {
        _container = container;
        _scope = container.BeginScope();
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
        return _container.Kernel.HasComponent(serviceType)
            ? _container.Resolve(serviceType)
            : null;
    }
}
