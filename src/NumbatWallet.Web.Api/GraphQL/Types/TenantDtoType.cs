using HotChocolate.Types;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Web.Api.GraphQL.Types;

/// <summary>
/// GraphQL object type for <see cref="TenantDto"/>.
/// SECURITY: excludes the per-tenant DB <c>ConnectionString</c> from the schema so it can
/// never be selected through the admin <c>tenants</c>/<c>tenant</c> queries (which project
/// TenantDto directly). The DTO keeps the model property for internal connection routing in
/// TenantService; this type simply removes it from the public API surface.
/// </summary>
public class TenantDtoType : ObjectType<TenantDto>
{
    protected override void Configure(IObjectTypeDescriptor<TenantDto> descriptor)
    {
        descriptor.Name("TenantDto");
        descriptor.Field(t => t.ConnectionString).Ignore();
    }
}
