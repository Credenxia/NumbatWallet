using Microsoft.Extensions.DependencyInjection;
using NumbatWallet.Application.CQRS;
using NumbatWallet.Application.CQRS.Interfaces;
using System.Reflection;
using FluentValidation;
using Scrutor;

namespace NumbatWallet.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register AutoMapper
        services.AddAutoMapper(assembly);

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
        // TODO: Implement application services when commands/queries are created
        // services.AddScoped<IWalletService, WalletService>();
        // services.AddScoped<ICredentialService, CredentialService>();

        return services;
    }
}