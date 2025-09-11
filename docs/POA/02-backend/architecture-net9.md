# Backend Architecture - Clean Architecture with .NET 9 and C# 13

**Version:** 1.1  
**POA Phase:** Week 1-2  
**Last Updated:** September 10, 2025  
**Runtime:** .NET 9.0 (POA) → .NET 10 (Production December 2025)  
**Language:** C# 13 (POA) → C# 14 (Production December 2025)

## Table of Contents
- [Overview](#overview)
- [.NET 9 and C# 13 Features](#net-9-and-c-13-features)
- [Architecture Principles](#architecture-principles)
- [Project Structure](#project-structure)
- [Domain Layer with C# 13](#domain-layer-with-c-13)
- [Application Layer](#application-layer)
- [Infrastructure Layer](#infrastructure-layer)
- [API Layer](#api-layer)
- [GraphQL API Strategy](#graphql-api-strategy)
- [Migration Path to .NET 10](#migration-path-to-net-10)

## Overview

The NumbatWallet POA backend is built on **.NET 9** with **C# 13**, implementing Clean Architecture principles. The solution is designed for easy migration to .NET 10 and C# 14 when selected for production in December 2025.

### Technology Stack
- **.NET 9.0** - Latest LTS runtime for POA phase
- **C# 13** - Modern language features for cleaner code
- **ASP.NET Core 9** - High-performance web framework
- **Entity Framework Core 9** - ORM with PostgreSQL provider
- **MediatR 12** - CQRS implementation
- **FluentValidation 11** - Validation framework
- **HotChocolate 13** - GraphQL server implementation
- **Serilog** - Structured logging with Application Insights

### C# 13 Key Features Used
- Primary constructors for classes
- Enhanced pattern matching
- Default lambda parameters
- Improved collection expressions
- Alias any type
- Inline arrays
- Enhanced `nameof` scope

## .NET 9 and C# 13 Features

### Global Usings and File-Scoped Namespaces

```csharp
// GlobalUsings.cs
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using MediatR;
global using Microsoft.Extensions.Logging;
global using NumbatWallet.Domain.Common;
global using NumbatWallet.Domain.Entities;
global using NumbatWallet.Domain.ValueObjects;
```

### Project Configuration (.NET 9)

```xml
<!-- NumbatWallet.Domain.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>13.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnablePreviewFeatures>false</EnablePreviewFeatures>
    
    <!-- Performance optimizations -->
    <PublishAot>true</PublishAot>
    <PublishTrimmed>true</PublishTrimmed>
    <InvariantGlobalization>false</InvariantGlobalization>
    <TieredCompilation>true</TieredCompilation>
    <TieredCompilationQuickJit>true</TieredCompilationQuickJit>
  </PropertyGroup>
</Project>
```

## Domain Layer with C# 13

### Entity with Primary Constructor (C# 13)

```csharp
namespace NumbatWallet.Domain.Entities;

// C# 13: Primary constructor for entity
public sealed class DigitalCredential(
    TenantId tenantId,
    WalletId walletId,
    CredentialType type,
    CredentialSchema schema) : AggregateRoot
{
    private readonly List<CredentialAttribute> _attributes = [];  // C# 13: Collection expression
    private readonly List<PresentationRecord> _presentations = [];
    
    // Properties using primary constructor parameters
    public CredentialId Id { get; private init; } = CredentialId.New();
    public TenantId TenantId { get; } = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
    public WalletId WalletId { get; } = walletId ?? throw new ArgumentNullException(nameof(walletId));
    public CredentialType Type { get; } = type;
    public CredentialSchema Schema { get; } = schema ?? throw new ArgumentNullException(nameof(schema));
    public CredentialStatus Status { get; private set; } = CredentialStatus.Active;
    public DateTime IssuedAt { get; private init; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; init; }
    public DateTime? RevokedAt { get; private set; }
    
    // C# 13: Enhanced pattern matching with property patterns
    public bool IsValid => Status switch
    {
        CredentialStatus.Active when ExpiresAt is null => true,
        CredentialStatus.Active when ExpiresAt > DateTime.UtcNow => true,
        _ => false
    };
    
    // C# 13: Static abstract interface implementation
    public static DigitalCredential Issue(
        TenantId tenantId,
        WalletId walletId,
        CredentialType type,
        CredentialSchema schema,
        Dictionary<string, object> claims,
        DateTime? expiresAt = null)
    {
        var credential = new DigitalCredential(tenantId, walletId, type, schema)
        {
            ExpiresAt = expiresAt
        };
        
        // C# 13: Enhanced foreach with pattern matching
        foreach (var (key, value) in claims)
        {
            credential.AddAttribute(key, value);
        }
        
        credential.RaiseDomainEvent(new CredentialIssuedEvent(credential));
        return credential;
    }
    
    // C# 13: Switch expression with when clause
    public Result Revoke(RevocationReason reason, string revokedBy) => Status switch
    {
        CredentialStatus.Revoked => Result.Failure("Credential already revoked"),
        _ when string.IsNullOrEmpty(revokedBy) => Result.Failure("Revoker identity required"),
        _ => ExecuteRevocation(reason, revokedBy)
    };
    
    private Result ExecuteRevocation(RevocationReason reason, string revokedBy)
    {
        Status = CredentialStatus.Revoked;
        RevokedAt = DateTime.UtcNow;
        RaiseDomainEvent(new CredentialRevokedEvent(Id, TenantId, reason, revokedBy, RevokedAt.Value));
        return Result.Success();
    }
    
    // C# 13: List patterns
    public SelectiveDisclosure CreateDisclosure(params string[] requestedAttributes) => requestedAttributes switch
    {
        [] => SelectiveDisclosure.Empty(),
        [var single] => SelectiveDisclosure.Single(_attributes.FirstOrDefault(a => a.Name == single)),
        [.. var multiple] => SelectiveDisclosure.Multiple(_attributes.Where(a => multiple.Contains(a.Name)))
    };
}
```

### Value Objects with Records (C# 13)

```csharp
namespace NumbatWallet.Domain.ValueObjects;

// C# 13: Primary constructor with validation
public record struct CredentialId(string Value)
{
    // C# 13: Init-only property with validation
    public string Value { get; init; } = ValidateAndFormat(Value);
    
    private static string ValidateAndFormat(string value) => value switch
    {
        null or "" => throw new ArgumentException("CredentialId cannot be empty"),
        not ['c', 'r', 'e', 'd', '_', ..] => throw new ArgumentException("CredentialId must start with 'cred_'"),
        _ => value
    };
    
    // C# 13: Static factory with enhanced pattern
    public static CredentialId New() => new($"cred_{GenerateId()}");
    
    // C# 13: Inline array for performance
    private static ReadOnlySpan<char> GenerateId()
    {
        Span<char> buffer = stackalloc char[16];
        Random.Shared.NextBytes(MemoryMarshal.AsBytes(buffer));
        return buffer;
    }
    
    // Implicit conversions
    public static implicit operator string(CredentialId id) => id.Value;
    public static explicit operator CredentialId(string value) => new(value);
}

// C# 13: Type alias for complex types
using TenantConfiguration = Dictionary<string, (bool Enabled, int Limit, string Description)>;

// C# 13: Record with positional parameters and validation
public sealed record TenantId(Guid Value) : IComparable<TenantId>
{
    public Guid Value { get; } = Value == Guid.Empty 
        ? throw new ArgumentException("TenantId cannot be empty") 
        : Value;
    
    public static TenantId New() => new(Guid.NewGuid());
    public int CompareTo(TenantId? other) => Value.CompareTo(other?.Value);
}
```

## Application Layer

### CQRS with C# 13 Features

```csharp
namespace NumbatWallet.Application.Commands;

// C# 13: File-scoped namespace and primary constructor
public sealed record IssueCredentialCommand(
    string WalletId,
    string CredentialType,
    Dictionary<string, object> Claims,
    DateTime? ExpiresAt = null) : IRequest<Result<CredentialDto>>
{
    // C# 13: Enhanced pattern matching for validation
    public bool IsValid => (WalletId, CredentialType, Claims) switch
    {
        (null or "", _, _) => false,
        (_, null or "", _) => false,
        (_, _, null or { Count: 0 }) => false,
        _ => true
    };
}

// C# 13: Generic attributes with type constraints
[Handler<IssueCredentialCommand, Result<CredentialDto>>]
public sealed class IssueCredentialCommandHandler(
    ICredentialRepository repository,
    IPKIService pkiService,
    ITenantContext tenantContext,
    ILogger<IssueCredentialCommandHandler> logger) 
    : IRequestHandler<IssueCredentialCommand, Result<CredentialDto>>
{
    // C# 13: Simplified async patterns
    public async Task<Result<CredentialDto>> Handle(
        IssueCredentialCommand request,
        CancellationToken cancellationToken)
    {
        // C# 13: Pattern matching with property patterns
        if (request is not { IsValid: true })
            return Result<CredentialDto>.Failure("Invalid request");
        
        try
        {
            // C# 13: Target-typed new
            WalletId walletId = new(request.WalletId);
            var wallet = await repository.GetWalletAsync(walletId, cancellationToken);
            
            // C# 13: Switch expression with tuple pattern
            var credential = (wallet, request.CredentialType) switch
            {
                (null, _) => throw new NotFoundException($"Wallet {walletId} not found"),
                ({ IsActive: false }, _) => throw new InvalidOperationException("Wallet is not active"),
                (_, var type) => await CreateCredential(wallet, type, request.Claims, request.ExpiresAt)
            };
            
            // Sign with PKI
            var signature = await pkiService.SignAsync(credential, cancellationToken);
            credential.ApplySignature(signature);
            
            // Save
            await repository.AddAsync(credential, cancellationToken);
            await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Credential {Id} issued for wallet {WalletId}", 
                credential.Id, walletId);
            
            // C# 13: Target-typed new with mapper
            return Result<CredentialDto>.Success(new(credential));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to issue credential");
            return Result<CredentialDto>.Failure(ex.Message);
        }
    }
    
    private async Task<DigitalCredential> CreateCredential(
        Wallet wallet,
        string type,
        Dictionary<string, object> claims,
        DateTime? expiresAt)
    {
        // C# 13: Enum parsing with pattern matching
        var credentialType = Enum.TryParse<CredentialType>(type, out var parsed) 
            ? parsed 
            : throw new ArgumentException($"Invalid credential type: {type}");
        
        var schema = await repository.GetSchemaAsync(credentialType);
        
        return DigitalCredential.Issue(
            tenantContext.TenantId,
            wallet.Id,
            credentialType,
            schema,
            claims,
            expiresAt);
    }
}
```

### Validation with FluentValidation and C# 13

```csharp
namespace NumbatWallet.Application.Validators;

// C# 13: Static abstract members in interfaces
public interface ICommandValidator<T> where T : IRequest
{
    static abstract ValidationResult Validate(T command);
}

public sealed class IssueCredentialCommandValidator 
    : AbstractValidator<IssueCredentialCommand>, ICommandValidator<IssueCredentialCommand>
{
    public IssueCredentialCommandValidator()
    {
        // C# 13: Enhanced lambda expressions
        RuleFor(x => x.WalletId)
            .NotEmpty()
            .Matches(@"^wallet_[\w]{16}$")
            .WithMessage("Invalid wallet ID format");
        
        RuleFor(x => x.CredentialType)
            .NotEmpty()
            .Must(type => Enum.TryParse<CredentialType>(type, out _))
            .WithMessage("Invalid credential type");
        
        // C# 13: Collection patterns in validation
        RuleFor(x => x.Claims)
            .NotEmpty()
            .Must(claims => claims switch
            {
                { Count: > 0 and < 100 } => true,
                _ => false
            })
            .WithMessage("Claims must contain 1-99 items");
        
        // C# 13: Optional parameters in lambda
        RuleFor(x => x.ExpiresAt)
            .Must((date = null) => date is null || date > DateTime.UtcNow)
            .WithMessage("Expiry date must be in the future");
    }
    
    public static ValidationResult Validate(IssueCredentialCommand command)
    {
        var validator = new IssueCredentialCommandValidator();
        return validator.Validate(command);
    }
}
```

## Infrastructure Layer

### EF Core 9 with C# 13

```csharp
namespace NumbatWallet.Infrastructure.Persistence;

// C# 13: Primary constructor for DbContext
public sealed class WalletDbContext(
    DbContextOptions<WalletDbContext> options,
    ITenantContext tenantContext,
    IMediator mediator) : DbContext(options), IUnitOfWork
{
    public DbSet<DigitalCredential> Credentials => Set<DigitalCredential>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<CredentialSchema> Schemas => Set<CredentialSchema>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // C# 13: Enhanced method group conversion
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WalletDbContext).Assembly);
        
        // Global query filters for multi-tenancy
        modelBuilder.Entity<DigitalCredential>()
            .HasQueryFilter(c => c.TenantId == tenantContext.TenantId);
        
        // C# 13: Inline array for performance
        ReadOnlySpan<Type> tenantEntities = [
            typeof(DigitalCredential),
            typeof(Wallet),
            typeof(CredentialSchema)
        ];
        
        foreach (var entityType in tenantEntities)
        {
            modelBuilder.Entity(entityType)
                .HasIndex("TenantId")
                .HasFilter("deleted_at IS NULL");
        }
    }
    
    // C# 13: Simplified async with cancellation token
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // C# 13: Pattern matching for audit entries
        var auditEntries = ChangeTracker.Entries()
            .Where(e => e is { Entity: IAuditable, State: EntityState.Added or EntityState.Modified })
            .Select(e => (Entry: e, Auditable: (IAuditable)e.Entity));
        
        foreach (var (entry, auditable) in auditEntries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    auditable.CreatedAt = DateTime.UtcNow;
                    auditable.CreatedBy = tenantContext.UserId;
                    break;
                case EntityState.Modified:
                    auditable.UpdatedAt = DateTime.UtcNow;
                    auditable.UpdatedBy = tenantContext.UserId;
                    break;
            }
        }
        
        // Dispatch domain events
        await DispatchDomainEvents(cancellationToken);
        
        return await base.SaveChangesAsync(cancellationToken);
    }
    
    private async Task DispatchDomainEvents(CancellationToken cancellationToken)
    {
        var domainEvents = ChangeTracker.Entries<AggregateRoot>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Any())
            .SelectMany(e => e.DomainEvents)
            .ToList();
        
        // C# 13: Parallel foreach with async
        await Parallel.ForEachAsync(domainEvents, cancellationToken, async (domainEvent, ct) =>
        {
            await mediator.Publish(domainEvent, ct);
        });
        
        // Clear events after publishing
        ChangeTracker.Entries<AggregateRoot>()
            .Select(e => e.Entity)
            .ToList()
            .ForEach(e => e.ClearDomainEvents());
    }
}
```

### Repository with C# 13

```csharp
namespace NumbatWallet.Infrastructure.Repositories;

// C# 13: Generic repository with constraints
public sealed class CredentialRepository<TCredential>(
    WalletDbContext context,
    ITenantContext tenantContext,
    IMemoryCache cache) 
    : ICredentialRepository<TCredential> 
    where TCredential : DigitalCredential
{
    // C# 13: Simplified property patterns
    public async Task<TCredential?> GetByIdAsync(
        CredentialId id,
        CancellationToken cancellationToken = default)
    {
        // Try cache first
        if (cache.TryGetValue<TCredential>($"cred:{id}", out var cached))
            return cached;
        
        // C# 13: Enhanced query comprehension
        var credential = await context.Credentials
            .OfType<TCredential>()
            .Include(c => c.Attributes)
            .Include(c => c.Presentations)
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantContext.TenantId, cancellationToken);
        
        // Cache if found
        if (credential is not null)
        {
            cache.Set($"cred:{id}", credential, TimeSpan.FromMinutes(5));
        }
        
        return credential;
    }
    
    // C# 13: Async enumerable with cancellation
    public async IAsyncEnumerable<TCredential> GetByWalletAsync(
        WalletId walletId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = context.Credentials
            .OfType<TCredential>()
            .Where(c => c.WalletId == walletId && c.TenantId == tenantContext.TenantId)
            .OrderByDescending(c => c.IssuedAt)
            .AsAsyncEnumerable();
        
        await foreach (var credential in query.WithCancellation(cancellationToken))
        {
            yield return credential;
        }
    }
}
```

## API Layer

### Minimal APIs with C# 13

```csharp
// Program.cs - .NET 9 with C# 13
using NumbatWallet.API;

var builder = WebApplication.CreateBuilder(args);

// C# 13: Target-typed new
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPresentation();

// .NET 9: Enhanced performance
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// .NET 9: Native AOT support
builder.Services.ConfigureRouteHandlerFilter<ValidationFilter>();

var app = builder.Build();

// C# 13: Enhanced pattern matching in middleware
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    appBuilder => appBuilder.UseRateLimiting()
);

// Minimal API endpoints with C# 13
var api = app.MapGroup("/api/v{version:apiVersion}")
    .RequireAuthorization()
    .WithOpenApi();

// C# 13: Lambda improvements
api.MapPost("/credentials", async Task<Results<Created<CredentialDto>, BadRequest<ProblemDetails>>> 
    (IssueCredentialCommand command, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(command, ct);
    return result switch
    {
        { IsSuccess: true } => TypedResults.Created($"/api/v1/credentials/{result.Value.Id}", result.Value),
        _ => TypedResults.BadRequest(new ProblemDetails { Detail = result.Error })
    };
});

api.MapGet("/credentials/{id}", async Task<Results<Ok<CredentialDto>, NotFound>> 
    (string id, IMediator mediator, CancellationToken ct) =>
{
    var query = new GetCredentialByIdQuery(id);
    var result = await mediator.Send(query, ct);
    return result is not null 
        ? TypedResults.Ok(result) 
        : TypedResults.NotFound();
});

app.Run();

// C# 13: Source generation for JSON
[JsonSerializable(typeof(CredentialDto))]
[JsonSerializable(typeof(IssueCredentialCommand))]
[JsonSerializable(typeof(ProblemDetails))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }
```

## GraphQL API Strategy

The backend implements a **hybrid API approach** to serve different consumer needs:

### Primary GraphQL API
- **Technology**: HotChocolate 13 for GraphQL server
- **Consumers**: Admin Portal, SDKs (.NET, Flutter, JavaScript)
- **Benefits**: Flexible querying, real-time subscriptions, reduced over-fetching
- **Documentation**: See [GraphQL API Documentation](./graphql-api.md)

### REST Adapter for DTP
- **Technology**: Minimal APIs with OpenAPI
- **Consumer**: Digital Trust Platform (DTP) only
- **Reason**: DTP requires REST interface for legacy compatibility
- **Implementation**: Thin adapter layer that internally calls GraphQL resolvers

### API Architecture
```csharp
// Program.cs - Hybrid API setup
var builder = WebApplication.CreateBuilder(args);

// GraphQL Configuration (Primary API)
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddSubscriptionType<Subscription>()
    .AddAuthorization()
    .AddFiltering()
    .AddSorting()
    .AddProjections();

// REST Adapter for DTP
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// GraphQL endpoint (primary)
app.MapGraphQL("/graphql");

// REST endpoints for DTP only
app.MapGroup("/api/v1/dtp")
    .MapDtpEndpoints()  // Minimal API adapter
    .RequireAuthorization("DtpOnly");
```

### Benefits of This Approach
1. **Single source of truth**: Business logic in MediatR handlers
2. **Flexibility**: GraphQL for modern clients, REST for legacy
3. **Performance**: GraphQL reduces network overhead
4. **Real-time**: GraphQL subscriptions for live updates
5. **Maintainability**: REST adapter is thin layer over GraphQL

For detailed GraphQL implementation, see [GraphQL API Documentation](./graphql-api.md).

## Migration Path to .NET 10

### Preparation for .NET 10 and C# 14 (December 2025)

```xml
<!-- Future: .NET 10 project file -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14.0</LangVersion>
    <!-- C# 14 features to adopt -->
    <Features>
      field-keyword;
      params-collections;
      partial-properties;
      lock-object;
    </Features>
  </PropertyGroup>
</Project>
```

### C# 14 Features to Adopt

```csharp
// C# 14: Field keyword in properties (future)
public class FutureCredential
{
    public string Id { get; set => field = value?.ToLowerInvariant(); }
}

// C# 14: Params collections (future)
public void ProcessCredentials(params ReadOnlySpan<CredentialId> ids)
{
    // Process credentials efficiently
}

// C# 14: Partial properties (future)
public partial class CredentialService
{
    public partial CredentialDto CurrentCredential { get; set; }
}

// C# 14: Lock object (future)
public class ThreadSafeRepository
{
    private readonly Lock _lock = new();
    
    public void Update()
    {
        using (_lock.EnterScope())
        {
            // Thread-safe operations
        }
    }
}
```

## Performance Optimizations

### .NET 9 Performance Features

```csharp
// Use frozen collections for read-only data
private static readonly FrozenDictionary<string, CredentialType> CredentialTypes = 
    new Dictionary<string, CredentialType>
    {
        ["driver_license"] = CredentialType.DriverLicense,
        ["proof_of_age"] = CredentialType.ProofOfAge,
        ["working_with_children"] = CredentialType.WorkingWithChildren
    }.ToFrozenDictionary();

// SearchValues for efficient string searching
private static readonly SearchValues<string> ValidClaimNames = 
    SearchValues.Create(["name", "dob", "address", "license_number"], StringComparison.OrdinalIgnoreCase);

// Use ITimer for better testability
public class TokenRefreshService(ITimer timer, ITokenService tokenService)
{
    public void Start() => timer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(5));
}
```

## Deployment Configuration

### Docker Support for .NET 9

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["NumbatWallet.API/NumbatWallet.API.csproj", "NumbatWallet.API/"]
RUN dotnet restore "NumbatWallet.API/NumbatWallet.API.csproj"
COPY . .
WORKDIR "/src/NumbatWallet.API"
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NumbatWallet.API.dll"]
```

## Summary

This architecture leverages .NET 9 and C# 13 features for the POA phase while maintaining a clear migration path to .NET 10 and C# 14 for production. The clean architecture ensures maintainability and testability throughout the evolution of the platform.