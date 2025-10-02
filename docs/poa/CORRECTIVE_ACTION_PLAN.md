# NumbatWallet POA - Corrective Action Plan

**Date**: October 2, 2025
**Plan Version**: 1.0
**Status**: 🔴 **CRITICAL - IMMEDIATE ACTION REQUIRED**
**Total Estimated Effort**: 18-24 hours

---

## 🎯 OBJECTIVES

1. **Fix all critical blockers** identified in truthful assessment
2. **Achieve genuine production readiness** (not claimed readiness)
3. **Implement ALL security features** (not just middleware files)
4. **Implement ALL performance features** (distributed caching, response caching)
5. **Verify SDK integration** (end-to-end testing)
6. **Create truthful documentation** (what's actually working)

---

## 📋 PHASE 1: DATABASE - FIX MIGRATION DISASTER

**Priority**: 🔴 P0 - CRITICAL BLOCKER
**Estimated Time**: 4-6 hours
**Dependencies**: None

### 1.1 Create Missing Entity Configurations (2-3 hours)

#### Create TenantConfiguration.cs
```csharp
// File: /src/NumbatWallet.Infrastructure/Data/Configurations/TenantConfiguration.cs
public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Identifier)
            .HasColumnName("identifier")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(t => t.Identifier)
            .IsUnique();

        builder.Property(t => t.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(t => t.SubscriptionTier)
            .HasColumnName("subscription_tier")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(t => t.Settings)
            .HasColumnName("settings")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");
    }
}
```

#### Create OrganizationConfiguration.cs
```csharp
// Follow same pattern as TenantConfiguration
// Map all properties to snake_case columns
// Add indexes, foreign keys, constraints
```

#### Create All Missing Configurations (9 more)
1. ✅ TenantConfiguration
2. ✅ OrganizationConfiguration
3. ⏸️ CertificateAuthorityConfiguration
4. ⏸️ CertificateRevocationConfiguration
5. ⏸️ CertificateTrustStoreConfiguration
6. ⏸️ TenantCertificateConfiguration
7. ⏸️ IssuanceConfiguration
8. ⏸️ RevocationRegistryConfiguration
9. ⏸️ SupportedCredentialTypeConfiguration
10. ⏸️ CredentialSchemaConfiguration
11. ⏸️ WalletTemplateFieldConfiguration

### 1.2 Delete Manual Migration (5 minutes)

```bash
# Delete the manually created migration that EF Core doesn't recognize
rm /src/NumbatWallet.Infrastructure/Data/Migrations/20251002181803_CompleteInitialSchema.cs

# Delete outdated ModelSnapshot
rm /src/NumbatWallet.Infrastructure/Data/Migrations/NumbatWalletDbContextModelSnapshot.cs
```

### 1.3 Generate Proper Migration (30 minutes)

```bash
# Let EF Core generate migration from entity configurations
cd /src/NumbatWallet.Infrastructure

dotnet ef migrations add InitialSchema \
    --project ../NumbatWallet.Infrastructure \
    --startup-project ../NumbatWallet.Web.Api \
    --output-dir Data/Migrations

# This will:
# 1. Create migration with proper designer metadata
# 2. Create updated ModelSnapshot
# 3. Register migration in EF Core history
```

### 1.4 Verify Migration (30 minutes)

```bash
# Check migration was created
dotnet ef migrations list

# Should show:
# 20251002xxxxxx_InitialSchema (Pending)

# Apply migration to test database
dotnet ef database update

# Verify all 19 tables created
psql -d numbatwallet_test -c "\dt"

# Should show all 19 tables:
# - tenants
# - persons
# - wallets
# - credentials
# - issuers
# - wallet_templates
# - wallet_template_fields
# - tenant_certificates
# - certificate_authorities
# - certificate_trust_stores
# - certificate_revocations
# - issuances
# - revocation_registries
# - supported_credential_types
# - audit_logs
# - unmask_audits
# - admin_users
# - event_store
# - event_snapshots
```

### 1.5 Run Integration Tests (30 minutes)

```bash
# Verify database schema works with TestContainers
dotnet test src/Tests/NumbatWallet.Integration.Tests \
    --filter "Category=Database"

# All database tests should pass
```

---

## 📋 PHASE 2: SECURITY - IMPLEMENT ALL FEATURES

**Priority**: 🔴 P0 - CRITICAL BLOCKER
**Estimated Time**: 4-6 hours
**Dependencies**: None

### 2.1 Fix Authentication (Development vs Production) (1 hour)

#### Move TestAuthenticationHandler to Separate File

**File**: `/src/NumbatWallet.Web.Api/Authentication/TestAuthenticationHandler.cs`
```csharp
namespace NumbatWallet.Web.Api.Authentication;

/// <summary>
/// Test authentication handler for DEVELOPMENT ONLY
/// Validates real JWT tokens but provides fallback for testing
/// </summary>
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    // Move all code from Program.cs here
    // ... existing implementation
}
```

#### Update Program.cs

```csharp
// AUTHENTICATION: Environment-specific configuration
if (builder.Environment.IsDevelopment())
{
    // Development: Use test handler for easier testing
    builder.Services.AddAuthentication("Test")
        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
            "Test", options => { });

    Log.Information("Using TEST authentication handler (Development only)");
}
else
{
    // Production/Staging: Use real JWT Bearer authentication
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = builder.Configuration["Jwt:Authority"];
            options.Audience = builder.Configuration["Jwt:Audience"];
            options.RequireHttpsMetadata = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };
        });

    Log.Information("Using JWT Bearer authentication (Production)");
}
```

### 2.2 Implement Rate Limiting (2 hours)

#### Add to Program.cs (before `var app = builder.Build();`)

```csharp
// SECURITY: Rate Limiting (ASP.NET Core 9)
builder.Services.AddRateLimiter(options =>
{
    // Global rate limit: 100 requests per minute per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, partition =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            });
    });

    // Authentication endpoints: Stricter limits (5 attempts per 15 minutes)
    options.AddPolicy("Authentication", context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetSlidingWindowLimiter(ipAddress, partition =>
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                SegmentsPerWindow = 3,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    // API endpoints: Token bucket for burst handling
    options.AddPolicy("Api", context =>
    {
        var userId = context.User.Identity?.Name ?? "anonymous";

        return RateLimitPartition.GetTokenBucketLimiter(userId, partition =>
            new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1000,
                ReplenishmentPeriod = TimeSpan.FromHours(1),
                TokensPerPeriod = 500,
                QueueLimit = 100
            });
    });

    // Rejection response
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
        }

        context.HttpContext.Response.Headers.Append("X-RateLimit-Limit", context.Lease.ToString());
        context.HttpContext.Response.Headers.Append("X-RateLimit-Remaining", "0");

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            Error = "Too many requests",
            Message = "Rate limit exceeded. Please try again later.",
            RetryAfter = retryAfter?.TotalSeconds
        }, cancellationToken);
    };
});
```

#### Add to Pipeline (after `app.UseCors()`)

```csharp
// SECURITY: Use environment-specific CORS
app.UseCors(app.Environment.IsProduction() ? "Production" : "Development");

// SECURITY: Rate Limiting (MUST be after CORS, before authentication)
app.UseRateLimiter();

// PERFORMANCE: Output caching
app.UseOutputCache();
```

#### Apply to Controllers

```csharp
// In AuthenticationController.cs
[HttpPost("login")]
[RequireRateLimiting("Authentication")]
public async Task<IActionResult> Login([FromBody] LoginCommand command)
{
    // ...
}

// In all other API controllers
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[RequireRateLimiting("Api")]
public class CredentialController : ControllerBase
{
    // ...
}
```

### 2.3 Implement Input Sanitization (1 hour)

#### Wire Up Existing Middleware

```csharp
// In Program.cs pipeline (after security headers, before authentication)

// SECURITY: Add comprehensive security headers
app.UseMiddleware<SecurityHeadersMiddleware>();

// SECURITY: Input sanitization (validate content-type, check for injection attempts)
app.UseMiddleware<InputSanitizationMiddleware>();

// SECURITY: Add security audit logging for 401/403 responses
app.UseMiddleware<SecurityAuditMiddleware>();
```

---

## 📋 PHASE 3: PERFORMANCE - IMPLEMENT ALL CACHING

**Priority**: 🔴 P0 - CRITICAL BLOCKER
**Estimated Time**: 3-4 hours
**Dependencies**: None

### 3.1 Add Redis Distributed Caching (2 hours)

#### Add NuGet Package

```bash
dotnet add src/NumbatWallet.Web.Api package StackExchange.Redis
dotnet add src/NumbatWallet.Web.Api package Microsoft.Extensions.Caching.StackExchangeRedis
```

#### Configure in Program.cs

```csharp
// PERFORMANCE: Distributed Caching (Redis)
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "NumbatWallet:";
        options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
        {
            AbortOnConnectFail = false,
            ConnectTimeout = 5000,
            SyncTimeout = 5000,
            AsyncTimeout = 5000,
            ConnectRetry = 3,
            EndPoints = { redisConnectionString }
        };
    });

    Log.Information("Redis distributed caching enabled: {RedisConnection}",
        redisConnectionString);
}
else
{
    // Fallback to in-memory distributed cache for development
    builder.Services.AddDistributedMemoryCache();
    Log.Warning("Redis not configured. Using in-memory distributed cache.");
}
```

#### Add to appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=numbatwallet;Username=postgres;Password=...",
    "Redis": "localhost:6379"
  }
}
```

### 3.2 Add Response Caching Middleware (30 minutes)

#### Configure in Program.cs

```csharp
// PERFORMANCE: Response Caching
builder.Services.AddResponseCaching(options =>
{
    options.MaximumBodySize = 64 * 1024 * 1024; // 64 MB
    options.UseCaseSensitivePaths = true;
    options.SizeLimit = 100 * 1024 * 1024; // 100 MB total cache size
});
```

#### Add to Pipeline

```csharp
// PERFORMANCE: Response caching (MUST be before authentication)
app.UseResponseCaching();

// PERFORMANCE: Output caching (ASP.NET Core 9)
app.UseOutputCache();
```

#### Apply to Controllers

```csharp
// In controllers that return cacheable data
[HttpGet("{id}")]
[ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "id" })]
public async Task<IActionResult> GetWalletById(Guid id)
{
    // ...
}

// For frequently accessed but rarely changing data
[HttpGet]
[ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "page", "pageSize", "tenantId" })]
public async Task<IActionResult> GetWallets(int page = 1, int pageSize = 20)
{
    // ...
}
```

### 3.3 Implement Query Result Caching (1 hour)

#### Create CacheService

**File**: `/src/NumbatWallet.Infrastructure/Services/CacheService.cs`

```csharp
public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        // Try get from cache
        var cachedValue = await _cache.GetStringAsync(key, cancellationToken);

        if (!string.IsNullOrEmpty(cachedValue))
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
        }

        // Cache miss - execute factory
        _logger.LogDebug("Cache miss for key: {Key}", key);
        var value = await factory();

        // Store in cache
        var serialized = JsonSerializer.Serialize(value, _jsonOptions);
        await _cache.SetStringAsync(key, serialized, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        }, cancellationToken);

        return value;
    }
}
```

#### Use in Repositories

```csharp
public class PersonRepository : IPersonRepository
{
    private readonly ICacheService _cache;

    public async Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _cache.GetOrSetAsync(
            $"person:{id}",
            async () => await _dbContext.Persons.FindAsync(id),
            TimeSpan.FromMinutes(5),
            cancellationToken);
    }
}
```

---

## 📋 PHASE 4: SDK INTEGRATION VERIFICATION

**Priority**: 🟡 P1 - HIGH
**Estimated Time**: 4-6 hours
**Dependencies**: PHASE 1 complete

### 4.1 Export Backend GraphQL Schema (30 minutes)

```bash
# From Web.Api project
dotnet run --project src/NumbatWallet.Web.Api &
sleep 10

# Download schema
curl http://localhost:5000/graphql?sdl > schema.graphql

# Copy to SDK repo
cp schema.graphql /repo/NumbatWallet-sdks/numbatwallet-dotnet-sdk/schema.graphql
```

### 4.2 Fix SDK Compilation Errors (2-3 hours)

#### Add Missing Types to SDK

**File**: `/repo/NumbatWallet-sdks/numbatwallet-dotnet-sdk/src/NumbatWallet.Sdk/Models/ErrorCode.cs`

```csharp
namespace NumbatWallet.Sdk.Models;

public enum ErrorCode
{
    Unknown = 0,
    ValidationError = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    Conflict = 409,
    InternalServerError = 500
}
```

**File**: `/repo/NumbatWallet-sdks/numbatwallet-dotnet-sdk/src/NumbatWallet.Sdk/Models/PagedResult.cs`

```csharp
namespace NumbatWallet.Sdk.Models;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public PageInfo PageInfo { get; set; } = new();
    public int TotalCount { get; set; }
}
```

**File**: `/repo/NumbatWallet-sdks/numbatwallet-dotnet-sdk/src/NumbatWallet.Sdk/Models/PageInfo.cs`

```csharp
namespace NumbatWallet.Sdk.Models;

public class PageInfo
{
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public string? StartCursor { get; set; }
    public string? EndCursor { get; set; }
}
```

### 4.3 Run SDK Tests (1 hour)

```bash
cd /repo/NumbatWallet-sdks/numbatwallet-dotnet-sdk

# Rebuild SDK
dotnet build

# Run contract tests
dotnet test --filter "Category=Contract"

# Should pass all tests now
```

### 4.4 Integration Test SDK Against Backend (2 hours)

```bash
# Start backend
cd /repo/NumbatWallet
dotnet run --project src/NumbatWallet.Web.Api &

# Run SDK integration tests against running backend
cd /repo/NumbatWallet-sdks/numbatwallet-dotnet-sdk
dotnet test tests/NumbatWallet.Sdk.IntegrationTests

# Verify all queries/mutations work
```

---

## 📋 PHASE 5: ADMIN PORTAL - ENSURE FULLY FUNCTIONAL

**Priority**: 🟡 P1 - HIGH
**Estimated Time**: 2-3 hours
**Dependencies**: PHASE 1, 4 complete

### 5.1 Verify All GraphQL Queries Exist (1 hour)

#### Create Verification Script

```bash
# Extract all GraphQL operations from Admin portal
grep -r "UseQuery\|UseMutation" /src/NumbatWallet.Web.Admin/ -A 5 \
    > admin_graphql_operations.txt

# Compare against backend schema
# Ensure all queries/mutations in admin portal exist in backend
```

### 5.2 Test All Admin Pages (1 hour)

```bash
# Start Admin portal
dotnet run --project src/NumbatWallet.Web.Admin

# Manual testing checklist:
# - [ ] Dashboard loads with statistics
# - [ ] Tenants page loads and displays data
# - [ ] Wallets page loads and displays data
# - [ ] Credentials page loads and displays data
# - [ ] Audit logs page loads and displays data
# - [ ] Certificate management page loads
# - [ ] User management page loads
# - [ ] Reports page loads
# - [ ] Backup/restore page loads
# - [ ] Key rotation page loads
```

### 5.3 Fix Any Missing Endpoints (1 hour)

If any admin page fails, add missing GraphQL query/mutation to backend.

---

## 📋 PHASE 6: FINAL VERIFICATION

**Priority**: 🟡 P1 - HIGH
**Estimated Time**: 2-3 hours
**Dependencies**: ALL phases complete

### 6.1 Run All Tests (1 hour)

```bash
# Unit tests (should be 100%)
dotnet test --filter "Category=Unit"

# Integration tests (should be >80%)
dotnet test --filter "Category=Integration"

# SDK tests (should be 100%)
cd /repo/NumbatWallet-sdks/numbatwallet-dotnet-sdk
dotnet test
```

### 6.2 Build with Zero Tolerance (30 minutes)

```bash
# Build with warnings as errors
dotnet build -warnaserror

# Should have 0 errors, 0 warnings
```

### 6.3 Security Scan (30 minutes)

```bash
# Check for vulnerable packages
dotnet list package --vulnerable

# Should show no vulnerabilities
```

### 6.4 Create Truthful Documentation (1 hour)

Update all documentation to reflect ACTUAL state:
- What's ACTUALLY implemented
- What's NOT implemented
- Known limitations
- Required configuration

---

## 📊 SUCCESS CRITERIA

### PHASE 1: Database
- [ ] All 11 missing entity configurations created
- [ ] Manual migration deleted
- [ ] New migration generated via EF Core tools
- [ ] Migration applied successfully
- [ ] All 19 tables exist in database
- [ ] ModelSnapshot up to date
- [ ] `dotnet ef migrations list` shows migration
- [ ] Integration tests pass

### PHASE 2: Security
- [ ] Authentication uses environment-specific configuration
- [ ] Test handler ONLY in Development
- [ ] Rate limiting configured in Program.cs
- [ ] Rate limiting in pipeline
- [ ] Rate limiting policies applied to controllers
- [ ] Input sanitization middleware in pipeline
- [ ] Integration tests pass for rate limiting
- [ ] 429 response returned when limit exceeded

### PHASE 3: Performance
- [ ] Redis package installed
- [ ] Distributed caching configured
- [ ] Response caching configured
- [ ] Query result caching implemented
- [ ] All caching middleware in pipeline
- [ ] Cache hit/miss logged
- [ ] Performance tests show improvement

### PHASE 4: SDK Integration
- [ ] GraphQL schema exported
- [ ] Missing types added to SDK
- [ ] SDK compiles without errors
- [ ] SDK contract tests pass
- [ ] SDK integration tests pass against backend
- [ ] All queries/mutations verified

### PHASE 5: Admin Portal
- [ ] All pages load without errors
- [ ] All GraphQL queries return data
- [ ] No missing endpoints
- [ ] Dashboard shows real statistics
- [ ] CRUD operations work

### PHASE 6: Final Verification
- [ ] 100% unit tests passing
- [ ] >80% integration tests passing
- [ ] 100% SDK tests passing
- [ ] 0 build errors
- [ ] 0 build warnings
- [ ] 0 vulnerable packages
- [ ] Documentation truthful and complete

---

## 📈 PROGRESS TRACKING

### Hours Estimated: 18-24 hours
### Hours Actual: _____

### Blockers Resolved:
- [ ] Migration not applied
- [ ] Missing entity configurations
- [ ] Rate limiting not configured
- [ ] Distributed caching not configured
- [ ] SDK compilation errors

### Completion Percentage:
- Phase 1: ___% (Target: 100%)
- Phase 2: ___% (Target: 100%)
- Phase 3: ___% (Target: 100%)
- Phase 4: ___% (Target: 100%)
- Phase 5: ___% (Target: 100%)
- Phase 6: ___% (Target: 100%)

**OVERALL: ___% (Target: 100%)**

---

## 🚦 GO/NO-GO DECISION

### Production Deployment Readiness

**GO Criteria** (ALL must be TRUE):
- [ ] All 6 phases 100% complete
- [ ] All unit tests passing (100%)
- [ ] All integration tests passing (>80%)
- [ ] All SDK tests passing (100%)
- [ ] Zero build errors
- [ ] Zero build warnings
- [ ] Zero vulnerable packages
- [ ] All security features implemented AND tested
- [ ] All performance features implemented AND tested
- [ ] Admin portal fully functional
- [ ] Truthful documentation complete

**Current Status**: NO-GO ❌

**Recommendation**: Complete all 6 phases before reconsidering deployment.

---

**Plan Created**: October 2, 2025
**Plan Owner**: Development Team
**Review Date**: Upon completion of all phases
**Approval Required**: Technical Lead + Product Owner
