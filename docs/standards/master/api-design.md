# NumbatWallet API Standards

## Table of Contents
- [API Design Principles](#api-design-principles)
- [RESTful Conventions](#restful-conventions)
- [Versioning Strategy](#versioning-strategy)
- [Request/Response Format](#requestresponse-format)
- [Error Handling](#error-handling)
- [Authentication & Authorization](#authentication--authorization)
- [Rate Limiting](#rate-limiting)
- [OpenAPI Documentation](#openapi-documentation)
- [SDK Standards](#sdk-standards)

## API Design Principles

### Core Principles

1. **Resource-Oriented** - URLs identify resources, HTTP methods define operations
2. **Stateless** - Each request contains all information needed
3. **Consistent** - Predictable patterns across all endpoints
4. **Secure by Default** - HTTPS only, authentication required
5. **Tenant-Aware** - Multi-tenant isolation built-in
6. **Standards-Compliant** - W3C VC, OpenID4VCI, ISO 18013-5

## RESTful Conventions

### Resource Naming

```
# Collection resources (plural)
GET    /api/v1/credentials           # List credentials
POST   /api/v1/credentials           # Create credential
GET    /api/v1/credentials/{id}      # Get specific credential
PUT    /api/v1/credentials/{id}      # Update credential
DELETE /api/v1/credentials/{id}      # Delete credential

# Sub-resources
GET    /api/v1/wallets/{walletId}/credentials
POST   /api/v1/credentials/{id}/presentations
POST   /api/v1/credentials/{id}/revoke

# Actions (when REST doesn't fit)
POST   /api/v1/credentials/{id}/actions/verify
POST   /api/v1/credentials/batch-issue
```

### HTTP Methods

| Method | Operation | Idempotent | Safe | Use Case |
|--------|-----------|------------|------|----------|
| GET | Read | Yes | Yes | Retrieve resources |
| POST | Create | No | No | Create new resources |
| PUT | Update | Yes | No | Full update |
| PATCH | Partial Update | No | No | Partial update |
| DELETE | Delete | Yes | No | Remove resources |
| HEAD | Headers | Yes | Yes | Check existence |
| OPTIONS | Options | Yes | Yes | CORS preflight |

### Status Codes

```csharp
// Success
200 OK                  // GET, PUT, PATCH success
201 Created            // POST success with new resource
202 Accepted           // Async operation started
204 No Content         // DELETE success

// Client Errors
400 Bad Request        // Invalid request format
401 Unauthorized       // Missing/invalid authentication
403 Forbidden          // Valid auth, insufficient permissions
404 Not Found          // Resource doesn't exist
409 Conflict           // State conflict (e.g., duplicate)
422 Unprocessable      // Validation errors
429 Too Many Requests  // Rate limit exceeded

// Server Errors
500 Internal Error     // Unhandled server error
502 Bad Gateway        // Upstream service error
503 Service Unavailable // Temporary unavailability
504 Gateway Timeout    // Upstream timeout
```

## Versioning Strategy

### URL Path Versioning

```
/api/v1/credentials   # Version 1
/api/v2/credentials   # Version 2 (breaking changes)
```

### Version Lifecycle

```csharp
[ApiController]
[ApiVersion("1.0", Deprecated = false)]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class CredentialsController : ControllerBase
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetV1() { /* ... */ }
    
    [HttpGet]
    [MapToApiVersion("2.0")]
    public async Task<IActionResult> GetV2() { /* ... */ }
}
```

### Deprecation Policy

1. New version released with deprecation notice
2. 6-month deprecation period
3. Sunset headers sent 3 months before removal
4. Version removed after sunset date

```http
HTTP/1.1 200 OK
Sunset: Sat, 31 Mar 2025 23:59:59 GMT
Deprecation: true
Link: </api/v2/credentials>; rel="successor-version"
```

## Request/Response Format

### Request Structure

```json
// POST /api/v1/credentials
{
  "walletId": "550e8400-e29b-41d4-a716-446655440000",
  "credentialType": "DriverLicense",
  "validFrom": "2025-01-01T00:00:00Z",
  "validUntil": "2030-01-01T00:00:00Z",
  "claims": {
    "firstName": "John",
    "lastName": "Doe",
    "licenseNumber": "DL123456",
    "dateOfBirth": "1990-01-01",
    "categories": ["C", "R"]
  },
  "evidence": {
    "type": "document_verification",
    "verifier": "Transport Authority",
    "timestamp": "2024-12-01T10:00:00Z"
  }
}
```

### Response Structure

```json
// Successful response
{
  "data": {
    "id": "cred_2LfDtKxBQzGRpjSkg",
    "walletId": "550e8400-e29b-41d4-a716-446655440000",
    "type": "DriverLicense",
    "status": "active",
    "issuedAt": "2025-01-01T00:00:00Z",
    "expiresAt": "2030-01-01T00:00:00Z",
    "credential": {
      "@context": ["https://www.w3.org/2018/credentials/v1"],
      "type": ["VerifiableCredential", "DriverLicense"],
      "credentialSubject": { /* ... */ },
      "proof": { /* ... */ }
    }
  },
  "meta": {
    "timestamp": "2025-01-01T00:00:00Z",
    "version": "1.0",
    "requestId": "req_3NfEuKyBRzHSpkSlm"
  }
}

// Error response
{
  "error": {
    "type": "validation_error",
    "message": "The request contains invalid fields",
    "details": [
      {
        "field": "claims.dateOfBirth",
        "code": "invalid_format",
        "message": "Date must be in ISO 8601 format"
      }
    ],
    "requestId": "req_3NfEuKyBRzHSpkSlm",
    "timestamp": "2025-01-01T00:00:00Z"
  }
}
```

### Pagination

```json
// GET /api/v1/credentials?page=2&limit=20
{
  "data": [ /* ... */ ],
  "pagination": {
    "page": 2,
    "limit": 20,
    "total": 157,
    "totalPages": 8,
    "hasNext": true,
    "hasPrev": true
  },
  "links": {
    "self": "/api/v1/credentials?page=2&limit=20",
    "first": "/api/v1/credentials?page=1&limit=20",
    "prev": "/api/v1/credentials?page=1&limit=20",
    "next": "/api/v1/credentials?page=3&limit=20",
    "last": "/api/v1/credentials?page=8&limit=20"
  }
}
```

### Filtering & Sorting

```
GET /api/v1/credentials?status=active&type=DriverLicense&sort=-issuedAt&fields=id,status,type

# Operators
?status.eq=active          # Equals
?issuedAt.gte=2024-01-01  # Greater than or equal
?type.in=DL,Passport      # In list
?subject.contains=John     # Contains

# Sorting
?sort=issuedAt            # Ascending
?sort=-issuedAt           # Descending
?sort=type,-issuedAt      # Multiple

# Field selection
?fields=id,status,type    # Partial response
```

## Error Handling

### Error Response Format

```csharp
public class ApiError
{
    public string Type { get; set; }        // Error category
    public string Message { get; set; }     // Human-readable message
    public string Code { get; set; }        // Machine-readable code
    public List<ErrorDetail> Details { get; set; }
    public string RequestId { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Extensions { get; set; }
}

public class ErrorDetail
{
    public string Field { get; set; }
    public string Code { get; set; }
    public string Message { get; set; }
}
```

### Error Types

```csharp
public static class ErrorTypes
{
    public const string ValidationError = "validation_error";
    public const string AuthenticationError = "authentication_error";
    public const string AuthorizationError = "authorization_error";
    public const string NotFoundError = "not_found";
    public const string ConflictError = "conflict";
    public const string RateLimitError = "rate_limit_exceeded";
    public const string ServerError = "internal_server_error";
    public const string ServiceUnavailable = "service_unavailable";
}
```

### Global Error Handler

```csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await HandleValidationException(context, ex);
        }
        catch (NotFoundException ex)
        {
            await HandleNotFoundException(context, ex);
        }
        catch (UnauthorizedException ex)
        {
            await HandleUnauthorizedException(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleGenericException(context, ex);
        }
    }
}
```

## Authentication & Authorization

### Bearer Token Authentication

```http
GET /api/v1/credentials
Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
```

### JWT Token Structure

```json
{
  "iss": "https://identity.wallet.wa.gov.au",
  "sub": "user_2LfDtKxBQzGRpjSkg",
  "aud": "https://api.wallet.wa.gov.au",
  "exp": 1640995200,
  "iat": 1640908800,
  "jti": "token_3NfEuKyBRzHSpkSlm",
  "tenant_id": "wa_transport",
  "scope": "wallet:read wallet:write",
  "roles": ["issuer", "verifier"]
}
```

### Scope-Based Authorization

```csharp
[Authorize]
[RequireScope("wallet:read")]
public async Task<IActionResult> GetCredentials() { }

[Authorize]
[RequireScope("wallet:admin")]
public async Task<IActionResult> IssueCredential() { }

[Authorize]
[RequireAnyScope("wallet:write", "wallet:admin")]
public async Task<IActionResult> UpdateCredential() { }
```

### API Key Authentication (M2M)

```http
GET /api/v1/credentials
X-API-Key: sk_live_3NfEuKyBRzHSpkSlm
X-Tenant-Id: wa_transport
```

## Rate Limiting

### Rate Limit Headers

```http
HTTP/1.1 200 OK
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1640995200
X-RateLimit-Policy: 1000;w=3600
```

### Rate Limit Configuration

```csharp
services.AddRateLimiter(options =>
{
    options.AddPolicy("api", context =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: GetTenantId(context),
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1000,
                ReplenishmentPeriod = TimeSpan.FromHours(1),
                TokensPerPeriod = 1000,
                AutoReplenishment = true
            }));
    
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsJsonAsync(new ApiError
        {
            Type = ErrorTypes.RateLimitError,
            Message = "Rate limit exceeded. Please retry later.",
            Extensions = new()
            {
                ["retryAfter"] = context.Lease.TryGetMetadata(
                    MetadataName.RetryAfter, out var retry) ? retry : 60
            }
        });
    };
});
```

## OpenAPI Documentation

### OpenAPI Specification

```yaml
openapi: 3.0.3
info:
  title: NumbatWallet API
  description: Digital Wallet and Verifiable Credentials API
  version: 1.0.0
  contact:
    email: support@wallet.wa.gov.au
  license:
    name: Proprietary
servers:
  - url: https://api.wallet.wa.gov.au/v1
    description: Production
  - url: https://api-test.wallet.wa.gov.au/v1
    description: Test Environment
security:
  - bearerAuth: []
  - apiKey: []

paths:
  /credentials:
    post:
      summary: Issue a new credential
      operationId: issueCredential
      tags:
        - Credentials
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/IssueCredentialRequest'
      responses:
        '201':
          description: Credential created successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/CredentialResponse'
        '400':
          $ref: '#/components/responses/BadRequest'
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '422':
          $ref: '#/components/responses/ValidationError'

components:
  securitySchemes:
    bearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT
    apiKey:
      type: apiKey
      in: header
      name: X-API-Key
```

### Swagger Configuration

```csharp
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NumbatWallet API",
        Version = "v1",
        Description = "Digital Wallet and Verifiable Credentials API",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@wallet.wa.gov.au"
        }
    });
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });
    
    options.OperationFilter<SecurityRequirementsOperationFilter>();
    options.SchemaFilter<EnumSchemaFilter>();
    options.DocumentFilter<TenantAwareDocumentFilter>();
    
    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});
```

## SDK Standards

### SDK Design Principles

1. **Idiomatic** - Follow language conventions
2. **Type-Safe** - Strongly typed where possible
3. **Async-First** - Non-blocking operations
4. **Retryable** - Automatic retry with backoff
5. **Testable** - Mockable interfaces

### Flutter SDK

```dart
class WalletClient {
  final String baseUrl;
  final String apiKey;
  final Duration timeout;
  
  WalletClient({
    required this.baseUrl,
    required this.apiKey,
    this.timeout = const Duration(seconds: 30),
  });
  
  Future<Credential> issueCredential(IssueCredentialRequest request) async {
    final response = await _post('/credentials', request.toJson());
    return Credential.fromJson(response);
  }
  
  Stream<Credential> streamCredentials({
    CredentialFilter? filter,
    int pageSize = 20,
  }) async* {
    var page = 1;
    var hasMore = true;
    
    while (hasMore) {
      final response = await getCredentials(
        filter: filter,
        page: page,
        limit: pageSize,
      );
      
      for (final credential in response.data) {
        yield credential;
      }
      
      hasMore = response.hasNext;
      page++;
    }
  }
}
```

### .NET SDK

```csharp
public class WalletClient : IWalletClient
{
    private readonly HttpClient _httpClient;
    private readonly WalletClientOptions _options;
    
    public WalletClient(HttpClient httpClient, WalletClientOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }
    
    public async Task<Credential> IssueCredentialAsync(
        IssueCredentialRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await PostAsync<IssueCredentialRequest, CredentialResponse>(
            "/credentials",
            request,
            cancellationToken);
            
        return response.Data;
    }
    
    public async IAsyncEnumerable<Credential> GetCredentialsAsync(
        CredentialFilter filter = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var page = 1;
        var hasMore = true;
        
        while (hasMore && !cancellationToken.IsCancellationRequested)
        {
            var response = await GetPagedAsync<Credential>(
                "/credentials",
                page,
                filter,
                cancellationToken);
            
            foreach (var credential in response.Data)
            {
                yield return credential;
            }
            
            hasMore = response.HasNext;
            page++;
        }
    }
}
```

### TypeScript SDK

```typescript
export class WalletClient {
  constructor(
    private readonly config: WalletConfig
  ) {}
  
  async issueCredential(
    request: IssueCredentialRequest
  ): Promise<Credential> {
    const response = await this.post<CredentialResponse>(
      '/credentials',
      request
    );
    return response.data;
  }
  
  async *getCredentials(
    filter?: CredentialFilter
  ): AsyncGenerator<Credential> {
    let page = 1;
    let hasMore = true;
    
    while (hasMore) {
      const response = await this.get<PagedResponse<Credential>>(
        '/credentials',
        { ...filter, page }
      );
      
      for (const credential of response.data) {
        yield credential;
      }
      
      hasMore = response.hasNext;
      page++;
    }
  }
  
  // React hook
  useCredentials(filter?: CredentialFilter) {
    return useQuery({
      queryKey: ['credentials', filter],
      queryFn: () => this.getCredentials(filter),
    });
  }
}
```