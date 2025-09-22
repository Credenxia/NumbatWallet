using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using NumbatWallet.Web.Api.Authorization.Handlers;
using NumbatWallet.Web.Api.Middleware;
// TODO: Implement GraphQL types
// using NumbatWallet.Web.Api.GraphQL;
using HotChocolate.Execution.Configuration;

namespace NumbatWallet.Web.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add Controllers with JSON options
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = false;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        // Add API Versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("x-api-version"),
                new MediaTypeApiVersionReader("x-api-version"));
        });

        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Configure Security Middleware Options
        services.Configure<MutualTlsOptions>(configuration.GetSection("Security:MutualTls"));
        services.Configure<RequestSignatureOptions>(configuration.GetSection("Security:RequestSignature"));

        // Add CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowedOrigins", builder =>
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? new[] { "http://localhost:3000" };

                builder.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        // Add Authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));

        // Register authorization handlers
        services.AddSingleton<IAuthorizationHandler, TenantAccessHandler>();
        services.AddScoped<IAuthorizationHandler, CredentialOwnerHandler>();
        services.AddScoped<IAuthorizationHandler, WalletOwnerHandler>();
        services.AddHttpContextAccessor(); // Required for TenantAccessHandler

        // Add Authorization
        services.AddAuthorization(options =>
        {
            // Policy for citizen users (via ServiceWA)
            options.AddPolicy("CitizenUser", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("user_type", "citizen");
            });

            // Policy for government officers
            options.AddPolicy("GovernmentOfficer", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("user_type", "officer");
                policy.RequireRole("Officer", "Admin");
            });

            // Policy for system administrators
            options.AddPolicy("SystemAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Admin");
            });

            // Policy for API access
            options.AddPolicy("ApiAccess", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("scope", "api.access");
            });
        });

        // Swagger is configured in SwaggerExtensions.cs via AddSwaggerDocumentation()
        // Just add the EndpointsApiExplorer which is needed for minimal APIs
        services.AddEndpointsApiExplorer();

        return services;
    }

    public static IRequestExecutorBuilder AddGraphQL(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddGraphQLServer()
            .AddAuthorization()
            // TODO: Implement GraphQL types
            // .AddQueryType<Query>()
            // .AddMutationType<Mutation>()
            // .AddSubscriptionType<Subscription>()
            // .AddType<WalletType>()
            // .AddType<CredentialType>()
            // .AddType<PersonType>()
            // .AddType<IssuerType>()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .ModifyRequestOptions(opt =>
            {
                opt.IncludeExceptionDetails = configuration.GetValue<bool>("GraphQL:IncludeExceptionDetails");
            });
    }
}
