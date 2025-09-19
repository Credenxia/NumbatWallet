using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace NumbatWallet.Web.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            // Add API versions
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "NumbatWallet API",
                Description = "Western Australian Government Digital Identity Wallet API",
                TermsOfService = new Uri("https://www.wa.gov.au/terms"),
                Contact = new OpenApiContact
                {
                    Name = "WA Government Digital Services",
                    Email = "support@numbatwallet.wa.gov.au",
                    Url = new Uri("https://www.wa.gov.au/digital-services")
                },
                License = new OpenApiLicense
                {
                    Name = "Government License",
                    Url = new Uri("https://www.wa.gov.au/license")
                }
            });

            // Add security definitions
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token."
            });

            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Name = "X-API-Key",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "API Key authentication for service-to-service communication"
            });

            // Add OAuth2 for Azure AD
            options.AddSecurityDefinition("OAuth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri("https://login.microsoftonline.com/common/oauth2/v2.0/authorize"),
                        TokenUrl = new Uri("https://login.microsoftonline.com/common/oauth2/v2.0/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            ["api://numbatwallet/.default"] = "Access NumbatWallet API"
                        }
                    }
                }
            });

            // Add security requirement
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

            // Include XML comments
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFilename);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Add custom schema IDs to avoid conflicts
            options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

            // Add operation filters
            options.OperationFilter<SwaggerDefaultValues>();
            options.OperationFilter<AuthorizeCheckOperationFilter>();

            // Document filters
            options.DocumentFilter<LowercaseDocumentFilter>();

            // Enable annotations
            options.EnableAnnotations();
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerDocumentation(
        this IApplicationBuilder app,
        IApiVersionDescriptionProvider? provider = null)
    {
        app.UseSwagger(options =>
        {
            options.RouteTemplate = "api-docs/{documentName}/swagger.json";
            options.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
            {
                swaggerDoc.Servers = new List<OpenApiServer>
                {
                    new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" }
                };
            });
        });

        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = "api-docs";
            options.DocumentTitle = "NumbatWallet API Documentation";

            // Add endpoints for each API version
            if (provider != null)
            {
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint(
                        $"/api-docs/{description.GroupName}/swagger.json",
                        $"NumbatWallet API {description.GroupName.ToUpperInvariant()}");
                }
            }
            else
            {
                options.SwaggerEndpoint("/api-docs/v1/swagger.json", "NumbatWallet API V1");
            }

            // UI customizations
            options.DefaultModelsExpandDepth(-1); // Hide models section by default
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
            options.ShowExtensions();
            options.EnableValidator();

            // Add custom CSS
            options.InjectStylesheet("/swagger-ui/custom.css");

            // OAuth2 configuration
            options.OAuthClientId("numbatwallet-swagger");
            options.OAuthAppName("NumbatWallet API - Swagger");
            options.OAuthUsePkce();
        });

        // Redirect root to API docs
        app.Use(async (context, next) =>
        {
            if (context.Request.Path == "/")
            {
                context.Response.Redirect("/api-docs");
                return;
            }
            await next();
        });

        return app;
    }
}

public class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        operation.Deprecated |= apiDescription.IsDeprecated();

        foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
        {
            var responseKey = responseType.IsDefaultResponse ? "default" : responseType.StatusCode.ToString();
            var response = operation.Responses[responseKey];

            foreach (var contentType in response.Content.Keys)
            {
                if (responseType.ApiResponseFormats.All(x => x.MediaType != contentType))
                {
                    response.Content.Remove(contentType);
                }
            }
        }

        if (operation.Parameters == null)
        {
            return;
        }

        foreach (var parameter in operation.Parameters)
        {
            var description = apiDescription.ParameterDescriptions.First(p => p.Name == parameter.Name);

            parameter.Description ??= description.ModelMetadata?.Description;

            if (parameter.Schema.Default == null && description.DefaultValue != null)
            {
                // Set default value directly as a primitive type
                var defaultJson = System.Text.Json.JsonSerializer.Serialize(description.DefaultValue);
                parameter.Schema.Default = new Microsoft.OpenApi.Any.OpenApiString(defaultJson);
            }

            parameter.Required |= description.IsRequired;
        }
    }
}

public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAuthorize = context.MethodInfo.DeclaringType != null &&
            (context.MethodInfo.DeclaringType.GetCustomAttributes(true).OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Any() ||
             context.MethodInfo.GetCustomAttributes(true).OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Any());

        var hasAllowAnonymous = context.MethodInfo.GetCustomAttributes(true).OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>().Any();

        if (hasAuthorize && !hasAllowAnonymous)
        {
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });
        }
    }
}

public class LowercaseDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var paths = swaggerDoc.Paths.ToList();
        swaggerDoc.Paths.Clear();

        foreach (var path in paths)
        {
            swaggerDoc.Paths.Add(path.Key.ToLowerInvariant(), path.Value);
        }
    }
}