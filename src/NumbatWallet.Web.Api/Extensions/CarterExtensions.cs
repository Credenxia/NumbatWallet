using Carter;
using FluentValidation;
using NumbatWallet.Web.Api.Rest.Modules;
using NumbatWallet.Web.Api.Rest.Validators;

namespace NumbatWallet.Web.Api.Extensions;

public static class CarterExtensions
{
    public static IServiceCollection AddCarterWithValidation(this IServiceCollection services)
    {
        // Add Carter
        services.AddCarter();

        // Register validators
        services.AddScoped<IValidator<DtpVerifyRequest>, DtpVerifyRequestValidator>();
        services.AddScoped<IValidator<DtpIssueRequest>, DtpIssueRequestValidator>();
        services.AddScoped<IValidator<DtpRevokeRequest>, DtpRevokeRequestValidator>();

        return services;
    }

    public static WebApplication MapCarterEndpoints(this WebApplication app)
    {
        // Map Carter modules
        app.MapCarter();

        // Add endpoint metadata for OpenAPI
        app.MapGet("/api", () => new
        {
            message = "NumbatWallet API",
            version = "1.0.0",
            endpoints = new
            {
                graphql = "/graphql",
                graphql_playground = "/graphql",
                rest_dtp = "/api/v1/dtp",
                rest_webhooks = "/webhooks",
                health = "/health",
                swagger = "/swagger"
            }
        })
        .WithName("ApiRoot")
        .WithTags("API")
        .WithOpenApi()
        .AllowAnonymous();

        return app;
    }
}