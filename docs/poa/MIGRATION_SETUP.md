# Database Migration Setup - Production Ready

**Date**: October 30, 2025
**Status**: ✅ COMPLETE
**Database**: PostgreSQL 17 via Aspire

## Summary

Successfully resolved EF Core migration blocker and created production-ready database migrations. The database now has proper version control and repeatability through EF Core migrations.

## Issues Resolved

### 1. EF Core Design Roslyn Dependency Conflict

**Problem**: `Microsoft.EntityFrameworkCore.Design 9.0.9` had conflicting Roslyn dependencies preventing migration generation.

**Error**:
```
System.TypeLoadException: Method 'get_IsParamsArray' in type 'Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationParameterSymbol'
from assembly 'Microsoft.CodeAnalysis.Workspaces, Version=4.8.0.0' does not have an implementation.
```

**Root Cause**: EF Core Design required both:
- `Microsoft.CodeAnalysis.CSharp 4.11.0`
- `Microsoft.CodeAnalysis.CSharp.Workspaces 4.8.0`

These versions were incompatible, causing TypeLoadException during migration scaffolding.

**Solution**:

1. Updated `dotnet-ef` tool: 9.0.9 → 9.0.10
```bash
dotnet tool update --global dotnet-ef
```

2. Added version overrides to `Directory.Packages.props`:
```xml
<!-- Roslyn - force 4.11.0 to resolve EF Core Design dependency conflict -->
<PackageVersion Include="Microsoft.CodeAnalysis.Common" Version="4.11.0" />
<PackageVersion Include="Microsoft.CodeAnalysis.CSharp" Version="4.11.0" />
<PackageVersion Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="4.11.0" />
<PackageVersion Include="Microsoft.CodeAnalysis.Workspaces.Common" Version="4.11.0" />
```

3. Added explicit references to `NumbatWallet.Web.Api.csproj`:
```xml
<!-- Roslyn packages explicitly referenced to resolve EF Core Design 9.0.9 version conflict -->
<PackageReference Include="Microsoft.CodeAnalysis.Common" />
<PackageReference Include="Microsoft.CodeAnalysis.CSharp" />
<PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" />
<PackageReference Include="Microsoft.CodeAnalysis.Workspaces.Common" />
```

## Migrations Created

### InitialCreate (20251030023343)
- **Size**: 1,062 lines
- **Tables**: 21 tables
- **Indexes**: ~85 indexes
- **Status**: ✅ Applied to database

### Tables Created:
1. admin_users - Admin user accounts
2. audit_logs - Audit trail
3. CertificateAuthorities - PKI trust anchors
4. CertificateRevocations - CRL entries
5. CertificateTrustStores - Trusted certificates
6. Credentials - Verifiable credentials (CORE)
7. CredentialSchemas - Credential schemas
8. EventSnapshots - Event sourcing snapshots
9. EventStore - Event sourcing store
10. issuances - Issuance records
11. Issuers - Credential issuers
12. Organizations - Organizations
13. Persons - Person identities (CORE)
14. RevocationRegistries - Revocation lists
15. SupportedCredentialTypes - Supported types
16. TenantCertificates - Tenant-specific certs
17. Tenants - Multi-tenancy (CORE)
18. unmask_audits - Data unmask audit
19. Wallets - Digital wallets (CORE)
20. WalletTemplateFields - Template fields
21. WalletTemplates - Wallet templates

### SeedTestData (20251030024416)
- **Status**: Created but not applied
- **Reason**: Complex JSONB encrypted fields require application-layer encryption
- **Recommendation**: Add test data via API after services are running

## Migration Commands

### Generate Migration
```bash
cd src/NumbatWallet.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../NumbatWallet.Web.Api/NumbatWallet.Web.Api.csproj
```

### Apply Migration (Development)
```bash
# Get PostgreSQL password from Aspire container
docker inspect postgres-<container-id> | grep POSTGRES_PASSWORD

# Apply migration
dotnet ef database update \
  --connection "Host=localhost;Port=<port>;Database=numbatwallet;Username=postgres;Password='<password>'" \
  --startup-project ../NumbatWallet.Web.Api/NumbatWallet.Web.Api.csproj
```

### List Migrations
```bash
dotnet ef migrations list \
  --connection "Host=localhost;Port=<port>;Database=numbatwallet;Username=postgres;Password='<password>'" \
  --startup-project ../NumbatWallet.Web.Api/NumbatWallet.Web.Api.csproj
```

### Rollback Migration
```bash
dotnet ef database update <PreviousMigrationName> \
  --connection "Host=localhost;Port=<port>;Database=numbatwallet;Username=postgres;Password='<password>'" \
  --startup-project ../NumbatWallet.Web.Api/NumbatWallet.Web.Api.csproj
```

## Connection String Configuration

### Development (Aspire)
Aspire injects PostgreSQL connection at runtime via service discovery:
- Connection name: `numbatwallet` (matches `AddDatabase("numbatwallet")` in AppHost)
- Runtime injection: Aspire provides connection string automatically
- Port: Dynamic (check with `docker ps`)

### Design-Time (Migrations)
Created `appsettings.Development.json` in Infrastructure project:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=51942;Database=numbatwallet;Username=postgres;Password=xxx"
  }
}
```

**Note**: The `DesignTimeDbContextFactory` uses this for migrations, but connection string parameter is more reliable.

## Database Schema Notes

### Protected Data (JSONB Encryption)
Several tables use JSONB columns for encrypted/protected fields:
- **Persons**: Email, first_name, last_name, date_of_birth
- **Tenants**: settings

These fields require application-layer encryption and cannot be easily seeded via raw SQL.

### Naming Convention
- **PostgreSQL**: snake_case (e.g., `created_at`, `tenant_id`)
- **C# Entities**: PascalCase (e.g., `CreatedAt`, `TenantId`)
- **EF Core**: Configured via `UseSnakeCaseNamingConvention()`

## Files Modified

### Infrastructure
- `src/NumbatWallet.Infrastructure/appsettings.Development.json` (NEW)
- `src/NumbatWallet.Infrastructure/Migrations/20251030023343_InitialCreate.cs` (NEW)
- `src/NumbatWallet.Infrastructure/Migrations/20251030023343_InitialCreate.Designer.cs` (NEW)
- `src/NumbatWallet.Infrastructure/Migrations/20251030024416_SeedTestData.cs` (NEW)
- `src/NumbatWallet.Infrastructure/Migrations/20251030024416_SeedTestData.Designer.cs` (NEW)

### Configuration
- `Directory.Packages.props` - Added Roslyn version overrides
- `src/NumbatWallet.Web.Api/NumbatWallet.Web.Api.csproj` - Added Roslyn references

## Production Readiness

### ✅ Completed
- [x] EF Core migrations working
- [x] Database schema version controlled
- [x] Repeatable deployment process
- [x] PostgreSQL configuration
- [x] Migration history tracking

### 📋 Recommendations
1. **Seed Data**: Add via API or admin portal after deployment
2. **Connection Strings**: Use Azure Key Vault for production passwords
3. **Backup Strategy**: Implement automated PostgreSQL backups
4. **Migration CI/CD**: Add migration step to deployment pipeline
5. **Monitoring**: Add migration failure alerts

## Next Steps - Phase 1 Remaining

1. **Phase 1.3**: Azure Key Vault Integration
   - Replace MockKeyVaultService
   - Migrate secrets from configuration
   - Configure managed identity

2. **Phase 1.4**: Azure Blob Storage Integration
   - Replace MockBlobStorageService
   - Configure containers
   - Test upload/download

3. **Seed Data**: Add test data via:
   - Admin portal UI (preferred)
   - API endpoints
   - Or create simple migration script

## Troubleshooting

### Issue: "Couldn't set data source"
**Solution**: Use `--connection` parameter instead of relying on appsettings.json

### Issue: "No migrations were found"
**Solution**: Ensure project is built: `dotnet build -warnaserror`

### Issue: JSONB validation errors during seed
**Solution**: Use application APIs for adding data with encrypted fields

## References
- EF Core Migrations: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- Npgsql EF Core: https://www.npgsql.org/efcore/
- Aspire Service Discovery: https://learn.microsoft.com/en-us/dotnet/aspire/service-discovery/overview
