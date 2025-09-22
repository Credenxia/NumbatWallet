using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace NumbatWallet.Web.Api.Extensions;

/// <summary>
/// API versioning configuration extensions
/// POA: Implementing proper API versioning for backward compatibility
/// </summary>
public static class ApiVersioningExtensions
{
    public static IServiceCollection AddApiVersioningConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add API versioning
        services.AddApiVersioning(options =>
        {
            // Specify the default API version
            options.DefaultApiVersion = new ApiVersion(1, 0);

            // Assume default version when not specified
            options.AssumeDefaultVersionWhenUnspecified = true;

            // Report API versions in response headers
            options.ReportApiVersions = true;

            // API version readers (how version is specified)
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),           // /api/v1/...
                new HeaderApiVersionReader("X-API-Version"), // Header: X-API-Version: 1.0
                new MediaTypeApiVersionReader("version"),    // Accept: application/json;version=1.0
                new QueryStringApiVersionReader("api-version") // ?api-version=1.0
            );
        })
        .AddApiExplorer(options =>
        {
            // Format: 'v'major[.minor][-status]
            options.GroupNameFormat = "'v'VVV";

            // Substitute version in URLs
            options.SubstituteApiVersionInUrl = true;
        })
        .AddMvc(); // Add versioning to MVC

        return services;
    }

    public static IServiceCollection AddVersionedSwagger(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();

        // Configure Swagger for each API version
        services.AddSwaggerGen(options =>
        {
            // Add versions dynamically
            var provider = services.BuildServiceProvider()
                .GetRequiredService<IApiVersionDescriptionProvider>();

            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(
                    description.GroupName,
                    CreateInfoForApiVersion(description, configuration));
            }

            // Add security definition
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme."
            });

            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Name = "X-API-Key",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "API key authorization header."
            });

            // Add security requirements
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Add custom operation filter for versioning
            options.OperationFilter<ApiVersionOperationFilter>();

            // Add custom schema filter for deprecation
            options.SchemaFilter<DeprecatedSchemaFilter>();

            // Include XML comments if available
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    private static OpenApiInfo CreateInfoForApiVersion(
        ApiVersionDescription description,
        IConfiguration configuration)
    {
        var info = new OpenApiInfo
        {
            Title = configuration["Api:Title"] ?? "NumbatWallet API",
            Version = description.ApiVersion.ToString(),
            Description = configuration["Api:Description"] ??
                "Digital Identity Wallet API for secure credential management",
            Contact = new OpenApiContact
            {
                Name = "NumbatWallet Support",
                Email = "support@numbatwallet.gov.au",
                Url = new Uri("https://numbatwallet.gov.au/support")
            },
            License = new OpenApiLicense
            {
                Name = "Government License",
                Url = new Uri("https://numbatwallet.gov.au/license")
            },
            TermsOfService = new Uri("https://numbatwallet.gov.au/terms")
        };

        if (description.IsDeprecated)
        {
            info.Description += "\n\n**This API version is deprecated and will be removed in future releases.**";
        }

        // Add version-specific descriptions
        info.Description += description.ApiVersion.MajorVersion switch
        {
            1 => "\n\n### Version 1.0\n- Initial release\n- Basic credential operations\n- Wallet management",
            2 => "\n\n### Version 2.0\n- Enhanced security features\n- Bulk operations support\n- GraphQL endpoint\n- Real-time subscriptions",
            _ => ""
        };

        return info;
    }

    public static IApplicationBuilder UseVersionedSwagger(
        this IApplicationBuilder app,
        IApiVersionDescriptionProvider provider)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            // Build swagger endpoint for each API version
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    $"NumbatWallet API {description.GroupName}");
            }

            options.RoutePrefix = "api-docs";
            options.DocumentTitle = "NumbatWallet API Documentation";
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            options.EnableDeepLinking();
            options.EnableFilter();
            options.ShowExtensions();
            options.DisplayRequestDuration();
        });

        return app;
    }
}

/// <summary>
/// Operation filter to add version information to operations
/// </summary>
public class ApiVersionOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Add API version parameter
        var apiVersion = context.ApiDescription
            .ActionDescriptor.EndpointMetadata
            .OfType<ApiVersionAttribute>()
            .FirstOrDefault();

        if (apiVersion != null)
        {
            operation.Parameters ??= new List<OpenApiParameter>();

            // Add deprecated warning
            if (apiVersion.Versions.Any(v => v.Status == "Deprecated"))
            {
                operation.Deprecated = true;
                operation.Description = "**DEPRECATED:** " + operation.Description;
            }
        }

        // Add response headers
        operation.Responses.ToList().ForEach(response =>
        {
            response.Value.Headers ??= new Dictionary<string, OpenApiHeader>();
            response.Value.Headers.Add("X-API-Version", new OpenApiHeader
            {
                Description = "API version used",
                Schema = new OpenApiSchema { Type = "string" }
            });
            response.Value.Headers.Add("X-Deprecation-Warning", new OpenApiHeader
            {
                Description = "Deprecation warning if applicable",
                Schema = new OpenApiSchema { Type = "string" }
            });
        });
    }
}

/// <summary>
/// Schema filter to mark deprecated schemas
/// </summary>
public class DeprecatedSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var obsoleteAttribute = context.Type
            .GetCustomAttributes(typeof(ObsoleteAttribute), false)
            .FirstOrDefault() as ObsoleteAttribute;

        if (obsoleteAttribute != null)
        {
            schema.Deprecated = true;
            schema.Description = $"**DEPRECATED:** {obsoleteAttribute.Message}\n\n{schema.Description}";
        }
    }
}

/// <summary>
/// Versioning middleware for adding version headers
/// </summary>
public class ApiVersioningMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiVersioningMiddleware> _logger;

    public ApiVersioningMiddleware(
        RequestDelegate next,
        ILogger<ApiVersioningMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get requested version
        var apiVersion = context.GetRequestedApiVersion();

        // Add version to response headers
        if (apiVersion != null)
        {
            context.Response.Headers.Add("X-API-Version", apiVersion.ToString());

            // Add deprecation warning if needed
            if (apiVersion.Status == "Deprecated")
            {
                context.Response.Headers.Add("X-Deprecation-Warning",
                    $"API version {apiVersion} is deprecated and will be removed in future releases");

                // Also add Sunset header for deprecated versions
                var sunsetDate = DateTime.UtcNow.AddMonths(6); // 6 months notice
                context.Response.Headers.Add("Sunset", sunsetDate.ToString("R"));

                _logger.LogWarning(
                    "Deprecated API version {Version} called from {IP}",
                    apiVersion, context.Connection.RemoteIpAddress);
            }
        }

        // Add supported versions header
        var supportedVersions = GetSupportedVersions();
        context.Response.Headers.Add("X-Supported-Versions", string.Join(", ", supportedVersions));

        await _next(context);
    }

    private List<string> GetSupportedVersions()
    {
        return new List<string> { "1.0", "1.1", "2.0" };
    }
}

/// <summary>
/// API version constraint for routing
/// </summary>
public class ApiVersionRouteConstraint : IRouteConstraint
{
    private readonly string[] _validVersions = { "v1", "v1.1", "v2" };

    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        if (values.TryGetValue(routeKey, out var value))
        {
            var version = value?.ToString()?.ToLowerInvariant();
            return _validVersions.Contains(version);
        }

        return false;
    }
}

/// <summary>
/// Version-specific controller base
/// </summary>
[ApiController]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public abstract class VersionedApiControllerBase : ControllerBase
{
    protected ILogger Logger { get; }

    protected VersionedApiControllerBase(ILogger logger)
    {
        Logger = logger;
    }

    protected string GetApiVersion()
    {
        return HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
    }

    protected bool IsDeprecatedVersion()
    {
        var version = HttpContext.GetRequestedApiVersion();
        return version?.Status == "Deprecated";
    }

    protected IActionResult VersionedOk<T>(T data)
    {
        Response.Headers.Add("X-API-Version", GetApiVersion());
        return Ok(new VersionedResponse<T>
        {
            Version = GetApiVersion(),
            Data = data,
            Timestamp = DateTime.UtcNow
        });
    }

    protected IActionResult VersionedCreated<T>(string location, T data)
    {
        Response.Headers.Add("X-API-Version", GetApiVersion());
        return Created(location, new VersionedResponse<T>
        {
            Version = GetApiVersion(),
            Data = data,
            Timestamp = DateTime.UtcNow
        });
    }
}

/// <summary>
/// Versioned response wrapper
/// </summary>
public class VersionedResponse<T>
{
    public string Version { get; set; } = "1.0";
    public T Data { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public string? DeprecationWarning { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}