# POA Test Plan

**Version:** 1.0  
**Date:** September 10, 2025  
**Duration:** October 1 - November 4, 2025

## Executive Summary

This test plan defines the comprehensive testing strategy for the NumbatWallet POA phase. All development follows Test-Driven Development (TDD) methodology with mandatory test-first implementation.

## Test Strategy Overview

### Testing Principles
1. **TDD Mandatory**: No code without tests written first
2. **Automated First**: Manual testing only for UI/UX validation
3. **Continuous Testing**: Tests run on every commit
4. **Shift-Left**: Testing starts before coding
5. **Full Coverage**: Minimum 85% overall, 95% domain

### Test Pyramid Distribution
```
E2E Tests        5%    (19 tests)
Integration     25%    (95 tests)  
Unit Tests      70%    (266 tests)
Total Tests:    380 automated tests
```

## Test Schedule

### Week 1: Foundation Testing (Oct 1-4)
**Focus: Infrastructure and Unit Tests**

| Day | Testing Activities | Coverage Target |
|-----|-------------------|-----------------|
| Oct 1 | Set up test projects, CI/CD pipeline | Infrastructure ready |
| Oct 2 | Domain entity tests, test builders | 60% domain coverage |
| Oct 3 | Infrastructure tests, Flutter SDK tests | 80% domain coverage |
| Oct 4 | Integration test harness, fixtures | 85% domain coverage |

**Deliverables:**
- Test infrastructure operational
- 85% domain layer coverage
- CI/CD pipeline with test gates
- Test data builders ready

### Week 2: Integration Testing (Oct 7-11)
**Focus: API and Integration Tests**

| Day | Testing Activities | Coverage Target |
|-----|-------------------|-----------------|
| Oct 7 | Authentication integration tests | 70% overall |
| Oct 8 | Credential operation tests | 75% overall |
| Oct 9 | Multi-tenant isolation tests | 80% overall |
| Oct 10 | API endpoint tests, security tests | 85% overall |
| Oct 11 | Cross-SDK integration tests | 85% overall |

**Deliverables:**
- All integration tests passing
- 85% overall coverage achieved
- Security test suite complete
- Performance baseline established

### Week 3: E2E and Performance (Oct 14-18)
**Focus: User Journeys and Load Testing**

| Day | Testing Activities | Coverage Target |
|-----|-------------------|-----------------|
| Oct 14 | E2E test implementation | 87% overall |
| Oct 15 | Load testing setup (100 users) | 88% overall |
| Oct 16 | Security penetration testing | 89% overall |
| Oct 17 | Demo dry-run with full test suite | 90% overall |
| Oct 18 | Live demo (all tests passing) | 90% overall |

**Deliverables:**
- E2E tests for all user journeys
- Load tests passing (<500ms p95)
- Security scan clean
- 90% overall coverage

### Week 4-5: Regression and Support (Oct 21 - Nov 1)
**Focus: Test Maintenance and Support**

| Activity | Frequency | Owner |
|----------|-----------|-------|
| Regression test runs | Daily | QA |
| Bug fix test coverage | Per fix | Dev |
| Performance monitoring | Continuous | DevOps |
| Test report generation | Daily | QA |

## Test Types and Scope

### Unit Tests (266 tests)

#### Domain Layer Tests (95% coverage required)
```csharp
// Location: tests/Unit/NumbatWallet.Domain.Tests/
- Entities (45 tests)
  - DigitalCredential (15 tests)
  - Wallet (10 tests)
  - Tenant (8 tests)
  - User (12 tests)
  
- Value Objects (30 tests)
  - TenantId, WalletId, CredentialId
  - ClaimSet, CredentialSchema
  - RevocationReason, CredentialStatus
  
- Domain Services (25 tests)
  - CredentialValidator
  - SignatureService
  - RevocationService
```

#### Application Layer Tests (85% coverage required)
```csharp
// Location: tests/Unit/NumbatWallet.Application.Tests/
- Command Handlers (40 tests)
  - IssueCredentialHandler
  - RevokeCredentialHandler
  - CreateWalletHandler
  
- Query Handlers (35 tests)
  - GetCredentialHandler
  - ListCredentialsHandler
  - VerifyCredentialHandler
  
- Validators (20 tests)
  - IssueCredentialValidator
  - ClaimSetValidator
```

#### Infrastructure Tests (80% coverage required)
```csharp
// Location: tests/Unit/NumbatWallet.Infrastructure.Tests/
- Repositories (30 tests)
- External Services (25 tests)
- Persistence (20 tests)
```

#### SDK Tests (85% coverage required)
```dart
// Location: sdks/flutter/test/
- Models (15 tests)
- API Client (20 tests)
- Storage (10 tests)
- Authentication (15 tests)
```

### Integration Tests (95 tests)

#### API Integration Tests
```csharp
// Location: tests/Integration/NumbatWallet.API.Tests/
[Fact]
public async Task IssueCredential_ValidRequest_Returns201()
{
    // Arrange
    var request = new IssueCredentialRequest { /* ... */ };
    
    // Act
    var response = await _client.PostAsJsonAsync("/api/credentials", request);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

#### Database Integration Tests
```csharp
// Location: tests/Integration/NumbatWallet.Database.Tests/
- Repository operations (20 tests)
- Transaction handling (10 tests)
- Multi-tenant isolation (15 tests)
```

#### External Service Tests
```csharp
// Location: tests/Integration/NumbatWallet.External.Tests/
- WA IdX integration (10 tests)
- PKI service integration (10 tests)
- Azure Key Vault integration (5 tests)
```

### End-to-End Tests (19 tests)

#### Critical User Journeys
```csharp
// Location: tests/E2E/NumbatWallet.E2E.Tests/
1. Complete credential issuance flow
2. Credential verification (online)
3. Credential verification (offline)
4. Credential revocation flow
5. Multi-device synchronization
6. QR code sharing and scanning
7. Selective disclosure flow
8. Biometric authentication flow
9. Admin portal CRUD operations
10. Wallet recovery flow
```

### Performance Tests

#### Load Test Scenarios
```csharp
// Location: tests/Performance/NumbatWallet.LoadTests/
[Test]
public void Scenario_100ConcurrentUsers()
{
    var scenario = Scenario.Create("concurrent_users", async context =>
    {
        // Test implementation
    })
    .WithLoadSimulations(
        Simulation.InjectPerSec(100, TimeSpan.FromSeconds(30))
    );
}
```

#### Performance Targets
| Scenario | Users | Duration | Success Rate | p95 Response |
|----------|-------|----------|--------------|--------------|
| Credential Issue | 100 | 30s | >99% | <500ms |
| Credential Verify | 200 | 30s | >99% | <300ms |
| API Health Check | 500 | 30s | 100% | <100ms |
| Database Query | 100 | 30s | >99% | <100ms |

### Security Tests

#### Security Test Categories
```csharp
// Location: tests/Security/NumbatWallet.Security.Tests/
1. Authentication Tests
   - Token validation
   - Session management
   - Brute force protection
   
2. Authorization Tests
   - Role-based access
   - Tenant isolation
   - Resource permissions
   
3. Input Validation Tests
   - SQL injection
   - XSS attempts
   - Command injection
   
4. Encryption Tests
   - Data at rest
   - Data in transit
   - Key management
```

## Test Data Management

### Test Data Strategy
```csharp
public class TestDataFactory
{
    public static class Tenants
    {
        public static Tenant WaGov => new() { Id = "wa-gov", Name = "WA Government" };
        public static Tenant TestAgency => new() { Id = "test-agency", Name = "Test Agency" };
    }
    
    public static class Wallets
    {
        public static Wallet Active => new WalletBuilder().AsActive().Build();
        public static Wallet Suspended => new WalletBuilder().AsSuspended().Build();
    }
    
    public static class Credentials
    {
        public static DigitalCredential DriverLicense => 
            new CredentialBuilder()
                .WithType(CredentialType.DriverLicense)
                .WithClaim("licenseNumber", "WA123456")
                .Build();
    }
}
```

### Test Database Management
```yaml
# docker-compose.test.yml
services:
  postgres-test:
    image: postgres:15
    environment:
      POSTGRES_DB: wallet_test
      POSTGRES_USER: test
      POSTGRES_PASSWORD: test
    ports:
      - "5433:5432"
```

## Test Automation

### CI/CD Pipeline
```yaml
name: POA Test Pipeline
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - name: Run Unit Tests
        run: dotnet test --filter Category=Unit
        
      - name: Run Integration Tests
        run: dotnet test --filter Category=Integration
        
      - name: Check Coverage
        run: |
          if [ $coverage -lt 85 ]; then
            exit 1
          fi
          
      - name: Run Security Scan
        run: dotnet tool run security-scan
```

### Test Execution Matrix
| Test Type | Trigger | Frequency | Duration | Blocking |
|-----------|---------|-----------|----------|----------|
| Unit | Every commit | Always | <1 min | Yes |
| Integration | PR/merge | Always | <5 min | Yes |
| E2E | Daily/release | Daily | <15 min | Yes |
| Performance | Weekly/release | Weekly | <30 min | No |
| Security | Daily | Daily | <20 min | Yes |

## Test Reporting

### Daily Test Report
```
Date: October X, 2025
======================
Total Tests Run: 380
Passed: 375 (98.7%)
Failed: 5 (1.3%)
Skipped: 0

Coverage Report:
- Overall: 89.2%
- Domain: 96.5%
- Application: 87.3%
- Infrastructure: 82.1%
- API: 85.7%

Failed Tests:
1. [FLAKY] IntegrationTest.Timeout
2. [BUG] CredentialTest.EdgeCase
3-5. [ENVIRONMENT] DatabaseTests

Action Items:
- Fix flaky test (assigned: John)
- Investigate bug (assigned: Sarah)
- Check test environment (assigned: DevOps)
```

### Test Metrics Dashboard
```
┌─────────────────────────────────┐
│ POA Test Metrics                │
├─────────────────────────────────┤
│ Total Tests:        380         │
│ Coverage:           89.2%       │
│ Pass Rate:          98.7%       │
│ Avg Duration:       2.3s        │
│ Flaky Tests:        2           │
│ TDD Compliance:     100%        │
└─────────────────────────────────┘
```

## Risk Mitigation

### Testing Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Flaky tests | High | Medium | Immediate fix or removal |
| Low coverage | High | Low | Block PR merge if <85% |
| Slow tests | Medium | Medium | Parallel execution |
| Test data corruption | High | Low | Isolated test databases |
| Missing edge cases | High | Medium | Code review focus |

## Test Environment

### Environment Configuration
| Environment | Purpose | Database | Reset Frequency |
|-------------|---------|----------|-----------------|
| Local | Development | SQLite | Per test run |
| CI | Automated tests | PostgreSQL | Per pipeline |
| Integration | Integration tests | PostgreSQL | Daily |
| Performance | Load testing | PostgreSQL | Per test |
| Security | Security testing | PostgreSQL | Per scan |

## Success Criteria

### POA Test Success Metrics
- [ ] 380+ automated tests implemented
- [ ] 85% minimum coverage achieved
- [ ] 100% TDD compliance
- [ ] All E2E tests passing
- [ ] Performance targets met
- [ ] Zero critical security issues
- [ ] <2% test flakiness
- [ ] CI/CD pipeline <10 min

## Appendix A: TDD Workflow

### Standard TDD Cycle for POA
```
1. Create feature branch
2. Write failing test (RED)
3. Run test - verify failure
4. Write minimal code (GREEN)
5. Run test - verify pass
6. Refactor code (REFACTOR)
7. Run all tests - verify pass
8. Commit test + code together
9. Push and create PR
10. Verify CI passes
```

## Appendix B: Test Naming Conventions

### Test Naming Pattern
```csharp
[MethodUnderTest]_[Scenario]_[ExpectedResult]

Examples:
- IssueCredential_WithValidClaims_ReturnsSuccess
- RevokeCredential_WhenAlreadyRevoked_ThrowsException
- GetWalletCredentials_ForInvalidWallet_ReturnsEmpty
```

## Appendix C: Test Categories

### Category Usage
```csharp
[Category("Unit")]          // Fast, isolated
[Category("Integration")]   // External dependencies
[Category("E2E")]           // Full stack
[Category("Performance")]   // Load/stress
[Category("Security")]      // Security validation
[Category("Smoke")]         // Critical path
[Category("TDD")]           // Written before code
```

---

**Remember: Every line of code must have a test written first. No exceptions.**