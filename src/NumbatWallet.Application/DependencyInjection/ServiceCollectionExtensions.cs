using Microsoft.Extensions.DependencyInjection;
using NumbatWallet.Application.CQRS;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Application.Services;
using NumbatWallet.Domain.Events;
using System.Reflection;
using FluentValidation;

namespace NumbatWallet.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // AutoMapper removed - using extension methods for DTO conversions

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        // Register custom CQRS implementation (no MediatR as per issue #154)
        services.AddScoped<IDispatcher, Dispatcher>();

        // Auto-register all command handlers
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Auto-register all command handlers with results
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Auto-register all query handlers
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Register Application Services
        services.AddScoped<IEventDispatcher, EventDispatcher>();
        services.AddScoped<SharedKernel.Interfaces.IEventDispatcher, EventDispatcher>();

        // Register Mock Services for Development
        services.AddScoped<Commands.Credentials.Handlers.IVerificationService, Commands.Credentials.Handlers.MockVerificationService>();
        services.AddScoped<Commands.Credentials.Handlers.ICredentialSharingService, Commands.Credentials.Handlers.MockCredentialSharingService>();

        // Register Domain Event Handlers
        services.AddScoped<IDomainEventHandler<PersonCreatedEvent>, EventHandlers.PersonCreatedEventHandler>();
        services.AddScoped<IDomainEventHandler<WalletCreatedEvent>, EventHandlers.WalletCreatedEventHandler>();
        services.AddScoped<IDomainEventHandler<CredentialIssuedEvent>, EventHandlers.CredentialIssuedEventHandler>();
        services.AddScoped<IDomainEventHandler<CredentialRevokedEvent>, EventHandlers.CredentialRevokedEventHandler>();

        // Register Domain Services
        services.AddScoped<DomainServices.IWalletDomainService, DomainServices.WalletDomainService>();
        services.AddScoped<DomainServices.ICredentialDomainService, DomainServices.CredentialDomainService>();
        services.AddScoped<DomainServices.IVerificationDomainService, DomainServices.VerificationDomainService>();
        services.AddScoped<DomainServices.IPersonVerificationService, DomainServices.PersonVerificationService>();

        // Register Application Services
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<ICredentialService, CredentialService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IWalletTemplateService, WalletTemplateService>();
        services.AddScoped<ICertificateManagementService, CertificateManagementService>();
        // Note: IHealthCheckService is registered in Infrastructure layer

        // Register Bulk Operation Services
        services.AddSingleton<IBulkOperationStatusService, BulkOperationStatusService>();
        services.AddHostedService<BulkOperationCleanupService>();

        // TODO: Register Background Jobs after fixing them
        // services.AddHostedService<BackgroundJobs.CredentialExpiryCheckJob>();
        // services.AddHostedService<BackgroundJobs.StatisticsAggregationJob>();
        // services.AddHostedService<BackgroundJobs.DatabaseCleanupJob>();
        // services.AddHostedService<BackgroundJobs.NotificationProcessorJob>();

        // TODO: Register notification channel for async processing
        // services.AddSingleton(System.Threading.Channels.Channel.CreateUnbounded<BackgroundJobs.NotificationMessage>());

        return services;
    }
}
