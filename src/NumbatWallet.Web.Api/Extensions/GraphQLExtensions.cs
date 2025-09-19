using HotChocolate.AspNetCore;
using HotChocolate.Data;
using HotChocolate.Diagnostics;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NumbatWallet.Web.Api.GraphQL.Schema;

namespace NumbatWallet.Web.Api.Extensions;

public static class GraphQLExtensions
{
    public static IServiceCollection AddGraphQL(this IServiceCollection services, IConfiguration configuration)
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
            .AddDiagnosticEventListener<GraphQLDiagnosticEventListener>()
            .AddHttpRequestInterceptor<GraphQLRequestInterceptor>()
            .AddErrorFilter<GraphQLErrorFilter>()
            .AddMaxExecutionDepthRule(configuration.GetValue<int>("GraphQL:MaxExecutionDepth", 15))
            .AddTypeConverter<DateTimeTypeConverter>()
            .AddTypeConverter<GuidTypeConverter>()
            .UseExceptions()
            .UseTimeout()
            .UseDocumentCache()
            .UseDocumentValidation()
            .UseDocumentParser()
            .UseOperationCache()
            .UseOperationComplexityAnalyzer()
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
            .RequireAuthorization()
            .WithOptions(new GraphQLEndpointConventionBuilderExtensions.GraphQLEndpointOptions
            {
                EnableSchemaRequests = environment != "Production",
                EnableGetRequests = true
            });

        // Enable GraphQL Voyager for schema visualization in non-production
        if (environment != "Production")
        {
            app.MapGraphQLVoyager("/graphql-voyager", "/graphql");
            app.MapBananaCakePop("/graphql-ui");
        }

        // Map WebSocket for subscriptions
        app.MapGraphQLWebSocket("/graphql");

        return app;
    }
}

// Custom diagnostic event listener for monitoring
public class GraphQLDiagnosticEventListener : DiagnosticEventListener
{
    private readonly ILogger<GraphQLDiagnosticEventListener> _logger;

    public GraphQLDiagnosticEventListener(ILogger<GraphQLDiagnosticEventListener> logger)
    {
        _logger = logger;
    }

    public override IDisposable ExecuteRequest(IRequestContext context)
    {
        var start = DateTime.UtcNow;

        return new RequestScope(start, context, _logger);
    }

    private class RequestScope : IDisposable
    {
        private readonly DateTime _start;
        private readonly IRequestContext _context;
        private readonly ILogger<GraphQLDiagnosticEventListener> _logger;

        public RequestScope(DateTime start, IRequestContext context, ILogger<GraphQLDiagnosticEventListener> logger)
        {
            _start = start;
            _context = context;
            _logger = logger;
        }

        public void Dispose()
        {
            var duration = DateTime.UtcNow - _start;

            if (_context.Request?.Query != null)
            {
                _logger.LogInformation(
                    "GraphQL request executed in {Duration}ms. Operation: {OperationName}",
                    duration.TotalMilliseconds,
                    _context.Request.OperationName ?? "Anonymous");
            }

            if (duration.TotalSeconds > 5)
            {
                _logger.LogWarning(
                    "Slow GraphQL query detected. Duration: {Duration}ms, Query: {Query}",
                    duration.TotalMilliseconds,
                    _context.Request?.Query?.ToString());
            }
        }
    }
}

// Request interceptor for authentication and tenant context
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
        // Extract tenant information from headers or claims
        var tenantId = context.User.FindFirst("tenant_id")?.Value
            ?? context.Request.Headers["X-Tenant-Id"].FirstOrDefault();

        if (!string.IsNullOrEmpty(tenantId))
        {
            requestBuilder.SetProperty("TenantId", tenantId);
            _logger.LogDebug("GraphQL request for tenant: {TenantId}", tenantId);
        }

        // Extract correlation ID for distributed tracing
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        requestBuilder.SetProperty("CorrelationId", correlationId);
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}

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
                .AddLocation(error.Locations?.FirstOrDefault())
                .SetPath(error.Path)
                .Build();
        }

        return error;
    }
}

// Type converters for common types
public class DateTimeTypeConverter : ITypeConverter<DateTime, string>
{
    public string Convert(DateTime from)
    {
        return from.ToString("O"); // ISO 8601 format
    }
}

public class GuidTypeConverter : ITypeConverter<Guid, string>
{
    public string Convert(Guid from)
    {
        return from.ToString("D"); // Standard GUID format
    }
}