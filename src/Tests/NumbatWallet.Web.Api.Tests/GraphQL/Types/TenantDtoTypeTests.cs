using HotChocolate;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Web.Api.GraphQL.Types;

namespace NumbatWallet.Web.Api.Tests.GraphQL.Types;

/// <summary>
/// SECURITY regression tests: the per-tenant DB connection string must never be exposed on
/// the GraphQL <c>TenantDto</c> type. <see cref="TenantDtoType"/> Ignore()s it.
/// </summary>
public class TenantDtoTypeTests
{
    private static async Task<ISchema> BuildSchemaAsync()
    {
        return await new ServiceCollection()
            .AddGraphQLServer()
            .AddType<TenantDtoType>()
            .AddQueryType<TenantProbeQuery>()
            .BuildSchemaAsync();
    }

    // Minimal query root that references TenantDto so the type is bound into the schema.
    public class TenantProbeQuery
    {
        public TenantDto GetTenant() => new();
    }

    [Fact]
    public async Task TenantDtoType_DoesNotExpose_ConnectionString()
    {
        var schema = await BuildSchemaAsync();
        var type = schema.Types.OfType<HotChocolate.Types.ObjectType>()
            .Single(t => t.Name == "TenantDto");

        type.Fields.Any(f => string.Equals(f.Name, "connectionString", StringComparison.OrdinalIgnoreCase))
            .Should().BeFalse("the DB connection string must never be reachable through GraphQL");
    }

    [Fact]
    public async Task TenantDtoType_StillExposes_SafeFields()
    {
        var schema = await BuildSchemaAsync();
        var type = schema.Types.OfType<HotChocolate.Types.ObjectType>()
            .Single(t => t.Name == "TenantDto");

        type.Fields.Any(f => string.Equals(f.Name, "name", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
        type.Fields.Any(f => string.Equals(f.Name, "identifier", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
    }
}
