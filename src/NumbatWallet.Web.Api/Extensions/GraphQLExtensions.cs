using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.Data;
using HotChocolate.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NumbatWallet.Web.Api.GraphQL.Schema;

namespace NumbatWallet.Web.Api.Extensions;

public static class GraphQLExtensions
{
    public static IServiceCollection AddGraphQLServer(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddGraphQLServer()
            .AddAuthorization()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddSubscriptionType<Subscription>()
            .AddProjections()
            .AddFiltering()
            .AddSorting()
            .AddInMemorySubscriptions()
            .ModifyRequestOptions(opt =>
            {
                opt.IncludeExceptionDetails = configuration.GetValue<bool>("GraphQL:IncludeExceptionDetails");
                opt.ExecutionTimeout = TimeSpan.FromSeconds(configuration.GetValue<int>("GraphQL:ExecutionTimeoutSeconds", 30));
            })
            // .AddDiagnosticEventListener<GraphQLDiagnosticEventListener>() // TODO: Re-enable when API is stable
            // .AddHttpRequestInterceptor<GraphQLRequestInterceptor>() // TODO: Re-enable when API is stable
            .AddErrorFilter<GraphQLErrorFilter>()
            .AddMaxExecutionDepthRule(configuration.GetValue<int>("GraphQL:MaxExecutionDepth", 15))
            .UseExceptions()
            .UseTimeout()
            .UseDocumentCache()
            .UseDocumentValidation()
            .UseDocumentParser()
            .UseOperationCache()
            .UseOperationResolver()
            .UseOperationVariableCoercion()
            .UseOperationExecution();

        // Configure GraphQL playground and introspection based on environment
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        if (environment != "Production")
        {
            services.Configure<GraphQLServerOptions>(options =>
            {
                options.Tool.Enable = true;
                options.Tool.GraphQLEndpoint = "/graphql";
                options.Tool.Title = "NumbatWallet GraphQL Playground";
            });
        }

        return services;
    }

    public static WebApplication MapGraphQL(this WebApplication app)
    {
        var environment = app.Environment.EnvironmentName;

        app.MapGraphQL("/graphql")
            .RequireAuthorization();

        // TODO: Add GraphQL development tools when available

        // Map WebSocket for subscriptions
        app.MapGraphQLWebSocket("/graphql");

        return app;
    }
}

// TODO: Implement custom diagnostic event listener when HotChocolate.Diagnostics API is stable

// TODO: Implement request interceptor when HotChocolate API is stable

// Error filter for consistent error handling
public class GraphQLErrorFilter : IErrorFilter
{
    private readonly ILogger<GraphQLErrorFilter> _logger;

    public GraphQLErrorFilter(ILogger<GraphQLErrorFilter> logger)
    {
        _logger = logger;
    }

    public IError OnError(IError error)
    {
        // Log errors based on severity
        if (error.Exception != null)
        {
            _logger.LogError(error.Exception, "GraphQL execution error: {Message}", error.Message);
        }
        else
        {
            _logger.LogWarning("GraphQL validation error: {Message}", error.Message);
        }

        // Sanitize error messages for production
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        if (environment == "Production" && error.Exception != null)
        {
            // Hide internal error details in production
            return ErrorBuilder.New()
                .SetMessage("An error occurred while processing your request.")
                .SetCode(error.Code ?? "INTERNAL_ERROR")
                .AddLocation((error.Locations != null && error.Locations.Count > 0) ? error.Locations[0] : new Location(0, 0))
                .SetPath(error.Path)
                .Build();
        }

        return error;
    }
}

// TODO: Implement type converters when HotChocolate API is stable