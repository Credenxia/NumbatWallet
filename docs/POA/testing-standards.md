# POA Testing Standards

**Version:** 1.0  
**Date:** September 10, 2025  
**Applies to:** POA Phase (October 1 - November 4, 2025)

## Table of Contents
- [Test-Driven Development Mandate](#test-driven-development-mandate)
- [Testing Structure](#testing-structure)
- [Test Implementation Schedule](#test-implementation-schedule)
- [Test Categories and Coverage](#test-categories-and-coverage)
- [TDD Workflow for POA](#tdd-workflow-for-poa)
- [Testing Tools and Frameworks](#testing-tools-and-frameworks)
- [CI/CD Integration](#cicd-integration)
- [Test Data Management](#test-data-management)
- [Performance Testing Requirements](#performance-testing-requirements)
- [Security Testing Requirements](#security-testing-requirements)

## Test-Driven Development Mandate

### POA TDD Policy
**MANDATORY: All POA development MUST follow Test-Driven Development (TDD)**

No code will be accepted without corresponding tests written FIRST. This is non-negotiable for POA success.

### TDD Enforcement
```yaml
# CI/CD will enforce:
- Test files must have earlier timestamps than implementation files
- Coverage must increase or maintain with each commit
- All tests must pass before merge
- No ignored or skipped tests allowed
```

## Testing Structure

### Project Test Organization
```
/src/
├── NumbatWallet.sln
└── tests/
    ├── Unit/
    │   ├── NumbatWallet.Domain.Tests/
    │   │   ├── Entities/
    │   │   ├── ValueObjects/
    │   │   └── Services/
    │   ├── NumbatWallet.Application.Tests/
    │   │   ├── Commands/
    │   │   ├── Queries/
    │   │   └── Validators/
    │   └── NumbatWallet.Infrastructure.Tests/
    │       ├── Persistence/
    │       └── External/
    ├── Integration/
    │   ├── NumbatWallet.API.Tests/
    │   │   ├── GraphQL/
    │   │   └── REST/
    │   └── NumbatWallet.Database.Tests/
    ├── E2E/
    │   └── NumbatWallet.E2E.Tests/
    └── Performance/
        └── NumbatWallet.LoadTests/
```

### SDK Test Structure
```
/sdks/
├── flutter/
│   └── test/
│       ├── unit/
│       ├── widget/
│       └── integration/
├── dotnet/
│   └── NumbatWallet.SDK.Tests/
└── typescript/
    └── __tests__/
```

## Test Implementation Schedule

### Week 1: Foundation Testing (Oct 1-4)
| Task | Priority | Owner | GitHub Issue |
|------|----------|-------|--------------|
| Set up test projects structure | P0 | DevOps | #81 |
| Configure test databases | P0 | DevOps | #82 |
| Create test data builders | P0 | Backend | #83 |
| Domain entity unit tests | P0 | Backend | #84 |
| Infrastructure tests | P0 | DevOps | #85 |
| Flutter SDK unit tests | P0 | Mobile | #86 |
| CI/CD test pipeline | P0 | DevOps | #87 |

### Week 2: Integration Testing (Oct 7-11)
| Task | Priority | Owner | GitHub Issue |
|------|----------|-------|--------------|
| Authentication tests | P0 | Backend | #88 |
| Credential operation tests | P0 | Backend | #89 |
| Multi-tenant isolation tests | P0 | Backend | #90 |
| GraphQL API tests | P0 | Backend | #91 |
| REST adapter tests | P0 | Backend | #92 |
| PKI integration tests | P0 | Security | #93 |
| Cross-SDK integration tests | P1 | Team | #94 |

### Week 3: E2E and Performance (Oct 14-18)
| Task | Priority | Owner | GitHub Issue |
|------|----------|-------|--------------|
| E2E user journey tests | P0 | QA | #95 |
| Load testing (100 users) | P0 | QA | #96 |
| Performance benchmarks | P0 | QA | #97 |
| Security penetration tests | P0 | Security | #98 |
| Compliance validation tests | P1 | QA | #99 |

## Test Categories and Coverage

### Coverage Requirements by Layer
| Layer | Min Coverage | POA Target | Enforcement |
|-------|-------------|------------|-------------|
| Domain | 95% | 100% | Block PR if below |
| Application | 85% | 95% | Block PR if below |
| Infrastructure | 80% | 90% | Warning if below |
| API | 80% | 90% | Warning if below |
| SDKs | 85% | 90% | Block PR if below |
| **Overall** | **85%** | **92%** | **Block if below 85%** |

### Test Categories
```csharp
[Category("Unit")]          // <100ms, no dependencies
[Category("Integration")]   // <1s, real dependencies
[Category("E2E")]           // <10s, full stack
[Category("Performance")]   // Variable, load testing
[Category("Security")]      // Variable, security validation
[Category("Smoke")]         // <5s, critical paths only
[Category("TDD")]           // Marks test written before code
```

## TDD Workflow for POA

### Step-by-Step TDD Process

#### 1. Create GitHub Issue Branch
```bash
git checkout -b feature/POA-XXX-tdd-implementation
```

#### 2. Write Failing Test First (RED)
```csharp
// File: tests/Unit/NumbatWallet.Domain.Tests/CredentialTests.cs
// Created: 2025-10-01 09:00:00

[Fact]
[Category("Unit")]
[Category("TDD")]
public void IssueCredential_WithValidClaims_CreatesActiveCredential()
{
    // Arrange
    var tenantId = TenantId.Parse("wa-gov");
    var walletId = WalletId.New();
    var claims = new ClaimSet
    {
        { "firstName", "John" },
        { "lastName", "Doe" },
        { "licenseNumber", "WA123456" }
    };
    
    // Act
    var result = DigitalCredential.Issue(tenantId, walletId, claims);
    
    // Assert
    result.Should().BeSuccessful();
    result.Value.Should().NotBeNull();
    result.Value.Status.Should().Be(CredentialStatus.Active);
    result.Value.TenantId.Should().Be(tenantId);
}
```

#### 3. Run Test to Verify Failure
```bash
dotnet test --filter "FullyQualifiedName~CredentialTests"
# Expected: Test fails because DigitalCredential.Issue doesn't exist
```

#### 4. Write Minimal Code (GREEN)
```csharp
// File: src/NumbatWallet.Domain/Entities/DigitalCredential.cs
// Created: 2025-10-01 09:15:00 (AFTER test file)

public class DigitalCredential
{
    public static Result<DigitalCredential> Issue(
        TenantId tenantId, 
        WalletId walletId, 
        ClaimSet claims)
    {
        var credential = new DigitalCredential
        {
            Id = CredentialId.New(),
            TenantId = tenantId,
            WalletId = walletId,
            Status = CredentialStatus.Active,
            Claims = claims,
            IssuedAt = DateTime.UtcNow
        };
        
        return Result<DigitalCredential>.Success(credential);
    }
}
```

#### 5. Run Test to Verify Pass
```bash
dotnet test --filter "FullyQualifiedName~CredentialTests"
# Expected: Test passes
```

#### 6. Refactor with Confidence (REFACTOR)
```csharp
public class DigitalCredential : AggregateRoot
{
    private DigitalCredential() { } // For EF
    
    public static Result<DigitalCredential> Issue(
        TenantId tenantId, 
        WalletId walletId, 
        ClaimSet claims)
    {
        // Add validation
        if (tenantId == null)
            return Result<DigitalCredential>.Failure("TenantId is required");
        
        if (walletId == null)
            return Result<DigitalCredential>.Failure("WalletId is required");
        
        if (!claims.IsValid())
            return Result<DigitalCredential>.Failure("Invalid claims");
        
        var credential = new DigitalCredential
        {
            Id = CredentialId.New(),
            TenantId = tenantId,
            WalletId = walletId,
            Status = CredentialStatus.Active,
            Claims = claims,
            IssuedAt = DateTime.UtcNow
        };
        
        // Raise domain event
        credential.AddDomainEvent(new CredentialIssuedEvent(
            credential.Id, 
            credential.TenantId, 
            credential.WalletId));
        
        return Result<DigitalCredential>.Success(credential);
    }
}
```

#### 7. Verify Tests Still Pass
```bash
dotnet test
# Expected: All tests pass
```

#### 8. Commit Test and Implementation Together
```bash
git add tests/Unit/NumbatWallet.Domain.Tests/CredentialTests.cs
git add src/NumbatWallet.Domain/Entities/DigitalCredential.cs
git commit -m "POA-XXX: Implement credential issuance with TDD

- Added failing test for credential issuance
- Implemented Issue method on DigitalCredential
- Added validation and domain events
- Test coverage: 100% for Issue method"
```

## Testing Tools and Frameworks

### .NET Testing Stack
```xml
<!-- Test frameworks in .csproj -->
<PackageReference Include="xunit" Version="2.6.1" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="Moq" Version="4.20.69" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0" />
<PackageReference Include="Testcontainers.PostgreSql" Version="3.6.0" />
<PackageReference Include="coverlet.collector" Version="6.0.0" />
```

### Flutter Testing Stack
```yaml
# pubspec.yaml
dev_dependencies:
  flutter_test:
    sdk: flutter
  mockito: ^5.4.4
  build_runner: ^2.4.6
  integration_test:
    sdk: flutter
  flutter_driver:
    sdk: flutter
```

### Performance Testing Tools
- **NBomber**: .NET load testing framework
- **k6**: JavaScript-based load testing
- **Apache JMeter**: Comprehensive performance testing

## CI/CD Integration

### GitHub Actions Test Pipeline
```yaml
name: POA Test Pipeline

on:
  pull_request:
    branches: [ main ]
  push:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      postgres:
        image: postgres:15
        env:
          POSTGRES_PASSWORD: test
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
          
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore
      
    - name: Run Unit Tests
      run: dotnet test --no-build --filter Category=Unit --logger:trx --collect:"XPlat Code Coverage"
      
    - name: Run Integration Tests
      run: dotnet test --no-build --filter Category=Integration --logger:trx
      
    - name: Check Coverage
      run: |
        dotnet tool install --global dotnet-reportgenerator-globaltool
        reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coveragereport -reporttypes:Html
        
    - name: Enforce Coverage Requirements
      run: |
        coverage=$(xmllint --xpath "string(//coverage/@line-rate)" coverage.cobertura.xml)
        if (( $(echo "$coverage < 0.85" | bc -l) )); then
          echo "Coverage ${coverage} is below required 85%"
          exit 1
        fi
        
    - name: Upload Test Results
      uses: actions/upload-artifact@v3
      with:
        name: test-results
        path: |
          **/*.trx
          coveragereport/
```

### Pre-commit Hooks
```bash
#!/bin/bash
# .git/hooks/pre-commit

# Run tests before commit
echo "Running tests..."
dotnet test --filter Category=Unit

if [ $? -ne 0 ]; then
    echo "Tests failed. Commit aborted."
    exit 1
fi

# Check coverage
coverage=$(dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=json | grep "Line coverage" | sed 's/.*: \(.*\)%/\1/')
if (( $(echo "$coverage < 85" | bc -l) )); then
    echo "Coverage $coverage% is below 85%. Commit aborted."
    exit 1
fi

echo "All tests passed with $coverage% coverage"
```

## Test Data Management

### Test Data Builders
```csharp
public class CredentialBuilder
{
    private TenantId _tenantId = TenantId.Parse("test-tenant");
    private WalletId _walletId = WalletId.New();
    private CredentialType _type = CredentialType.DriverLicense;
    private Dictionary<string, object> _claims = new();
    private CredentialStatus _status = CredentialStatus.Active;
    
    public CredentialBuilder ForTenant(string tenantId)
    {
        _tenantId = TenantId.Parse(tenantId);
        return this;
    }
    
    public CredentialBuilder ForWallet(WalletId walletId)
    {
        _walletId = walletId;
        return this;
    }
    
    public CredentialBuilder WithType(CredentialType type)
    {
        _type = type;
        return this;
    }
    
    public CredentialBuilder WithClaim(string key, object value)
    {
        _claims[key] = value;
        return this;
    }
    
    public CredentialBuilder AsRevoked()
    {
        _status = CredentialStatus.Revoked;
        return this;
    }
    
    public DigitalCredential Build()
    {
        var result = DigitalCredential.Issue(_tenantId, _walletId, new ClaimSet(_claims));
        var credential = result.Value;
        
        if (_status == CredentialStatus.Revoked)
        {
            credential.Revoke(RevocationReason.TestPurpose);
        }
        
        return credential;
    }
}

// Usage in tests
var credential = new CredentialBuilder()
    .ForTenant("wa-gov")
    .WithClaim("firstName", "John")
    .WithClaim("lastName", "Doe")
    .Build();
```

### Test Fixtures
```csharp
public class DatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer _container;
    public string ConnectionString { get; private set; }
    
    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("wallet_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
            
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
        
        // Run migrations
        using var context = CreateContext();
        await context.Database.MigrateAsync();
    }
    
    public WalletDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WalletDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new WalletDbContext(options);
    }
    
    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
```

## Performance Testing Requirements

### Load Test Scenarios
```csharp
[Test]
[Category("Performance")]
public void CredentialIssuance_100ConcurrentUsers()
{
    var scenario = Scenario.Create("credential_issuance", async context =>
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/credentials/issue")
        {
            Content = JsonContent.Create(new
            {
                walletId = $"wallet-{context.ScenarioInfo.ThreadId}",
                type = "DriverLicense",
                claims = new { /* test data */ }
            })
        };
        
        var response = await context.HttpClient.SendAsync(request);
        
        return response.IsSuccessStatusCode 
            ? Response.Ok() 
            : Response.Fail($"Status: {response.StatusCode}");
    })
    .WithLoadSimulations(
        Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromSeconds(30))
    );
    
    var stats = NBomberRunner
        .RegisterScenarios(scenario)
        .Run();
    
    // Assertions
    stats.AllOkCount.Should().BeGreaterThan(2900); // >95% success
    stats.ScenarioStats[0].Ok.Latency.Mean.Should().BeLessThan(500); // <500ms avg
    stats.ScenarioStats[0].Ok.Latency.P95.Should().BeLessThan(1000); // <1s p95
}
```

### Performance Targets
| Metric | Target | Maximum | Test Method |
|--------|--------|---------|-------------|
| API Response (p50) | 200ms | 300ms | Load test |
| API Response (p95) | 400ms | 500ms | Load test |
| API Response (p99) | 800ms | 1000ms | Load test |
| Throughput | 500 req/s | 1000 req/s | Stress test |
| Concurrent Users | 100 | 200 | Load test |
| CPU Usage | <70% | <85% | Monitor during test |
| Memory Usage | <2GB | <4GB | Monitor during test |

## Security Testing Requirements

### Security Test Categories
```csharp
[Test]
[Category("Security")]
public class AuthenticationSecurityTests
{
    [Fact]
    public async Task API_RejectsInvalidTokens()
    {
        // Test with expired token
        var expiredToken = GenerateExpiredToken();
        var response = await CallApiWithToken(expiredToken);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        
        // Test with tampered token
        var tamperedToken = TamperWithToken(ValidToken);
        response = await CallApiWithToken(tamperedToken);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task TenantIsolation_PreventsDataLeakage()
    {
        // Create credential in tenant A
        var credentialA = await CreateCredentialForTenant("tenant-a");
        
        // Try to access from tenant B
        var tokenB = GenerateTokenForTenant("tenant-b");
        var response = await GetCredential(credentialA.Id, tokenB);
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public void SensitiveData_IsEncrypted()
    {
        var credential = new CredentialBuilder()
            .WithClaim("ssn", "123-45-6789")
            .Build();
            
        var json = JsonSerializer.Serialize(credential);
        json.Should().NotContain("123-45-6789");
        json.Should().MatchRegex(@"""ssn""\s*:\s*""[A-Za-z0-9+/=]+""");
    }
}
```

### Security Testing Checklist
- [ ] Authentication bypass attempts
- [ ] Authorization elevation attempts
- [ ] SQL injection tests
- [ ] XSS vulnerability tests
- [ ] CSRF protection validation
- [ ] Rate limiting verification
- [ ] Sensitive data encryption
- [ ] Multi-tenant isolation
- [ ] Session management
- [ ] Input validation

## Test Reporting

### Coverage Report Generation
```bash
# Generate coverage report
dotnet test /p:CollectCoverage=true \
            /p:CoverletOutputFormat=cobertura \
            /p:CoverletOutput=./coverage/

# Generate HTML report
reportgenerator -reports:coverage/coverage.cobertura.xml \
                -targetdir:coverage/report \
                -reporttypes:Html

# Open report
open coverage/report/index.html
```

### Test Result Dashboard
```yaml
# GitHub Actions badge in README.md
[![Tests](https://github.com/Credenxia/NumbatWallet/actions/workflows/test.yml/badge.svg)](https://github.com/Credenxia/NumbatWallet/actions/workflows/test.yml)
[![Coverage](https://codecov.io/gh/Credenxia/NumbatWallet/branch/main/graph/badge.svg)](https://codecov.io/gh/Credenxia/NumbatWallet)
```

## POA Testing Success Criteria

### Week 1 Exit Criteria
- [ ] Test infrastructure operational
- [ ] 80% domain layer coverage
- [ ] All unit tests passing
- [ ] CI/CD pipeline working

### Week 2 Exit Criteria
- [ ] Integration tests complete
- [ ] 85% overall coverage
- [ ] Security tests passing
- [ ] Performance baseline established

### Week 3 Exit Criteria
- [ ] E2E tests passing
- [ ] Load tests meeting targets
- [ ] 90% overall coverage
- [ ] No critical security issues

### Week 4-5 Exit Criteria
- [ ] Regression suite complete
- [ ] 92% overall coverage
- [ ] All acceptance tests passing
- [ ] Test documentation complete

---

**Remember:** No code without tests. Write the test first, watch it fail, make it pass, then refactor. This is the path to POA success.