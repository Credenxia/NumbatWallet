using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace NumbatWallet.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register AutoMapper
        services.AddAutoMapper(assembly);

        // Register FluentValidation validators
        // TODO: Uncomment when validators are implemented
        // services.AddValidatorsFromAssembly(assembly);

        // Register custom CQRS implementation (no MediatR as per issue #154)
        // TODO: Implement CommandDispatcher and QueryDispatcher
        // services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        // services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        // Register all command handlers
        // TODO: Implement command handlers
        // services.Scan(scan => scan
        //     .FromAssemblies(assembly)
        //     .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)))
        //     .AsImplementedInterfaces()
        //     .WithScopedLifetime());

        // Register Application Services
        // TODO: Implement application services
        // services.AddScoped<IWalletService, WalletService>();
        // services.AddScoped<ICredentialService, CredentialService>();

        return services;
    }
}