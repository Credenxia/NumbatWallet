# NumbatWallet Testing Standards

## Table of Contents
- [Testing Philosophy](#testing-philosophy)
- [Test Strategy](#test-strategy)
- [Unit Testing](#unit-testing)
- [Integration Testing](#integration-testing)
- [End-to-End Testing](#end-to-end-testing)
- [Performance Testing](#performance-testing)
- [Security Testing](#security-testing)
- [Test Data Management](#test-data-management)
- [Acceptance Criteria](#acceptance-criteria)

## Testing Philosophy

### Core Principles

**Tests represent requirements** - Tests are executable specifications that validate business requirements and compliance standards for digital wallet operations.

1. **Test-First Development** - Write tests before implementation
2. **Behavior-Driven** - Tests describe expected behavior, not implementation
3. **Isolated & Fast** - Unit tests run in milliseconds
4. **Deterministic** - Same input always produces same output
5. **Self-Documenting** - Test names explain what and why

### Testing Pyramid

```
        /\
       /E2E\      <- End-to-End Tests (5%)
      /------\       Few, critical user journeys
     /  Integ \   <- Integration Tests (25%)
    /----------\     API, database, external services
   /    Unit    \ <- Unit Tests (70%)
  /--------------\   Domain logic, business rules
```

## Test Strategy

### Coverage Requirements

| Component | Minimum Coverage | Target Coverage |
|-----------|-----------------|-----------------|
| Domain Layer | 95% | 100% |
| Application Layer | 90% | 95% |
| Infrastructure Layer | 80% | 90% |
| API Controllers | 85% | 95% |
| Flutter SDK | 85% | 90% |
| Overall | 85% | 95% |

### Test Categories

```csharp
[Category("Unit")]          // Fast, isolated tests
[Category("Integration")]   // Database, external services
[Category("E2E")]           // Full system tests
[Category("Performance")]   // Load and stress tests
[Category("Security")]      // Security validation
[Category("Smoke")]         // Critical path tests
```

## Unit Testing

### Domain Logic Testing

```csharp
public class DigitalCredentialTests
{
    [Fact]
    public void Issue_ValidData_CreatesCredential()
    {
        // Arrange
        var tenantId = TenantId.New();
        var walletId = WalletId.New();
        var schema = CredentialSchemaFactory.CreateDriverLicense();
        var claims = ValidClaimsFactory.CreateDriverLicenseClaims();
        
        // Act
        var credential = DigitalCredential.Issue(
            tenantId, walletId, schema, claims);
        
        // Assert
        credential.Should().NotBeNull();
        credential.Status.Should().Be(CredentialStatus.Active);
        credential.TenantId.Should().Be(tenantId);
        credential.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<CredentialIssuedEvent>();
    }
    
    [Fact]
    public void Revoke_ActiveCredential_ChangesStatusToRevoked()
    {
        // Arrange
        var credential = CredentialFactory.CreateActive();
        var reason = RevocationReason.Compromised;
        
        // Act
        credential.Revoke(reason);
        
        // Assert
        credential.Status.Should().Be(CredentialStatus.Revoked);
        credential.GetDomainEvents().Should().Contain(
            e => e is CredentialRevokedEvent evt && 
                 evt.Reason == reason);
    }
    
    [Fact]
    public void Revoke_AlreadyRevoked_ThrowsDomainException()
    {
        // Arrange
        var credential = CredentialFactory.CreateRevoked();
        
        // Act
        var act = () => credential.Revoke(RevocationReason.Expired);
        
        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Credential already revoked");
    }
}
```

### Command Handler Testing

```csharp
public class IssueCredentialCommandHandlerTests
{
    private readonly Mock<ICredentialRepository> _repository;
    private readonly Mock<IPKIService> _pkiService;
    private readonly IssueCredentialCommandHandler _handler;
    
    public IssueCredentialCommandHandlerTests()
    {
        _repository = new Mock<ICredentialRepository>();
        _pkiService = new Mock<IPKIService>();
        _handler = new IssueCredentialCommandHandler(
            _repository.Object, _pkiService.Object);
    }
    
    [Fact]
    public async Task Handle_ValidCommand_IssuesAndStoresCredential()
    {
        // Arrange
        var command = new IssueCredentialCommand
        {
            TenantId = TenantId.New(),
            WalletId = WalletId.New(),
            Type = CredentialType.DriverLicense,
            Claims = ValidClaimsFactory.CreateDriverLicenseClaims()
        };
        
        var signature = CryptographicSignature.Create();
        _pkiService.Setup(x => x.SignCredentialAsync(It.IsAny<DigitalCredential>()))
            .ReturnsAsync(signature);
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.Should().NotBeEmpty();
        _repository.Verify(x => x.AddAsync(
            It.Is<DigitalCredential>(c => 
                c.TenantId == command.TenantId &&
                c.WalletId == command.WalletId)), 
            Times.Once);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}
```

### Flutter SDK Testing

```dart
void main() {
  group('WalletClient', () {
    late WalletClient client;
    late MockHttpClient mockHttp;
    
    setUp(() {
      mockHttp = MockHttpClient();
      client = WalletClient(httpClient: mockHttp);
    });
    
    test('fetchCredential returns credential when successful', () async {
      // Arrange
      final credentialId = 'cred-123';
      final response = CredentialResponse(
        id: credentialId,
        type: 'DriverLicense',
        status: 'Active',
      );
      
      when(mockHttp.get(any))
        .thenAnswer((_) async => Response(jsonEncode(response), 200));
      
      // Act
      final credential = await client.fetchCredential(credentialId);
      
      // Assert
      expect(credential.id, equals(credentialId));
      expect(credential.status, equals(CredentialStatus.active));
    });
    
    test('fetchCredential throws when unauthorized', () async {
      // Arrange
      when(mockHttp.get(any))
        .thenAnswer((_) async => Response('Unauthorized', 401));
      
      // Act & Assert
      expect(
        () => client.fetchCredential('cred-123'),
        throwsA(isA<UnauthorizedException>()),
      );
    });
  });
}
```

## Integration Testing

### API Integration Tests

```csharp
public class CredentialApiIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    
    public CredentialApiIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace real services with test doubles
                    services.RemoveAll<IPKIService>();
                    services.AddSingleton<IPKIService, MockPKIService>();
                });
            })
            .CreateClient();
    }
    
    [Fact]
    public async Task POST_IssueCredential_ReturnsCreatedCredential()
    {
        // Arrange
        var request = new IssueCredentialRequest
        {
            WalletId = "wallet-123",
            Type = "DriverLicense",
            Claims = new { /* ... */ }
        };
        
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", TestTokens.ValidAdminToken);
        
        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/v1/credentials/issue", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var credential = await response.Content.ReadFromJsonAsync<CredentialDto>();
        credential.Should().NotBeNull();
        credential.Id.Should().NotBeEmpty();
        credential.Status.Should().Be("Active");
    }
}
```

### Database Integration Tests

```csharp
public class CredentialRepositoryTests : IDisposable
{
    private readonly WalletDbContext _context;
    private readonly CredentialRepository _repository;
    
    public CredentialRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<WalletDbContext>()
            .UseNpgsql("Host=localhost;Database=wallet_test;Username=test;Password=test")
            .Options;
            
        _context = new WalletDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new CredentialRepository(_context);
    }
    
    [Fact]
    public async Task GetByWalletId_ReturnsOnlyWalletCredentials()
    {
        // Arrange
        var walletId = WalletId.New();
        var otherWalletId = WalletId.New();
        
        await _repository.AddAsync(CredentialFactory.Create(walletId));
        await _repository.AddAsync(CredentialFactory.Create(walletId));
        await _repository.AddAsync(CredentialFactory.Create(otherWalletId));
        await _context.SaveChangesAsync();
        
        // Act
        var credentials = await _repository.GetByWalletIdAsync(walletId);
        
        // Assert
        credentials.Should().HaveCount(2);
        credentials.Should().OnlyContain(c => c.WalletId == walletId);
    }
    
    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

## End-to-End Testing

### Critical User Journeys

```csharp
[TestClass]
[Category("E2E")]
public class CredentialIssuanceE2ETests
{
    private IWebDriver _driver;
    private TestServer _server;
    
    [TestInitialize]
    public void Setup()
    {
        _server = new TestServer(/* configuration */);
        _driver = new ChromeDriver();
    }
    
    [TestMethod]
    public async Task CompleteCredentialIssuanceFlow()
    {
        // 1. Admin logs in
        _driver.Navigate().GoToUrl($"{_server.BaseUrl}/admin/login");
        _driver.FindElement(By.Id("username")).SendKeys("admin@test.com");
        _driver.FindElement(By.Id("password")).SendKeys("Test123!");
        _driver.FindElement(By.Id("login-button")).Click();
        
        // 2. Navigate to credential issuance
        _driver.FindElement(By.LinkText("Issue Credential")).Click();
        
        // 3. Fill credential form
        _driver.FindElement(By.Id("wallet-id")).SendKeys("wallet-123");
        _driver.FindElement(By.Id("credential-type"))
            .SelectByText("Driver License");
        // ... fill claims
        
        // 4. Issue credential
        _driver.FindElement(By.Id("issue-button")).Click();
        
        // 5. Verify success
        var successMessage = _driver.FindElement(By.ClassName("success-message"));
        Assert.IsTrue(successMessage.Text.Contains("Credential issued successfully"));
        
        // 6. Verify in wallet app
        var walletApi = new WalletApiClient(_server.BaseUrl);
        var credentials = await walletApi.GetWalletCredentials("wallet-123");
        Assert.AreEqual(1, credentials.Count);
    }
}
```

## Performance Testing

### Load Testing

```csharp
[TestClass]
[Category("Performance")]
public class CredentialApiLoadTests
{
    [TestMethod]
    public async Task IssueCredential_100ConcurrentRequests_MeetsPerformanceTargets()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        var client = new HttpClient { BaseAddress = new Uri("https://test-api.wallet.wa.gov.au") };
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(IssueCredentialAsync(client, $"wallet-{i}"));
        }
        
        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Assert
        responses.Should().OnlyContain(r => r.IsSuccessStatusCode);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10)); // All complete within 10s
        
        var responseTimes = responses.Select(r => 
            double.Parse(r.Headers.GetValues("X-Response-Time").First()));
        responseTimes.Average().Should().BeLessThan(500); // Avg < 500ms
        responseTimes.Percentile(95).Should().BeLessThan(1000); // p95 < 1s
    }
}
```

## Security Testing

### Security Validation Tests

```csharp
[TestClass]
[Category("Security")]
public class SecurityTests
{
    [TestMethod]
    public async Task API_RequiresAuthentication()
    {
        // Arrange
        var client = new HttpClient();
        
        // Act
        var response = await client.GetAsync("/api/v1/credentials");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [TestMethod]
    public async Task TenantIsolation_PreventsCrossAccess()
    {
        // Arrange
        var tenant1Token = GenerateTokenForTenant("tenant-1");
        var tenant2CredentialId = "cred-tenant2-123";
        
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", tenant1Token);
        
        // Act
        var response = await client.GetAsync(
            $"/api/v1/credentials/{tenant2CredentialId}");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [TestMethod]
    public void SensitiveData_IsEncrypted()
    {
        // Arrange
        var credential = CredentialFactory.CreateWithSensitiveData();
        var repository = new CredentialRepository();
        
        // Act
        repository.Save(credential);
        var rawData = GetRawDatabaseValue(credential.Id);
        
        // Assert
        rawData.Should().NotContain("123-45-6789"); // SSN should not be in plain text
        rawData.Should().MatchRegex(@"^[A-Za-z0-9+/=]+$"); // Should be base64 encrypted
    }
}
```

## Test Data Management

### Test Data Builders

```csharp
public class CredentialBuilder
{
    private TenantId _tenantId = TenantId.New();
    private WalletId _walletId = WalletId.New();
    private CredentialStatus _status = CredentialStatus.Active;
    private Dictionary<string, object> _claims = new();
    
    public CredentialBuilder ForTenant(TenantId tenantId)
    {
        _tenantId = tenantId;
        return this;
    }
    
    public CredentialBuilder ForWallet(WalletId walletId)
    {
        _walletId = walletId;
        return this;
    }
    
    public CredentialBuilder WithStatus(CredentialStatus status)
    {
        _status = status;
        return this;
    }
    
    public CredentialBuilder WithClaim(string key, object value)
    {
        _claims[key] = value;
        return this;
    }
    
    public DigitalCredential Build()
    {
        var credential = DigitalCredential.Issue(
            _tenantId, _walletId, 
            CredentialSchemaFactory.Default(), 
            _claims);
            
        if (_status == CredentialStatus.Revoked)
            credential.Revoke(RevocationReason.Test);
            
        return credential;
    }
}

// Usage
var credential = new CredentialBuilder()
    .ForTenant(testTenant)
    .ForWallet(testWallet)
    .WithClaim("firstName", "John")
    .WithClaim("lastName", "Doe")
    .WithStatus(CredentialStatus.Active)
    .Build();
```

### Test Data Reset

```csharp
public class DatabaseFixture : IDisposable
{
    public WalletDbContext Context { get; }
    
    public DatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<WalletDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
            
        Context = new WalletDbContext(options);
        SeedTestData();
    }
    
    private void SeedTestData()
    {
        var tenants = new[]
        {
            new Tenant { Id = "test-tenant-1", Name = "Test Agency 1" },
            new Tenant { Id = "test-tenant-2", Name = "Test Agency 2" }
        };
        
        Context.Tenants.AddRange(tenants);
        Context.SaveChanges();
    }
    
    public void Dispose()
    {
        Context.Dispose();
    }
}
```

## Acceptance Criteria

### Definition of Done

A feature is considered complete when:

1. **Code Complete**
   - [ ] Implementation follows coding standards
   - [ ] Code reviewed and approved
   - [ ] No compiler warnings

2. **Testing Complete**
   - [ ] Unit tests written and passing
   - [ ] Integration tests written and passing
   - [ ] Code coverage meets minimum requirements
   - [ ] No flaky tests

3. **Documentation Complete**
   - [ ] API documentation updated
   - [ ] README updated if needed
   - [ ] Complex logic has comments

4. **Security Complete**
   - [ ] Security tests passing
   - [ ] No security scan violations
   - [ ] Sensitive data encrypted

5. **Performance Complete**
   - [ ] Response time < 500ms (p95)
   - [ ] Load tests passing
   - [ ] No memory leaks

### POA Acceptance Tests

```gherkin
Feature: Credential Issuance
  As a government agency
  I want to issue digital credentials
  So that citizens can prove their identity

  Scenario: Issue driver license credential
    Given I am authenticated as an admin
    And a wallet "wallet-123" exists
    When I issue a driver license credential with valid data
    Then the credential should be created with status "Active"
    And the credential should be stored in the wallet
    And an audit log entry should be created
    And the wallet holder should receive a notification

  Scenario: Prevent duplicate credential issuance
    Given a driver license already exists for wallet "wallet-123"
    When I attempt to issue another driver license
    Then I should receive an error "Duplicate credential"
    And no new credential should be created
```

### Performance Acceptance Criteria

| Metric | Target | Maximum |
|--------|--------|---------|
| API Response Time (p50) | 200ms | 300ms |
| API Response Time (p95) | 400ms | 500ms |
| API Response Time (p99) | 800ms | 1000ms |
| Concurrent Users | 100 | 150 |
| Requests per Second | 500 | 1000 |
| Database Query Time | 50ms | 100ms |
| Error Rate | <0.1% | <1% |
| Availability | 99.9% | 99.5% |