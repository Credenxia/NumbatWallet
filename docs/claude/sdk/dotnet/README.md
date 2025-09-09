# Credenxia .NET SDK

![Version](https://img.shields.io/nuget/v/Credenxia.SDK)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-Proprietary-red)
![Build](https://img.shields.io/badge/build-passing-green)

Official .NET SDK for the Credenxia Digital Wallet and Verifiable Credentials platform.

## Features

- 🔐 **Enterprise-Grade Security** - Built on .NET 8 with modern security practices
- 🚀 **High Performance** - Async/await patterns and efficient resource management
- 📜 **W3C Standards** - Full compliance with VC Data Model 2.0 and DID Core
- 🔄 **Resilient Communication** - Polly integration for retry and circuit breaker
- 📊 **Observability** - OpenTelemetry support for distributed tracing
- 🧪 **Testable** - Dependency injection and interface-based design
- 🔌 **Extensible** - Plugin architecture for custom implementations

## Installation

### Package Manager Console

```powershell
Install-Package Credenxia.SDK -Version 1.0.0
```

### .NET CLI

```bash
dotnet add package Credenxia.SDK --version 1.0.0
```

### PackageReference

```xml
<PackageReference Include="Credenxia.SDK" Version="1.0.0" />
```

## Quick Start

### Configure Services

```csharp
using Credenxia.SDK;
using Credenxia.SDK.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Credenxia SDK services
builder.Services.AddCredenxia(options =>
{
    options.BaseUrl = "https://api.credenxia.gov.au";
    options.ApiKey = builder.Configuration["Credenxia:ApiKey"];
    options.Environment = CredenxiaEnvironment.Production;
    options.EnableRetryPolicy = true;
    options.EnableCircuitBreaker = true;
    options.EnableTelemetry = true;
});

// Configure HTTP client
builder.Services.AddHttpClient<ICredenxiaClient>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

var app = builder.Build();
```

### Basic Usage

```csharp
using Credenxia.SDK;
using Credenxia.SDK.Models;

public class WalletService
{
    private readonly ICredenxiaClient _client;
    private readonly ILogger<WalletService> _logger;

    public WalletService(ICredenxiaClient client, ILogger<WalletService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Wallet> CreateWalletAsync(string userId)
    {
        try
        {
            var request = new CreateWalletRequest
            {
                UserId = userId,
                Type = WalletType.Personal,
                Metadata = new Dictionary<string, object>
                {
                    ["created_by"] = "ServiceWA",
                    ["platform"] = "Backend Service"
                }
            };

            var wallet = await _client.Wallets.CreateAsync(request);
            
            _logger.LogInformation("Wallet created successfully: {WalletId}", wallet.Id);
            return wallet;
        }
        catch (CredenxiaException ex)
        {
            _logger.LogError(ex, "Failed to create wallet for user {UserId}", userId);
            throw;
        }
    }
}
```

### Issue a Credential

```csharp
public async Task<Credential> IssueCredentialAsync(IssuanceRequest request)
{
    // Validate request
    var validationResult = await ValidateIssuanceRequestAsync(request);
    if (!validationResult.IsValid)
    {
        throw new ValidationException(validationResult.Errors);
    }

    // Build credential
    var credentialRequest = new IssueCredentialRequest
    {
        CredentialType = request.Type,
        Subject = new CredentialSubject
        {
            Id = request.SubjectId,
            Claims = request.Claims
        },
        WalletId = request.WalletId,
        ExpirationDate = DateTime.UtcNow.AddYears(1),
        Options = new IssuanceOptions
        {
            ProofType = ProofType.BbsBlsSignature2020,
            ProofPurpose = ProofPurpose.AssertionMethod,
            EnableRevocation = true
        }
    };

    // Issue credential
    var credential = await _client.Credentials.IssueAsync(credentialRequest);
    
    // Audit the issuance
    await AuditIssuanceAsync(credential);
    
    return credential;
}
```

### Verify a Presentation

```csharp
public async Task<VerificationResult> VerifyPresentationAsync(
    VerifiablePresentation presentation)
{
    var verificationRequest = new VerificationRequest
    {
        Presentation = presentation,
        Options = new VerificationOptions
        {
            Challenge = GenerateChallenge(),
            Domain = "verifier.example.com",
            CheckStatus = true,
            CheckTrustRegistry = true,
            AllowExpiredCredentials = false
        }
    };

    var result = await _client.Presentations.VerifyAsync(verificationRequest);
    
    if (result.IsValid)
    {
        _logger.LogInformation("Presentation verified successfully");
        
        // Extract verified claims
        var claims = result.VerifiedClaims;
        await ProcessVerifiedClaimsAsync(claims);
    }
    else
    {
        _logger.LogWarning("Presentation verification failed: {Errors}", 
            string.Join(", ", result.Errors));
    }
    
    return result;
}
```

## Advanced Features

### Batch Operations

```csharp
// Issue multiple credentials in a batch
var batchRequest = new BatchIssuanceRequest
{
    Credentials = new[]
    {
        new IssueCredentialRequest { /* ... */ },
        new IssueCredentialRequest { /* ... */ },
        new IssueCredentialRequest { /* ... */ }
    },
    Options = new BatchOptions
    {
        ContinueOnError = true,
        MaxParallelism = 5
    }
};

var batchResult = await _client.Credentials.IssueBatchAsync(batchRequest);

foreach (var result in batchResult.Results)
{
    if (result.Success)
    {
        _logger.LogInformation("Credential {Id} issued", result.Credential.Id);
    }
    else
    {
        _logger.LogError("Failed to issue credential: {Error}", result.Error);
    }
}
```

### Trust Registry Integration

```csharp
public async Task<bool> ValidateIssuerAsync(string issuerDid)
{
    // Check if issuer is in trust registry
    var trustRegistry = await _client.Trust.GetRegistryAsync();
    
    var issuer = await trustRegistry.GetIssuerAsync(issuerDid);
    if (issuer == null)
    {
        _logger.LogWarning("Issuer {Did} not found in trust registry", issuerDid);
        return false;
    }
    
    // Validate issuer status
    if (issuer.Status != IssuerStatus.Active)
    {
        _logger.LogWarning("Issuer {Did} is not active: {Status}", 
            issuerDid, issuer.Status);
        return false;
    }
    
    // Check issuer's authority for credential type
    var hasAuthority = issuer.AuthorizedCredentialTypes
        .Contains("DriversLicense");
    
    return hasAuthority;
}
```

### Resilience Patterns

```csharp
// Configure Polly policies
private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => !msg.IsSuccessStatusCode)
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                var logger = context.Values["logger"] as ILogger;
                logger?.LogWarning("Retry {RetryCount} after {Delay}ms", 
                    retryCount, timespan.TotalMilliseconds);
            });
}

private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            5,
            TimeSpan.FromSeconds(30),
            onBreak: (result, timespan) =>
            {
                // Circuit opened
            },
            onReset: () =>
            {
                // Circuit closed
            });
}
```

### Distributed Tracing

```csharp
// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("wallet-service"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddCredenxiaInstrumentation() // Custom instrumentation
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:4317");
            });
    });
```

## Testing

### Unit Testing

```csharp
[TestClass]
public class WalletServiceTests
{
    private Mock<ICredenxiaClient> _mockClient;
    private Mock<ILogger<WalletService>> _mockLogger;
    private WalletService _service;

    [TestInitialize]
    public void Setup()
    {
        _mockClient = new Mock<ICredenxiaClient>();
        _mockLogger = new Mock<ILogger<WalletService>>();
        _service = new WalletService(_mockClient.Object, _mockLogger.Object);
    }

    [TestMethod]
    public async Task CreateWallet_Success_ReturnsWallet()
    {
        // Arrange
        var expectedWallet = new Wallet { Id = Guid.NewGuid() };
        _mockClient.Setup(x => x.Wallets.CreateAsync(It.IsAny<CreateWalletRequest>()))
            .ReturnsAsync(expectedWallet);

        // Act
        var result = await _service.CreateWalletAsync("user-123");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedWallet.Id, result.Id);
        _mockClient.Verify(x => x.Wallets.CreateAsync(
            It.Is<CreateWalletRequest>(r => r.UserId == "user-123")), 
            Times.Once);
    }
}
```

### Integration Testing

```csharp
[TestClass]
[TestCategory("Integration")]
public class CredenxiaIntegrationTests
{
    private ICredenxiaClient _client;

    [TestInitialize]
    public void Setup()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json")
            .Build();

        _client = new CredenxiaClient(new CredenxiaOptions
        {
            BaseUrl = configuration["Credenxia:BaseUrl"],
            ApiKey = configuration["Credenxia:ApiKey"],
            Environment = CredenxiaEnvironment.Sandbox
        });
    }

    [TestMethod]
    public async Task EndToEnd_WalletCreation_And_CredentialIssuance()
    {
        // Create wallet
        var wallet = await _client.Wallets.CreateAsync(new CreateWalletRequest
        {
            UserId = Guid.NewGuid().ToString(),
            Type = WalletType.Personal
        });

        Assert.IsNotNull(wallet);

        // Issue credential
        var credential = await _client.Credentials.IssueAsync(
            new IssueCredentialRequest
            {
                WalletId = wallet.Id,
                CredentialType = "TestCredential",
                Subject = new { name = "Test User" }
            });

        Assert.IsNotNull(credential);
        Assert.AreEqual(wallet.Id, credential.WalletId);
    }
}
```

## Configuration

### appsettings.json

```json
{
  "Credenxia": {
    "BaseUrl": "https://api.credenxia.gov.au",
    "ApiKey": "${CREDENXIA_API_KEY}",
    "Environment": "Production",
    "Timeout": 30,
    "MaxRetries": 3,
    "EnableTelemetry": true,
    "CacheOptions": {
      "Enabled": true,
      "SlidingExpiration": "00:05:00",
      "AbsoluteExpiration": "00:30:00"
    }
  }
}
```

### Environment Variables

```bash
CREDENXIA_API_KEY=your-api-key
CREDENXIA_ENVIRONMENT=Production
CREDENXIA_ENABLE_CACHE=true
CREDENXIA_LOG_LEVEL=Information
```

## Performance Optimization

### Caching

```csharp
// Configure memory cache
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICredenxiaCache, MemoryCredenxiaCache>();

// Use cached trust registry
public class CachedTrustRegistry : ITrustRegistry
{
    private readonly IMemoryCache _cache;
    private readonly ITrustRegistry _inner;

    public async Task<Issuer> GetIssuerAsync(string did)
    {
        return await _cache.GetOrCreateAsync($"issuer:{did}", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(5);
            return await _inner.GetIssuerAsync(did);
        });
    }
}
```

### Connection Pooling

```csharp
// Configure HTTP client factory with connection pooling
builder.Services.AddHttpClient<ICredenxiaClient>()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
        MaxConnectionsPerServer = 10
    });
```

## Security Best Practices

1. **API Key Management**: Never hardcode API keys. Use Azure Key Vault or environment variables
2. **Certificate Pinning**: Enable certificate pinning in production
3. **Request Signing**: Enable request signing for sensitive operations
4. **Audit Logging**: Log all credential operations for compliance
5. **Rate Limiting**: Implement client-side rate limiting to prevent abuse

## Troubleshooting

### Common Issues

**Issue**: `CredenxiaException: Unauthorized`
```csharp
// Solution: Check API key configuration
var apiKey = configuration["Credenxia:ApiKey"];
if (string.IsNullOrEmpty(apiKey))
{
    throw new ConfigurationException("API key not configured");
}
```

**Issue**: `TimeoutException`
```csharp
// Solution: Increase timeout or implement retry
options.Timeout = TimeSpan.FromSeconds(60);
options.EnableRetryPolicy = true;
```

**Issue**: `RateLimitException`
```csharp
// Solution: Implement exponential backoff
await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
```

## API Reference

Complete API documentation: https://docs.credenxia.gov.au/sdk/dotnet

## Support

- **Documentation**: https://docs.credenxia.gov.au
- **NuGet Package**: https://www.nuget.org/packages/Credenxia.SDK
- **GitHub**: https://github.com/credenxia/dotnet-sdk
- **Email**: sdk-support@credenxia.gov.au

## License

This SDK is proprietary software. See LICENSE file for details.

## Contributing

Contributions are welcome! Please see our [Contributing Guide](CONTRIBUTING.md).

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history.

---

Built with .NET for enterprise-grade digital identity solutions