# POA Development Standards

**Version:** 1.0  
**Date:** September 10, 2025  
**Applies to:** POA Phase (October 1 - November 4, 2025)

## Table of Contents
- [GitHub Workflow](#github-workflow)
- [Code Standards](#code-standards)
- [Architecture Guidelines](#architecture-guidelines)
- [Testing Requirements](#testing-requirements)
- [Documentation Standards](#documentation-standards)
- [Security Practices](#security-practices)
- [Performance Guidelines](#performance-guidelines)
- [Definition of Done](#definition-of-done)

## GitHub Workflow

### Issue Management

#### Before Starting Work
1. **Locate Your Task**
   - Find your POA task ID in `/docs/POA/00-task-breakdown.md`
   - Note the corresponding GitHub issue number
   - Navigate to issue in GitHub

2. **Pre-flight Checks**
   ```
   ✓ Review issue description and acceptance criteria
   ✓ Check for blocking dependencies
   ✓ Verify milestone and dates
   ✓ Read all comments for context
   ✓ Check for related PRs or issues
   ```

3. **Claim the Issue**
   - Assign yourself to the issue
   - Move to "In Progress" in Project #18
   - Comment: "Starting work on this issue"

#### During Development
- Update issue with progress (daily minimum)
- Document blockers immediately
- Link PRs with `Fixes #<number>` or `Relates to #<number>`
- Request help via issue comments if stuck

#### Completing Work
1. Verify all acceptance criteria met
2. Create PR with issue reference
3. Pass all CI checks
4. Get required reviews
5. Merge and close issue
6. Check if milestone can be closed

### Branch Strategy
```
main
├── feature/POA-001-azure-setup
├── feature/POA-012-backend-structure
├── feature/POA-015-flutter-sdk
└── hotfix/POA-XXX-critical-fix
```

### Commit Messages
```
POA-XXX: Brief description

Detailed explanation if needed.
- List of changes
- Any breaking changes

Fixes #<issue-number>
```

## Code Standards

### C# 13 / .NET 9 Standards

#### Naming Conventions
```csharp
// Classes, Interfaces, Enums: PascalCase
public class DigitalCredential { }
public interface ICredentialService { }
public enum CredentialStatus { }

// Methods: PascalCase
public async Task<Result> IssueCredentialAsync() { }

// Parameters, Variables: camelCase
public void Process(string tenantId, bool isValid)
{
    var credentialData = GetData();
    int retryCount = 3;
}

// Constants: PascalCase
public const string DefaultTenant = "WA-GOV";

// Private Fields: _camelCase
private readonly ILogger<Service> _logger;
private readonly List<Item> _items = [];
```

#### File Organization
```csharp
// File: DigitalCredential.cs
namespace NumbatWallet.Domain.Entities;

// Global usings in GlobalUsings.cs
global using System;
global using System.Collections.Generic;

// File-scoped namespace
namespace NumbatWallet.Domain.Entities;

// Primary constructor for simple types
public sealed class DigitalCredential(
    TenantId tenantId,
    WalletId walletId) : AggregateRoot
{
    // Properties first
    public CredentialId Id { get; } = CredentialId.New();
    
    // Fields
    private readonly List<Attribute> _attributes = [];
    
    // Constructors (if not using primary)
    
    // Public methods
    public Result Issue() { }
    
    // Private methods
    private void Validate() { }
}
```

#### Async/Await Best Practices
```csharp
// Always use Async suffix
public async Task<Result> ProcessAsync()
{
    // ConfigureAwait(false) in libraries
    await DoWorkAsync().ConfigureAwait(false);
    
    // Avoid async void except for event handlers
    // BAD: public async void Process()
    // GOOD: public async Task ProcessAsync()
    
    // Use ValueTask for hot paths
    public async ValueTask<bool> CheckCacheAsync()
    {
        if (_cache.TryGetValue(key, out var value))
            return value;
        
        return await LoadAsync();
    }
}
```

#### Error Handling
```csharp
// Use Result pattern for expected failures
public Result<Credential> Issue(ClaimSet claims)
{
    if (!claims.IsValid)
        return Result<Credential>.Failure("Invalid claims");
    
    return Result<Credential>.Success(credential);
}

// Throw exceptions for unexpected failures
public async Task ProcessAsync()
{
    ArgumentNullException.ThrowIfNull(input);
    
    try
    {
        await _service.ExecuteAsync();
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "HTTP request failed");
        throw new InfrastructureException("External service unavailable", ex);
    }
}
```

### GraphQL Standards (HotChocolate)

```csharp
// Query definitions
[QueryType]
public class CredentialQueries
{
    [GraphQLDescription("Get credential by ID")]
    public async Task<Credential?> GetCredentialAsync(
        [ID] Guid credentialId,
        [Service] ICredentialService service,
        CancellationToken cancellationToken)
    {
        return await service.GetByIdAsync(credentialId, cancellationToken);
    }
}

// Mutation definitions
[MutationType]
public class CredentialMutations
{
    [Error<ValidationException>]
    [Error<DomainException>]
    public async Task<Result<Credential>> IssueCredentialAsync(
        IssueCredentialInput input,
        [Service] IMediator mediator)
    {
        var command = new IssueCredentialCommand(input);
        return await mediator.Send(command);
    }
}
```

## Architecture Guidelines

### Clean Architecture Layers

```
NumbatWallet.Domain/          # No dependencies
├── Entities/
├── ValueObjects/
├── Events/
└── Specifications/

NumbatWallet.Application/     # Depends on Domain
├── Commands/
├── Queries/
├── Interfaces/
└── DTOs/

NumbatWallet.Infrastructure/  # Depends on Application
├── Persistence/
├── External/
├── Identity/
└── Messaging/

NumbatWallet.API/            # Depends on all layers
├── GraphQL/
├── REST/
├── Middleware/
└── Filters/
```

### CQRS with MediatR

```csharp
// Command
public record IssueCredentialCommand(
    Guid WalletId,
    string CredentialType,
    Dictionary<string, object> Claims) : IRequest<Result<Credential>>;

// Handler
public class IssueCredentialHandler : IRequestHandler<IssueCredentialCommand, Result<Credential>>
{
    public async Task<Result<Credential>> Handle(
        IssueCredentialCommand request,
        CancellationToken cancellationToken)
    {
        // Implementation
    }
}

// Query
public record GetCredentialQuery(Guid Id) : IRequest<CredentialDto?>;
```

## Testing Requirements

### Test-Driven Development (TDD) - MANDATORY

**All new code MUST follow TDD principles. No exceptions.**

#### TDD Workflow
```
1. RED: Write a failing test first
2. GREEN: Write minimal code to make test pass
3. REFACTOR: Improve code while keeping tests green
4. COMMIT: Push test + implementation together
```

#### TDD Checklist for Every Feature
- [ ] Test written BEFORE implementation
- [ ] Test fails initially (RED phase verified)
- [ ] Minimal code written to pass test
- [ ] Refactoring done with all tests passing
- [ ] Test and code committed together
- [ ] Coverage targets met

#### Example TDD Flow
```csharp
// STEP 1: Write failing test (RED)
[Fact]
public void Issue_WithValidClaims_CreatesActiveCredential()
{
    // This test will fail because Credential.Issue doesn't exist yet
    var claims = ValidClaimsFactory.Create();
    var result = Credential.Issue(claims);
    result.Should().BeSuccessful();
    result.Value.Status.Should().Be(CredentialStatus.Active);
}

// STEP 2: Write minimal code (GREEN)
public static Result<Credential> Issue(ClaimSet claims)
{
    // Minimal implementation to make test pass
    return Result<Credential>.Success(new Credential 
    { 
        Status = CredentialStatus.Active 
    });
}

// STEP 3: Refactor (REFACTOR)
public static Result<Credential> Issue(ClaimSet claims)
{
    // Improved implementation with validation
    if (!claims.IsValid)
        return Result<Credential>.Failure("Invalid claims");
    
    var credential = new Credential(claims);
    credential.RaiseEvent(new CredentialIssuedEvent(credential.Id));
    return Result<Credential>.Success(credential);
}
```

### Unit Tests (xUnit)
```csharp
public class CredentialTests
{
    [Fact]
    public void Issue_WithValidClaims_Succeeds()
    {
        // Arrange
        var claims = new ClaimSetBuilder().Build();
        
        // Act
        var result = Credential.Issue(claims);
        
        // Assert
        result.Should().BeSuccessful();
        result.Value.Status.Should().Be(CredentialStatus.Active);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Issue_WithInvalidId_Throws(string id)
    {
        // Assert
        Assert.Throws<ArgumentException>(() => new Credential(id));
    }
}
```

### Integration Tests
```csharp
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetCredential_ReturnsCredential()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act
        var response = await client.GetAsync("/api/credentials/123");
        
        // Assert
        response.Should().BeSuccessful();
    }
}
```

### Test Coverage Requirements
- **Minimum**: 80% overall
- **Domain Layer**: 95%
- **Application Layer**: 85%
- **API Layer**: 75%
- **New Code**: 90% (enforced in CI/CD)

## Documentation Standards

### Code Documentation
```csharp
/// <summary>
/// Issues a new digital credential to a wallet.
/// </summary>
/// <param name="walletId">Target wallet identifier</param>
/// <param name="claims">Credential claims to include</param>
/// <returns>Issued credential or failure result</returns>
/// <exception cref="ArgumentNullException">When walletId is null</exception>
public async Task<Result<Credential>> IssueAsync(
    WalletId walletId, 
    ClaimSet claims)
{
    // Implementation
}
```

### API Documentation
- All endpoints must have OpenAPI/Swagger annotations
- GraphQL types must have descriptions
- Include example requests/responses
- Document error codes and meanings

## Security Practices

### Authentication & Authorization
```csharp
[Authorize(Policy = "CredentialIssuer")]
[RequireScope("credentials:write")]
public class IssueController : ControllerBase
{
    // Endpoints
}
```

### Data Protection
```csharp
// Encrypt sensitive data
public class EncryptedCredential
{
    [Encrypted]
    public string Data { get; set; }
    
    [Encrypted]
    public string PrivateKey { get; set; }
}
```

### Secret Management
```csharp
// Never hardcode secrets
// BAD: var key = "abc123";
// GOOD: 
var key = _configuration["KeyVault:ApiKey"];
var secret = await _keyVault.GetSecretAsync("api-key");
```

## Performance Guidelines

### Response Time Targets
- **P50**: <200ms
- **P95**: <500ms
- **P99**: <1000ms

### Optimization Patterns
```csharp
// Use caching for frequent reads
[ResponseCache(Duration = 300)]
public async Task<IActionResult> GetPublicData()

// Use pagination for lists
public async Task<PagedResult<T>> GetPagedAsync(
    int page = 1, 
    int pageSize = 20)

// Use projection for queries
var results = await _context.Credentials
    .Where(c => c.IsActive)
    .Select(c => new CredentialSummary
    {
        Id = c.Id,
        Type = c.Type
    })
    .ToListAsync();
```

## Definition of Done

A task is considered DONE when:

### Code Complete
- [ ] All acceptance criteria met
- [ ] Code follows standards in this document
- [ ] No compiler warnings
- [ ] No TODO comments remaining

### Testing Complete
- [ ] Tests written BEFORE code (TDD followed)
- [ ] Unit tests written and passing
- [ ] Integration tests (if applicable)
- [ ] Test coverage meets requirements (>80%)
- [ ] No flaky or ignored tests
- [ ] Performance tests for critical paths
- [ ] Security tests for sensitive operations
- [ ] Manual testing performed

### Documentation Complete
- [ ] Code comments added
- [ ] API documentation updated
- [ ] README updated (if needed)
- [ ] Architecture diagrams updated (if changed)

### Review Complete
- [ ] Self-review performed
- [ ] Peer review approved
- [ ] CI/CD pipeline passing
- [ ] Security scan passing

### GitHub Complete
- [ ] PR merged to main
- [ ] Issue closed with summary
- [ ] Project board updated
- [ ] Milestone checked

## Quick Reference Card

### Daily Checklist
```
Morning:
□ Check GitHub issues assigned to you
□ Review blocking dependencies
□ Update issue status

During Work:
□ Follow coding standards
□ Write tests as you go
□ Commit frequently with good messages

End of Day:
□ Push all changes
□ Update issue with progress
□ Note any blockers
```

### PR Checklist
```
Before Creating PR:
□ All tests passing locally
□ Code self-reviewed
□ No merge conflicts
□ Issue referenced in PR

PR Description:
□ Link to issue
□ Summary of changes
□ Testing performed
□ Breaking changes noted
```

### Emergency Contacts
- **Technical Issues**: Create comment in GitHub issue
- **Blocking Issues**: Message in #poa-blockers Slack
- **Critical Problems**: Escalate to Tech Lead immediately

---

**Remember**: When in doubt, check the GitHub issue first. All requirements, context, and discussions are tracked there.