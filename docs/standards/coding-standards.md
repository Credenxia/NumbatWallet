# NumbatWallet Coding Standards

## Table of Contents
- [Overview](#overview)
- [Naming Conventions](#naming-conventions)
- [Clean Architecture Principles](#clean-architecture-principles)
- [Domain-Driven Design](#domain-driven-design)
- [CQRS Implementation](#cqrs-implementation)
- [Multi-Tenant Patterns](#multi-tenant-patterns)
- [Security Guidelines](#security-guidelines)
- [Code Review Checklist](#code-review-checklist)

## Overview

This document establishes coding standards for the NumbatWallet Digital Wallet and Verifiable Credentials platform. These standards ensure consistency, maintainability, and security across our multi-tenant wallet solution.

**Core Principles:**
- **Clean Architecture**: Proper separation of concerns with clear boundaries
- **Domain-Driven Design**: Rich domain models for credential and wallet management
- **Multi-Tenant First**: Complete tenant isolation at all layers
- **Security by Default**: Zero-trust approach to credential handling
- **Test-Driven Development**: Comprehensive test coverage for compliance

## Naming Conventions

### C# Code Standards (.NET 8)

#### Classes, Interfaces, and Types
```csharp
// ✅ GOOD - Clear, domain-specific names
public class DigitalCredential : AggregateRoot
public interface ICredentialRepository : IRepository<DigitalCredential>
public enum CredentialStatus { Active, Suspended, Revoked, Expired }
public record CredentialIssuedEvent(Guid CredentialId, Guid TenantId, DateTime IssuedAt);

// ❌ BAD - Vague, non-specific names
public class Manager
public interface IHelper
public enum Status
```

#### Methods and Properties
```csharp
// ✅ GOOD - Clear intent, proper async suffix
public async Task<DigitalCredential> IssueCredentialAsync(
    IssueCredentialCommand command, 
    CancellationToken cancellationToken)
    
public bool IsValidForPresentation()
public string CredentialSubject => Subject?.GetDisplayName() ?? "Unknown";

// ❌ BAD - Unclear purpose, missing context
public async Task<object> Process(object data)
public bool Check()
```

#### Domain-Specific Naming
```csharp
// Wallet Domain
public class WalletHolder : Entity
public class WalletInstance : Entity
public interface IWalletService

// Credential Domain
public class VerifiableCredential : AggregateRoot
public class CredentialSchema : ValueObject
public class SelectiveDisclosure : ValueObject

// PKI Domain
public class IssuerCertificate : Entity
public class CertificateChain : ValueObject
public class RevocationList : Entity
```

### Flutter/Dart Standards

```dart
// ✅ GOOD - Flutter SDK naming
class WalletProvider extends ChangeNotifier
class CredentialCard extends StatelessWidget
class BiometricAuthService
Future<Credential> fetchCredential(String credentialId)

// ❌ BAD
class provider extends ChangeNotifier
class card extends StatelessWidget
```

### TypeScript/JavaScript Standards

```typescript
// ✅ GOOD - TypeScript SDK naming
interface ICredentialVerifier
class WalletClient
type CredentialPresentation = {...}
async function verifyCredential(credential: VerifiableCredential): Promise<VerificationResult>

// ❌ BAD
interface verifier
class client
```

## Clean Architecture Principles

### Layer Organization

```
NumbatWallet.Backend/
├── Domain/              # No dependencies
│   ├── Entities/       # Business entities
│   ├── ValueObjects/   # Immutable values
│   ├── Events/         # Domain events
│   └── Interfaces/     # Domain contracts
│
├── Application/         # Domain dependency only
│   ├── Commands/       # Write operations
│   ├── Queries/        # Read operations
│   ├── DTOs/          # Data transfer objects
│   └── Interfaces/     # Application contracts
│
├── Infrastructure/      # External concerns
│   ├── Persistence/    # EF Core, repositories
│   ├── Identity/       # Authentication/authorization
│   ├── PKI/           # Certificate management
│   └── External/       # Third-party integrations
│
└── API/                # Presentation layer
    ├── Controllers/    # REST endpoints
    ├── Middleware/     # Cross-cutting concerns
    └── Models/         # API models
```

### Dependency Rules

```csharp
// ✅ GOOD - Dependencies point inward
// Domain layer - no dependencies
namespace NumbatWallet.Domain.Entities;
public class DigitalCredential
{
    // Pure business logic, no framework dependencies
}

// Application layer - depends on Domain only
namespace NumbatWallet.Application.Commands;
public class IssueCredentialCommandHandler : IRequestHandler<IssueCredentialCommand>
{
    private readonly ICredentialRepository _repository; // Domain interface
}

// Infrastructure - implements interfaces
namespace NumbatWallet.Infrastructure.Persistence;
public class CredentialRepository : ICredentialRepository
{
    private readonly WalletDbContext _context; // EF Core dependency OK here
}
```

## Domain-Driven Design

### Aggregate Design

```csharp
// Credential Aggregate
public class DigitalCredential : AggregateRoot
{
    private readonly List<CredentialAttribute> _attributes = new();
    private readonly List<PresentationRecord> _presentations = new();
    
    public CredentialId Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public WalletId HolderId { get; private set; }
    public CredentialSchema Schema { get; private set; }
    public CredentialStatus Status { get; private set; }
    
    // Business logic encapsulated
    public void Revoke(RevocationReason reason)
    {
        if (Status == CredentialStatus.Revoked)
            throw new DomainException("Credential already revoked");
            
        Status = CredentialStatus.Revoked;
        AddDomainEvent(new CredentialRevokedEvent(Id, reason, DateTime.UtcNow));
    }
    
    public PresentationToken GeneratePresentation(
        SelectiveDisclosureRequest request)
    {
        EnsureNotRevoked();
        EnsureNotExpired();
        
        var disclosed = _attributes
            .Where(a => request.RequestedAttributes.Contains(a.Name))
            .ToList();
            
        var presentation = new PresentationRecord(disclosed, DateTime.UtcNow);
        _presentations.Add(presentation);
        
        return PresentationToken.Create(this, disclosed);
    }
}
```

### Value Objects

```csharp
public class CredentialSchema : ValueObject
{
    public string Type { get; }
    public string Version { get; }
    public IReadOnlyList<SchemaAttribute> Attributes { get; }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Type;
        yield return Version;
        foreach (var attr in Attributes)
            yield return attr;
    }
}
```

## CQRS Implementation

### Command Pattern

```csharp
// Command
public record IssueCredentialCommand : IRequest<CredentialId>
{
    public required TenantId TenantId { get; init; }
    public required WalletId WalletId { get; init; }
    public required CredentialType Type { get; init; }
    public required Dictionary<string, object> Claims { get; init; }
}

// Handler
public class IssueCredentialCommandHandler 
    : IRequestHandler<IssueCredentialCommand, CredentialId>
{
    private readonly ICredentialRepository _repository;
    private readonly IPKIService _pkiService;
    private readonly IEventBus _eventBus;
    
    public async Task<CredentialId> Handle(
        IssueCredentialCommand command,
        CancellationToken cancellationToken)
    {
        // Validate
        var schema = await _schemaRegistry.GetSchemaAsync(command.Type);
        schema.ValidateClaims(command.Claims);
        
        // Create aggregate
        var credential = DigitalCredential.Issue(
            command.TenantId,
            command.WalletId,
            schema,
            command.Claims);
        
        // Sign with PKI
        var signature = await _pkiService.SignCredentialAsync(credential);
        credential.ApplySignature(signature);
        
        // Persist
        await _repository.AddAsync(credential);
        await _repository.SaveChangesAsync();
        
        // Publish events
        await _eventBus.PublishAsync(credential.GetDomainEvents());
        
        return credential.Id;
    }
}
```

### Query Pattern

```csharp
// Query
public record GetWalletCredentialsQuery : IRequest<IReadOnlyList<CredentialDto>>
{
    public required TenantId TenantId { get; init; }
    public required WalletId WalletId { get; init; }
    public bool IncludeRevoked { get; init; } = false;
}

// Read model
public class CredentialReadModelHandler 
    : IRequestHandler<GetWalletCredentialsQuery, IReadOnlyList<CredentialDto>>
{
    private readonly IReadDbContext _readDb;
    
    public async Task<IReadOnlyList<CredentialDto>> Handle(
        GetWalletCredentialsQuery query,
        CancellationToken cancellationToken)
    {
        return await _readDb.Credentials
            .Where(c => c.TenantId == query.TenantId)
            .Where(c => c.WalletId == query.WalletId)
            .Where(c => query.IncludeRevoked || c.Status != "Revoked")
            .ProjectTo<CredentialDto>()
            .ToListAsync(cancellationToken);
    }
}
```

## Multi-Tenant Patterns

### Tenant Isolation

```csharp
// Middleware for tenant resolution
public class TenantResolutionMiddleware
{
    public async Task InvokeAsync(HttpContext context, ITenantResolver resolver)
    {
        var tenant = await resolver.ResolveAsync(context);
        if (tenant == null)
        {
            context.Response.StatusCode = 404;
            return;
        }
        
        context.Items["TenantId"] = tenant.Id;
        context.Items["TenantContext"] = tenant;
        
        await _next(context);
    }
}

// Repository with automatic tenant filtering
public class TenantAwareRepository<T> : IRepository<T> 
    where T : class, ITenantEntity
{
    private readonly DbContext _context;
    private readonly ITenantContext _tenantContext;
    
    public IQueryable<T> Query()
    {
        return _context.Set<T>()
            .Where(e => e.TenantId == _tenantContext.TenantId);
    }
}
```

### Database Per Tenant

```csharp
public class TenantDbContextFactory
{
    private readonly IConfiguration _configuration;
    
    public WalletDbContext CreateDbContext(TenantId tenantId)
    {
        var connectionString = _configuration
            .GetConnectionString($"Tenant_{tenantId}");
            
        var optionsBuilder = new DbContextOptionsBuilder<WalletDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        
        return new WalletDbContext(optionsBuilder.Options, tenantId);
    }
}
```

## Security Guidelines

### Credential Handling

```csharp
// ✅ GOOD - Secure credential storage
public class SecureCredentialStore
{
    private readonly IDataProtector _protector;
    
    public async Task StoreCredentialAsync(DigitalCredential credential)
    {
        var sensitiveData = credential.GetSensitiveData();
        var encrypted = _protector.Protect(
            JsonSerializer.Serialize(sensitiveData));
        
        // Store encrypted data
        await _repository.StoreEncryptedAsync(credential.Id, encrypted);
        
        // Audit log
        await _auditLog.LogCredentialStoredAsync(
            credential.Id, 
            _userContext.UserId,
            DateTime.UtcNow);
    }
}

// ❌ BAD - Plain text storage
public async Task StoreCredentialAsync(DigitalCredential credential)
{
    var json = JsonSerializer.Serialize(credential);
    await File.WriteAllTextAsync($"creds/{credential.Id}.json", json);
}
```

### API Security

```csharp
[ApiController]
[Authorize]
[RequireScope("wallet:read")]
[Route("api/v1/[controller]")]
public class CredentialsController : ControllerBase
{
    [HttpPost("issue")]
    [RequireScope("wallet:admin")]
    [ValidateTenant]
    public async Task<IActionResult> IssueCredential(
        [FromBody] IssueCredentialRequest request)
    {
        // Validate request signature
        if (!await _signatureValidator.ValidateAsync(Request))
            return Unauthorized();
        
        // Additional implementation...
    }
}
```

## Code Review Checklist

### Security Review
- [ ] No hardcoded secrets or credentials
- [ ] All inputs validated and sanitized
- [ ] Tenant isolation verified
- [ ] Encryption for sensitive data
- [ ] Audit logging for critical operations
- [ ] Rate limiting implemented

### Architecture Review
- [ ] Clean Architecture layers respected
- [ ] Dependencies point inward
- [ ] No business logic in controllers
- [ ] Domain logic encapsulated in entities
- [ ] CQRS pattern properly implemented
- [ ] Repository pattern used correctly

### Code Quality
- [ ] Meaningful variable and method names
- [ ] Methods under 20 lines
- [ ] Classes follow Single Responsibility
- [ ] No code duplication
- [ ] Proper error handling
- [ ] Async/await used correctly

### Testing
- [ ] Unit tests for domain logic
- [ ] Integration tests for APIs
- [ ] Test coverage > 80%
- [ ] Tests follow AAA pattern
- [ ] Mocks used appropriately
- [ ] No brittle tests

### Documentation
- [ ] XML comments for public APIs
- [ ] README updated if needed
- [ ] OpenAPI spec updated
- [ ] Complex logic has comments
- [ ] Architecture decisions documented