using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Utilities;
using NumbatWallet.Web.Api.GraphQL.Schema;
using NumbatWallet.Web.Api.GraphQL.Types;
using NumbatWallet.Web.Api.GraphQL.Queries;
using NumbatWallet.Web.Api.GraphQL.Mutations;
using System.Diagnostics.CodeAnalysis;

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
            // Register credential types
            .AddType<CredentialType>()
            .AddType<IssuanceType>()
            .AddType<VerificationResultType>()
            // Register credential queries and mutations
            .AddTypeExtension<CredentialQuery>()
            .AddTypeExtension<CredentialMutation>()
            .AddProjections()
            .AddFiltering()
            .AddSorting()
            .AddInMemorySubscriptions()
            .ModifyRequestOptions(opt =>
            {
                opt.IncludeExceptionDetails = configuration.GetValue<bool>("GraphQL:IncludeExceptionDetails");
                opt.ExecutionTimeout = TimeSpan.FromSeconds(configuration.GetValue<int>("GraphQL:ExecutionTimeoutSeconds", 30));
            })
            // .AddDiagnosticEventListener<GraphQLDiagnosticEventListener>() // Re-enable when HotChocolate supports this
            // .AddHttpRequestInterceptor<GraphQLRequestInterceptor>() // Re-enable when HotChocolate supports this
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

        // Allow anonymous access in Development for schema export and testing
        if (environment == "Development")
        {
            app.MapGraphQL("/graphql");
        }
        else
        {
            app.MapGraphQL("/graphql")
                .RequireAuthorization();
        }

        // TODO: Enable GraphQL Voyager when package is available
        // if (environment != "Production")
        // {
        //     app.UseGraphQLVoyager("/graphql-voyager");
        // }

        // Map WebSocket for subscriptions
        app.MapGraphQLWebSocket("/graphql");

        return app;
    }
}

// Custom diagnostic event listener for performance monitoring
// TODO: Re-enable when HotChocolate provides stable DiagnosticEventListener API
/*
public class GraphQLDiagnosticEventListener : DiagnosticEventListener
{
    private readonly ILogger<GraphQLDiagnosticEventListener> _logger;

    public GraphQLDiagnosticEventListener(ILogger<GraphQLDiagnosticEventListener> logger)
    {
        _logger = logger;
    }

    public override void RequestError(IRequestContext context, Exception exception)
    {
        _logger.LogError(exception, "GraphQL request error occurred");
    }

    public override void TaskError(IExecutionTask task, IError error)
    {
        _logger.LogError("Task error: {ErrorMessage}", error.Message);
    }

    public override void SyntaxError(IRequestContext context, IError error)
    {
        _logger.LogWarning("GraphQL syntax error: {ErrorMessage}", error.Message);
    }

    public override void ValidationErrors(IRequestContext context, IReadOnlyList<IError> errors)
    {
        foreach (var error in errors)
        {
            _logger.LogWarning("GraphQL validation error: {ErrorMessage}", error.Message);
        }
    }
}
*/

// Request interceptor for authentication and request modification
// TODO: Re-enable when HotChocolate provides stable interceptor API
/*
public class GraphQLRequestInterceptor : DefaultHttpRequestInterceptor
{
    private readonly ILogger<GraphQLRequestInterceptor> _logger;

    public GraphQLRequestInterceptor(ILogger<GraphQLRequestInterceptor> logger)
    {
        _logger = logger;
    }

    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        // Add correlation ID to GraphQL request
        var correlationId = context.TraceIdentifier;
        requestBuilder.SetProperty("CorrelationId", correlationId);

        // Add tenant context from HTTP context
        if (context.Items.TryGetValue("TenantId", out var tenantId))
        {
            requestBuilder.SetProperty("TenantId", tenantId);
        }

        // Log request details
        _logger.LogDebug("GraphQL request initiated with CorrelationId: {CorrelationId}", correlationId);

        return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}
*/

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

// Type converters for custom scalar types
// TODO: Implement when HotChocolate provides stable type converter API
/*
public class DateOnlyTypeConverter : IChangeTypeProvider
{
    public bool TryCreateConverter(
        Type source,
        Type target,
        ChangeTypeProvider root,
        [NotNullWhen(true)] out ChangeType? converter)
    {
        if (source == typeof(DateOnly) && target == typeof(string))
        {
            converter = (input) => ((DateOnly)input).ToString("yyyy-MM-dd");
            return true;
        }

        if (source == typeof(string) && target == typeof(DateOnly))
        {
            converter = (input) => DateOnly.Parse((string)input);
            return true;
        }

        converter = null;
        return false;
    }
}
*/
