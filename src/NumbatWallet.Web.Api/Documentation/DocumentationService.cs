using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Xml.Linq;
using System.Xml.XPath;

namespace NumbatWallet.Web.Api.Documentation;

/// <summary>
/// Service for generating API documentation
/// </summary>
public interface IDocumentationService
{
    string GenerateMarkdown();
    string GenerateHtml();
    Task<byte[]> GeneratePdfAsync();
}

/// <summary>
/// Implementation of documentation service
/// </summary>
public class DocumentationService : IDocumentationService
{
    private readonly ILogger<DocumentationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public DocumentationService(
        ILogger<DocumentationService> logger,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _configuration = configuration;
        _environment = environment;
    }

    public string GenerateMarkdown()
    {
        var markdown = new System.Text.StringBuilder();

        markdown.AppendLine("# NumbatWallet API Documentation");
        markdown.AppendLine();
        markdown.AppendLine($"**Version:** {_configuration["ApiVersion"] ?? "1.0.0"}");
        markdown.AppendLine($"**Environment:** {_environment.EnvironmentName}");
        markdown.AppendLine($"**Generated:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        markdown.AppendLine();

        markdown.AppendLine("## Overview");
        markdown.AppendLine();
        markdown.AppendLine("NumbatWallet is a digital identity wallet solution that enables secure storage, management, and verification of digital credentials.");
        markdown.AppendLine();

        markdown.AppendLine("## Base URL");
        markdown.AppendLine();
        markdown.AppendLine("```");
        markdown.AppendLine($"https://api.numbatwallet.wa.gov.au/api/v1");
        markdown.AppendLine("```");
        markdown.AppendLine();

        markdown.AppendLine("## Authentication");
        markdown.AppendLine();
        markdown.AppendLine("This API uses OAuth 2.0 with Azure Active Directory for authentication.");
        markdown.AppendLine();
        markdown.AppendLine("### Required Scopes");
        markdown.AppendLine();
        markdown.AppendLine("- `api://numbatwallet/read` - Read access to credentials");
        markdown.AppendLine("- `api://numbatwallet/write` - Write access to credentials");
        markdown.AppendLine("- `api://numbatwallet/admin` - Administrative access");
        markdown.AppendLine();

        markdown.AppendLine("## Rate Limiting");
        markdown.AppendLine();
        markdown.AppendLine("The API implements rate limiting to ensure fair usage:");
        markdown.AppendLine();
        markdown.AppendLine("- **Anonymous:** 100 requests per minute");
        markdown.AppendLine("- **Authenticated:** 1000 requests per minute");
        markdown.AppendLine("- **Premium:** 5000 requests per minute");
        markdown.AppendLine();

        markdown.AppendLine("## Endpoints");
        markdown.AppendLine();
        GenerateEndpointDocumentation(markdown);

        markdown.AppendLine("## Error Codes");
        markdown.AppendLine();
        GenerateErrorCodeDocumentation(markdown);

        markdown.AppendLine("## Webhooks");
        markdown.AppendLine();
        GenerateWebhookDocumentation(markdown);

        return markdown.ToString();
    }

    public string GenerateHtml()
    {
        var markdown = GenerateMarkdown();

        // Convert markdown to HTML using a simple converter
        var html = new System.Text.StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"UTF-8\">");
        html.AppendLine("    <title>NumbatWallet API Documentation</title>");
        html.AppendLine("    <style>");
        html.AppendLine("        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 1200px; margin: 0 auto; padding: 20px; }");
        html.AppendLine("        h1 { color: #2c3e50; border-bottom: 3px solid #3498db; padding-bottom: 10px; }");
        html.AppendLine("        h2 { color: #34495e; margin-top: 30px; }");
        html.AppendLine("        h3 { color: #7f8c8d; }");
        html.AppendLine("        code { background: #f4f4f4; padding: 2px 6px; border-radius: 3px; }");
        html.AppendLine("        pre { background: #f4f4f4; padding: 15px; border-radius: 5px; overflow-x: auto; }");
        html.AppendLine("        table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
        html.AppendLine("        th, td { padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }");
        html.AppendLine("        th { background-color: #3498db; color: white; }");
        html.AppendLine("        .endpoint { background: #ecf0f1; padding: 10px; margin: 10px 0; border-left: 4px solid #3498db; }");
        html.AppendLine("        .method { display: inline-block; padding: 3px 8px; border-radius: 3px; font-weight: bold; margin-right: 10px; }");
        html.AppendLine("        .method-get { background: #27ae60; color: white; }");
        html.AppendLine("        .method-post { background: #3498db; color: white; }");
        html.AppendLine("        .method-put { background: #f39c12; color: white; }");
        html.AppendLine("        .method-delete { background: #e74c3c; color: white; }");
        html.AppendLine("        .method-patch { background: #9b59b6; color: white; }");
        html.AppendLine("    </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");

        // Simple markdown to HTML conversion (very basic)
        var lines = markdown.Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                html.AppendLine($"<h1>{line[2..]}</h1>");
            }
            else if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                html.AppendLine($"<h2>{line[3..]}</h2>");
            }
            else if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                html.AppendLine($"<h3>{line[4..]}</h3>");
            }
            else if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                html.AppendLine($"<li>{line[2..]}</li>");
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                html.AppendLine($"<p>{line}</p>");
            }
        }

        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    public async Task<byte[]> GeneratePdfAsync()
    {
        // For PDF generation, we would typically use a library like iTextSharp or QuestPDF
        // For now, return a placeholder
        var html = GenerateHtml();
        var pdfBytes = System.Text.Encoding.UTF8.GetBytes($"PDF Generation Not Implemented\n\n{GenerateMarkdown()}");

        _logger.LogInformation("PDF documentation generated with {Size} bytes", pdfBytes.Length);

        return await Task.FromResult(pdfBytes);
    }

    private void GenerateEndpointDocumentation(System.Text.StringBuilder markdown)
    {
        markdown.AppendLine("### Credential Endpoints");
        markdown.AppendLine();
        markdown.AppendLine("#### Issue Credential");
        markdown.AppendLine("```http");
        markdown.AppendLine("POST /api/v1/credentials/issue");
        markdown.AppendLine("```");
        markdown.AppendLine();

        markdown.AppendLine("#### Verify Credential");
        markdown.AppendLine("```http");
        markdown.AppendLine("POST /api/v1/credentials/verify");
        markdown.AppendLine("```");
        markdown.AppendLine();

        markdown.AppendLine("#### Revoke Credential");
        markdown.AppendLine("```http");
        markdown.AppendLine("POST /api/v1/credentials/{id}/revoke");
        markdown.AppendLine("```");
        markdown.AppendLine();

        markdown.AppendLine("### Wallet Endpoints");
        markdown.AppendLine();
        markdown.AppendLine("#### Create Wallet");
        markdown.AppendLine("```http");
        markdown.AppendLine("POST /api/v1/wallets");
        markdown.AppendLine("```");
        markdown.AppendLine();

        markdown.AppendLine("#### Get Wallet");
        markdown.AppendLine("```http");
        markdown.AppendLine("GET /api/v1/wallets/{id}");
        markdown.AppendLine("```");
        markdown.AppendLine();
    }

    private void GenerateErrorCodeDocumentation(System.Text.StringBuilder markdown)
    {
        markdown.AppendLine("| Code | Description |");
        markdown.AppendLine("|------|-------------|");
        markdown.AppendLine("| 400 | Bad Request - Invalid input parameters |");
        markdown.AppendLine("| 401 | Unauthorized - Invalid or missing authentication |");
        markdown.AppendLine("| 403 | Forbidden - Insufficient permissions |");
        markdown.AppendLine("| 404 | Not Found - Resource does not exist |");
        markdown.AppendLine("| 409 | Conflict - Resource already exists |");
        markdown.AppendLine("| 429 | Too Many Requests - Rate limit exceeded |");
        markdown.AppendLine("| 500 | Internal Server Error |");
        markdown.AppendLine("| 503 | Service Unavailable |");
        markdown.AppendLine();
    }

    private void GenerateWebhookDocumentation(System.Text.StringBuilder markdown)
    {
        markdown.AppendLine("### Supported Events");
        markdown.AppendLine();
        markdown.AppendLine("- `credential.issued` - Fired when a credential is issued");
        markdown.AppendLine("- `credential.verified` - Fired when a credential is verified");
        markdown.AppendLine("- `credential.revoked` - Fired when a credential is revoked");
        markdown.AppendLine("- `wallet.created` - Fired when a wallet is created");
        markdown.AppendLine("- `wallet.updated` - Fired when a wallet is updated");
        markdown.AppendLine();

        markdown.AppendLine("### Webhook Payload");
        markdown.AppendLine();
        markdown.AppendLine("```json");
        markdown.AppendLine("{");
        markdown.AppendLine("  \"event\": \"credential.issued\",");
        markdown.AppendLine("  \"timestamp\": \"2024-01-01T00:00:00Z\",");
        markdown.AppendLine("  \"data\": {");
        markdown.AppendLine("    \"credentialId\": \"abc123\",");
        markdown.AppendLine("    \"holderId\": \"user123\",");
        markdown.AppendLine("    \"type\": \"ProofOfAge\"");
        markdown.AppendLine("  }");
        markdown.AppendLine("}");
        markdown.AppendLine("```");
        markdown.AppendLine();
    }
}

/// <summary>
/// Custom OpenAPI operation filter for documentation
/// </summary>
public class ApiDocumentationOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Add default responses
        operation.Responses?.TryAdd("401", new OpenApiResponse
        {
            Description = "Unauthorized - Invalid or missing authentication"
        });

        operation.Responses?.TryAdd("403", new OpenApiResponse
        {
            Description = "Forbidden - Insufficient permissions"
        });

        operation.Responses?.TryAdd("429", new OpenApiResponse
        {
            Description = "Too Many Requests - Rate limit exceeded"
        });

        operation.Responses?.TryAdd("500", new OpenApiResponse
        {
            Description = "Internal Server Error"
        });

        // Add operation ID if not present
        if (string.IsNullOrEmpty(operation.OperationId))
        {
            operation.OperationId = context.MethodInfo.Name;
        }

        // Add tags based on controller name
        if (context.ApiDescription.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor controllerDescriptor)
        {
            var controllerName = controllerDescriptor.ControllerName;
            operation.Tags ??= new HashSet<OpenApiTagReference>();
            operation.Tags.Add(new OpenApiTagReference(controllerName));
        }

        // Add security requirements for authorized endpoints
        var hasAuthorize = context.MethodInfo.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true).Any() ||
                          context.MethodInfo.DeclaringType?.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true).Any() == true;

        if (hasAuthorize)
        {
            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("Bearer"),
                        new List<string>()
                    }
                }
            ];
        }
    }
}

/// <summary>
/// Custom schema filter for better documentation
/// </summary>
public class ApiDocumentationSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        // Add descriptions from XML comments
        if (context.Type is null) return;

        var xmlDoc = GetXmlDocumentation(context.Type);
        if (!string.IsNullOrEmpty(xmlDoc))
        {
            schema.Description = xmlDoc;
        }
    }

    private static string? GetXmlDocumentation(Type type)
    {
        try
        {
            var xmlFile = $"{type.Assembly.GetName().Name}.xml";
            var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);

            if (File.Exists(xmlPath))
            {
                var doc = XDocument.Load(xmlPath);
                var memberName = $"T:{type.FullName}";
                var member = doc.XPathSelectElement($"//member[@name='{memberName}']/summary");

                return member?.Value?.Trim();
            }
        }
        catch
        {
            // Ignore XML parsing errors
        }

        return null;
    }
}