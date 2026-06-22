# NumbatWallet SDK - Breaking Changes Report

**Date**: October 3, 2025
**Backend Commit**: `2366746`
**Branch**: `feature/POA-backend-foundation`
**Status**: 🔴 **URGENT - ACTION REQUIRED**

---

## 📋 Executive Summary

The NumbatWallet backend GraphQL API has been updated with **breaking changes** to support HotChocolate v15 type system limitations. The SDK must be regenerated and updated to handle new INPUT type signatures.

**Impact**: 66 SDK integration tests are currently failing due to:
- Changed mutation input signatures (Dictionary → JSON string)
- Missing exception types (ErrorCode, RateLimitExceededException, etc.)
- Missing pagination types (PagedResult, PageInfo, CursorEdge)

**Timeline**: **Immediate action required** before SDK can interact with backend.

---

## 🔴 Breaking Changes

### 1. Mutation Input Signatures Changed

#### IssueCredential Mutation

**❌ OLD (No longer supported)**:
```graphql
input IssueCredentialInput {
  walletId: UUID!
  credentialType: CredentialType!
  subject: String!
  claims: Dictionary<String, Object>!  # ❌ NOT SUPPORTED in HotChocolate v15
  validFrom: DateTime
  validUntil: DateTime
  issuerOrganizationId: UUID!
}
```

**✅ NEW (Current)**:
```graphql
input IssueCredentialInput {
  walletId: UUID!
  credentialType: CredentialType!
  subject: String!
  claimsJson: String!  # ✅ JSON string instead of Dictionary
  validFrom: DateTime
  validUntil: DateTime
  issuerOrganizationId: UUID!
}
```

#### BulkIssueCredentials Mutation

**❌ OLD**:
```graphql
input BulkIssueCredentialsInput {
  walletIds: [UUID!]!
  credentialType: CredentialType!
  template: Dictionary<String, Object>!  # ❌ NOT SUPPORTED
  issuerOrganizationId: UUID!
  validFrom: DateTime
  validUntil: DateTime
}
```

**✅ NEW**:
```graphql
input BulkIssueCredentialsInput {
  walletIds: [UUID!]!
  credentialType: CredentialType!
  templateJson: String!  # ✅ JSON string
  issuerOrganizationId: UUID!
  validFrom: DateTime
  validUntil: DateTime
}
```

#### CreateIssuance Mutation

**❌ OLD**:
```graphql
input CreateIssuanceInput {
  credentialType: String!
  walletId: UUID!
  requiredDocuments: [String!]
  additionalData: Dictionary<String, Object>  # ❌ NOT SUPPORTED
}
```

**✅ NEW**:
```graphql
input CreateIssuanceInput {
  credentialType: String!
  walletId: UUID!
  requiredDocuments: [String!]
  additionalDataJson: String  # ✅ JSON string (optional)
}
```

### 2. Output Types (Unchanged)

**Good News**: OUTPUT types still use `AnyType` and return dynamic objects. No changes needed for reading data.

```graphql
type CredentialDto {
  id: UUID!
  walletId: UUID!
  type: String!
  claims: [Any!]!  # ✅ Still works - outputs as object array
  issuedAt: DateTime!
  expiresAt: DateTime
}
```

---

## 🛠️ Required SDK Changes

### A. Update GraphQL Mutations (C# SDK)

**File**: `src/NumbatWallet.Sdk/GraphQL/Mutations.cs`

#### 1. IssueCredential Mutation

**Change**:
```diff
public const string IssueCredential = @"
    mutation IssueCredential($input: IssueCredentialInput!) {
        issueCredential(input: $input) {
            id
            walletId
            type
            status
            claims
            issuedAt
            expiresAt
        }
    }
";
```

**Input DTO**:
```csharp
public class IssueCredentialInput
{
    public Guid WalletId { get; set; }
    public CredentialType CredentialType { get; set; }
    public string Subject { get; set; }
-   public Dictionary<string, object> Claims { get; set; }  // ❌ REMOVE
+   public string ClaimsJson { get; set; }                  // ✅ ADD
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public Guid IssuerOrganizationId { get; set; }
}
```

**SDK Client Usage**:
```csharp
// When calling the mutation:
var claimsDict = new Dictionary<string, object>
{
    ["firstName"] = "John",
    ["lastName"] = "Doe",
    ["age"] = 30,
    ["email"] = "john@example.com",
    ["driversLicenseNumber"] = "DL123456"
};

var input = new IssueCredentialInput
{
    WalletId = walletId,
    CredentialType = CredentialType.DriverLicense,
    Subject = "did:example:123",
    ClaimsJson = JsonSerializer.Serialize(claimsDict),  // ✅ Serialize to JSON
    ValidFrom = DateTime.UtcNow,
    IssuerOrganizationId = orgId
};

var result = await client.IssueCredentialAsync(input);
```

#### 2. BulkIssueCredentials Mutation

**Input DTO**:
```csharp
public class BulkIssueCredentialsInput
{
    public List<Guid> WalletIds { get; set; }
    public CredentialType CredentialType { get; set; }
-   public Dictionary<string, object> Template { get; set; }  // ❌ REMOVE
+   public string TemplateJson { get; set; }                  // ✅ ADD
    public Guid IssuerOrganizationId { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
}
```

**SDK Client Usage**:
```csharp
var template = new Dictionary<string, object>
{
    ["issuer"] = "WA Government",
    ["credentialSubject"] = new Dictionary<string, object>
    {
        ["givenName"] = "{firstName}",
        ["familyName"] = "{lastName}"
    }
};

var input = new BulkIssueCredentialsInput
{
    WalletIds = walletIds,
    CredentialType = CredentialType.ProofOfAge,
    TemplateJson = JsonSerializer.Serialize(template),  // ✅ Serialize
    IssuerOrganizationId = orgId
};
```

#### 3. CreateIssuance Mutation

**Input DTO**:
```csharp
public class CreateIssuanceInput
{
    public string CredentialType { get; set; }
    public Guid WalletId { get; set; }
    public List<string>? RequiredDocuments { get; set; }
-   public Dictionary<string, object>? AdditionalData { get; set; }  // ❌ REMOVE
+   public string? AdditionalDataJson { get; set; }                  // ✅ ADD
}
```

---

### B. Add Missing Exception Types

Create these files in `src/NumbatWallet.Sdk/Exceptions/`:

#### 1. ErrorCode.cs (Enum)
```csharp
namespace NumbatWallet.Sdk.Exceptions;

/// <summary>
/// Standard error codes returned by the NumbatWallet API.
/// </summary>
public enum ErrorCode
{
    /// <summary>Wallet not found</summary>
    WalletNotFound,

    /// <summary>Credential not found</summary>
    CredentialNotFound,

    /// <summary>Unauthorized access attempt</summary>
    UnauthorizedAccess,

    /// <summary>Input validation failed</summary>
    ValidationFailed,

    /// <summary>Rate limit exceeded</summary>
    RateLimitExceeded,

    /// <summary>Internal server error</summary>
    InternalServerError,

    /// <summary>Tenant isolation violation detected</summary>
    TenantIsolationViolation
}
```

#### 2. RateLimitExceededException.cs
```csharp
namespace NumbatWallet.Sdk.Exceptions;

/// <summary>
/// Thrown when API rate limits are exceeded.
/// </summary>
public class RateLimitExceededException : Exception
{
    public ErrorCode ErrorCode => ErrorCode.RateLimitExceeded;
    public int RetryAfterSeconds { get; }

    public RateLimitExceededException(int retryAfterSeconds)
        : base($"Rate limit exceeded. Retry after {retryAfterSeconds} seconds.")
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}
```

#### 3. WalletServiceException.cs
```csharp
namespace NumbatWallet.Sdk.Exceptions;

/// <summary>
/// General wallet service exception with error code.
/// </summary>
public class WalletServiceException : Exception
{
    public ErrorCode ErrorCode { get; }

    public WalletServiceException(ErrorCode errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public WalletServiceException(ErrorCode errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
```

#### 4. ValidationException.cs
```csharp
namespace NumbatWallet.Sdk.Exceptions;

/// <summary>
/// Thrown when input validation fails.
/// </summary>
public class ValidationException : Exception
{
    public ErrorCode ErrorCode => ErrorCode.ValidationFailed;
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException(Dictionary<string, string[]> errors)
        : base("Validation failed")
    {
        Errors = errors;
    }

    public ValidationException(string fieldName, string errorMessage)
        : base($"Validation failed for {fieldName}: {errorMessage}")
    {
        Errors = new Dictionary<string, string[]>
        {
            [fieldName] = new[] { errorMessage }
        };
    }
}
```

#### 5. UnauthorizedException.cs
```csharp
namespace NumbatWallet.Sdk.Exceptions;

/// <summary>
/// Thrown when authentication or authorization fails.
/// </summary>
public class UnauthorizedException : Exception
{
    public ErrorCode ErrorCode => ErrorCode.UnauthorizedAccess;

    public UnauthorizedException(string message = "Unauthorized access")
        : base(message)
    {
    }

    public UnauthorizedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
```

---

### C. Add Missing Pagination Types

Create these files in `src/NumbatWallet.Sdk/Models/GraphQL/`:

#### 1. PagedResult.cs
```csharp
namespace NumbatWallet.Sdk.Models.GraphQL;

/// <summary>
/// Relay-style cursor pagination result.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public PageInfo PageInfo { get; set; } = new();
    public int TotalCount { get; set; }
}
```

#### 2. PageInfo.cs
```csharp
namespace NumbatWallet.Sdk.Models.GraphQL;

/// <summary>
/// Relay-style page information for cursor pagination.
/// </summary>
public class PageInfo
{
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public string? StartCursor { get; set; }
    public string? EndCursor { get; set; }
}
```

#### 3. CursorEdge.cs
```csharp
namespace NumbatWallet.Sdk.Models.GraphQL;

/// <summary>
/// Relay-style edge containing cursor and node.
/// </summary>
public class CursorEdge<T>
{
    public string Cursor { get; set; } = string.Empty;
    public T Node { get; set; } = default!;
}
```

---

## 🧪 Testing & Verification

### 1. Build SDK
```bash
cd /Users/rodrigolmiranda/repo/NumbatWallet-sdks/numbatwallet-dotnet-sdk
dotnet build
```
**Expected**: 0 errors, 0 warnings

### 2. Run Integration Tests
```bash
dotnet test tests/NumbatWallet.Sdk.IntegrationTests/
```
**Expected**: 66 tests pass

### 3. Test GraphQL Connectivity

**Endpoint**: http://localhost:5042/graphql

**Test Query**:
```graphql
{
  __typename
}
```

**Expected Response**:
```json
{
  "data": {
    "__typename": "Query"
  }
}
```

### 4. Test IssueCredential Mutation

```graphql
mutation TestIssue {
  issueCredential(input: {
    walletId: "123e4567-e89b-12d3-a456-426614174000"
    credentialType: DRIVERS_LICENSE
    subject: "did:example:123"
    claimsJson: "{\"firstName\":\"John\",\"lastName\":\"Doe\",\"age\":30}"
    issuerOrganizationId: "223e4567-e89b-12d3-a456-426614174000"
  }) {
    id
    type
    claims
    issuedAt
  }
}
```

**Note**: `claimsJson` must be a **JSON string**, not an object.

---

## 📦 Schema Access

### Option 1: Banana Cake Pop UI (Recommended)
- **URL**: http://localhost:5042/graphql/
- **Features**:
  - Interactive GraphQL explorer
  - Schema documentation
  - Query builder
  - Export schema to SDL

### Option 2: Introspection Query
```bash
curl -X POST http://localhost:5042/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"query IntrospectionQuery { __schema { types { name kind fields { name type { name kind } } } } }"}'
```

### Option 3: Pre-exported Schema Files
Backend team has exported the complete schema:

1. **JSON Introspection**: `docs/poa/graphql-schema-introspection.json` (141KB)
2. **SDL Format**: `docs/poa/graphql-schema-sdl.json` (129KB)

Use these files to validate SDK types match the backend schema.

---

## 🔐 Authentication

### API Key Authentication (For Service Accounts)

**Header**:
```
X-API-Key: your-api-key-here
```

**Configuration** (Backend):
```json
{
  "ApiKeyAuthentication": {
    "Enabled": true,
    "ValidApiKeys": [
      "sdk-test-key-12345",
      "integration-test-key"
    ]
  }
}
```

**SDK Client Setup**:
```csharp
var client = new NumbatWalletClient(new NumbatWalletClientOptions
{
    BaseUrl = "http://localhost:5042/graphql",
    ApiKey = "sdk-test-key-12345",
    Timeout = TimeSpan.FromSeconds(30)
});
```

---

## ⏰ Timeline & Checklist

### Immediate Actions (Day 1)
- [ ] Update `IssueCredentialInput.claims` → `claimsJson: String!`
- [ ] Update `BulkIssueCredentialsInput.template` → `templateJson: String!`
- [ ] Update `CreateIssuanceInput.additionalData` → `additionalDataJson: String?`
- [ ] Add `ErrorCode` enum
- [ ] Add 5 exception types (RateLimitExceeded, WalletService, Validation, Unauthorized, Authentication)
- [ ] Add 3 pagination types (PagedResult, PageInfo, CursorEdge)

### Testing (Day 2)
- [ ] Update SDK client code to serialize Dictionary→JSON
- [ ] Build SDK with zero errors
- [ ] Run integration test suite
- [ ] Verify 66 tests pass
- [ ] Test against running backend

### Deployment (Day 3)
- [ ] Update SDK package version
- [ ] Update CHANGELOG with breaking changes
- [ ] Publish SDK to NuGet (if applicable)
- [ ] Notify SDK consumers of breaking changes

---

## 📞 Backend Team Contact

**GraphQL Endpoint**: http://localhost:5042/graphql
**Banana Cake Pop UI**: http://localhost:5042/graphql/
**Schema Files**: `docs/poa/graphql-schema-*.json`
**Backend Commit**: 2366746
**Branch**: feature/POA-backend-foundation

**Questions?** Review commit 2366746 for implementation details.

---

## 📚 Additional Resources

- **HotChocolate v15 Documentation**: https://chillicream.com/docs/hotchocolate/v15
- **GraphQL Relay Cursor Pagination**: https://relay.dev/graphql/connections.htm
- **Backend PR**: [Link to PR when created]

---

**Last Updated**: October 3, 2025, 15:30 UTC
**Status**: ⚠️ **URGENT - SDK UPDATES REQUIRED**
