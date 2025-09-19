using Carter;
using NumbatWallet.Web.Api.Rest.Common;

namespace NumbatWallet.Web.Api.Rest.Modules;

/// <summary>
/// Legacy compatibility endpoints (deprecated, use GraphQL instead)
/// </summary>
public class LegacyModule : RestEndpointBase
{
    public override string RoutePrefix => "/api/legacy";

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix)
            .WithTags("Legacy (Deprecated)")
            .RequireAuthorization();

        // Legacy endpoints redirect to GraphQL with deprecation notice
        group.MapGet("/{**path}", HandleLegacyRequest)
            .WithName("LegacyRedirect")
            .WithOpenApi()
            .Produces(301);

        group.MapPost("/{**path}", HandleLegacyRequest)
            .WithName("LegacyPostRedirect")
            .WithOpenApi()
            .Produces(301);

        group.MapPut("/{**path}", HandleLegacyRequest)
            .WithName("LegacyPutRedirect")
            .WithOpenApi()
            .Produces(301);

        group.MapDelete("/{**path}", HandleLegacyRequest)
            .WithName("LegacyDeleteRedirect")
            .WithOpenApi()
            .Produces(301);
    }

    private static IResult HandleLegacyRequest(string path, HttpContext context)
    {
        var response = new
        {
            deprecated = true,
            message = "This legacy endpoint is deprecated. Please use the GraphQL API instead.",
            graphqlEndpoint = "/graphql",
            documentation = "/graphql",
            deprecationDate = "2025-01-01",
            removalDate = "2025-07-01",
            requestedPath = path,
            alternativeEndpoints = GetAlternativeEndpoints(path)
        };

        // Add deprecation headers
        context.Response.Headers.Append("X-Deprecated", "true");
        context.Response.Headers.Append("X-Deprecation-Date", "2025-01-01");
        context.Response.Headers.Append("X-Sunset", "2025-07-01");
        context.Response.Headers.Append("Link", "</graphql>; rel=\"alternate\"; type=\"application/graphql\"");

        return Results.Json(response, statusCode: 301);
    }

    private static Dictionary<string, string> GetAlternativeEndpoints(string path)
    {
        // Map common legacy paths to GraphQL queries
        var mappings = new Dictionary<string, string>();

        if (path.Contains("wallet", StringComparison.OrdinalIgnoreCase))
        {
            mappings["graphql_query"] = @"
                query GetWallet($id: ID!) {
                    walletById(id: $id) {
                        id
                        personName
                        name
                        status
                    }
                }";
        }
        else if (path.Contains("credential", StringComparison.OrdinalIgnoreCase))
        {
            mappings["graphql_query"] = @"
                query GetCredential($id: ID!) {
                    credentialById(id: $id) {
                        id
                        type
                        status
                        issuanceDate
                    }
                }";
        }
        else if (path.Contains("person", StringComparison.OrdinalIgnoreCase))
        {
            mappings["graphql_query"] = @"
                query GetPerson($id: ID!) {
                    personById(id: $id) {
                        id
                        email
                        firstName
                        lastName
                    }
                }";
        }

        mappings["documentation"] = "https://docs.numbatwallet.wa.gov.au/api/graphql";
        mappings["migration_guide"] = "https://docs.numbatwallet.wa.gov.au/api/migration";

        return mappings;
    }
}