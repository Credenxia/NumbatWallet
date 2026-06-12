# CLAUDE.md - NumbatWallet Backend

## Quick Start
> **State (2026-06)**: full credential lifecycle incl. W3C VP-JWT presentations + OID4VP; citizen Bearer + Credentry SSO federation; PII encrypted w/ search tokens; deployed to shared nonprod AKS (`numbatwallet-test`, tst.numbatwallet.credentry.com.au); 3 SDKs contract-verified live. Unit suites 794+/0, integration 84/0/2. Details: session memory + docs.

- **Language**: C# 14 / .NET 10
- **Architecture**: Clean Architecture + DDD + CQRS (Custom, NO MediatR)
- **Database**: PostgreSQL 17 with EF Core 10 (real migrations; dev uses EnsureCreated)
- **Testing**: TDD mandatory (85% coverage minimum)
- **GitHub Project**: #18 (NumbatWallet POA Phase)
- **Wiki**: `/repo/NumbatWallet.wiki/` (separate clone)

## Project Structure
```
/repo/
├── NumbatWallet/                      # Backend API (THIS REPO)
│   ├── src/
│   │   ├── NumbatWallet.Domain/       # Pure business logic
│   │   ├── NumbatWallet.Application/  # CQRS handlers
│   │   ├── NumbatWallet.Infrastructure/ # EF Core, services
│   │   ├── NumbatWallet.Web.Api/      # REST/GraphQL API
│   │   ├── NumbatWallet.Web.Admin/    # Blazor admin
│   │   └── Tests/                     # All test projects
│   └── docs/
│       ├── standards/                 # Coding standards
│       └── POA/                       # POA documentation
├── NumbatWallet-sdks/                 # Client SDKs (SEPARATE REPO)
└── NumbatWallet.wiki/                 # Documentation (WIKI REPO)
```

## Architecture Overview

### Clean Architecture Layers
1. **Domain**: Entities, Value Objects, Domain Events, Specifications
   - No dependencies, pure C# classes
   - Business rules and invariants

2. **Application**: Commands, Queries, DTOs, Interfaces
   - CQRS pattern (custom implementation, NO MediatR - see issue #154)
   - Application services and use cases

3. **Infrastructure**: DbContext, Repositories, External Services
   - EF Core with PostgreSQL
   - Azure service integrations

4. **Web.Api**: Controllers, GraphQL, Middleware
   - REST endpoints with versioning
   - HotChocolate for GraphQL

5. **Web.Admin**: Blazor Server Components
   - Admin portal for management
   - Real-time dashboards

## Quality Checklist

### Zero Tolerance Standards
- ✅ **ZERO** compilation errors
- ✅ **ZERO** warnings (CS/CA/NU)
- ✅ **ALL** tests passing (no skipped)
- ✅ **85%+** test coverage
- ✅ **NO** vulnerable packages
- ✅ **NO** debugging artifacts in code

### Build Verification Commands
```bash
dotnet build -warnaserror           # Must have zero warnings
dotnet test                          # All tests must pass
dotnet list package --vulnerable    # No vulnerabilities allowed
```

## Development Guidelines

### TDD Workflow (MANDATORY)
1. **RED**: Write failing test first
2. **GREEN**: Minimal code to pass
3. **REFACTOR**: Improve while keeping green
4. **COMMIT**: Test + code together

### C# Coding Standards
- **Naming Conventions**:
  - Classes/Methods: PascalCase
  - Parameters/Variables: camelCase
  - Interfaces: IPrefix (e.g., IRepository)
  - Async methods: *Async suffix
  - Test methods: MethodName_Scenario_ExpectedResult

- **Modern C# Features**:
  - Nullable reference types: ENABLED
  - File-scoped namespaces: PREFERRED
  - Global usings: USE for common namespaces
  - Primary constructors: USE for DTOs
  - ArgumentNullException.ThrowIfNull: REQUIRED

### Git Workflow
```bash
# Branch naming
feature/POA-XXX-description

# Commit message format
POA-XXX: Brief description

- Detailed changes
- Test coverage: XX%

Fixes #XXX
```

## Architecture Guidelines

### Clean Architecture Rules
1. **Dependency Flow**: Domain ← Application ← Infrastructure ← Web
2. **Domain Layer**: No external dependencies, pure C#
3. **Application Layer**: CQRS handlers (NO MediatR)
4. **Infrastructure Layer**: EF Core, external services only
5. **Web Layer**: Controllers, GraphQL, no business logic

### CQRS Implementation
- **Custom implementation** (issue #154 - NO MediatR)
- **Commands**: ICommandHandler for write operations
- **Queries**: IQueryHandler for read operations
- **Validation**: FluentValidation in handlers

### Multi-tenancy Requirements
- **Complete isolation** at all layers
- **Per-tenant database** (Option A for pilot)
- **Tenant context** in every operation
- **No cross-tenant data access**

## Active Milestones (GitHub Project #18)

### Backend Development Status
| Milestone | Open | Closed | Status | Key Issues |
|-----------|------|--------|--------|------------|
| 001-PreDev-Standards | 0 | 2 | ✅ COMPLETE | JWT-VC, credential manifests done |
| 002-PreDev-PKI | 4 | 2 | 🟡 IN PROGRESS | IACA certs, trust lists |
| 003-PreDev-Infrastructure | 0 | 2 | ✅ COMPLETE | All infrastructure modules created |
| 011-Backend-Foundation | 0 | 12 | ✅ COMPLETE | All done |
| 012-Backend-Domain | 0 | 21 | ✅ COMPLETE | All done |
| 013-Backend-Infrastructure | 0 | 22 | ✅ COMPLETE | Key Vault, PostgreSQL done |
| 014-Backend-Application | 0 | 14 | ✅ COMPLETE | Handlers fully wired |
| 015-Backend-IaC | 1 | 18 | ✅ COMPLETE | Container Apps, ACR, VNet done |
| 016-Backend-API | 2 | 13 | ✅ COMPLETE | CQRS handlers wired |
| 017-Backend-Admin | 5 | 13 | 🟡 IN PROGRESS | Azure deployment remaining |

### Recently Completed (Sep 2025)
- ✅ **POA-121**: JWT-VC implementation with full W3C support
- ✅ **POA-123**: DIF Credential Manifest specification support
- ✅ **POA-191**: Platform-specific wallet templates (Apple/Google/Web)
- ✅ **POA-083**: Integration test harness with TestContainers
- ✅ **POA-200**: Wallet template builder with preset templates
- ✅ **All compilation errors fixed**: 0 errors, 14 warnings remaining

### Remaining Items
- **POA-181**: GraphQL implementation currently returns mock data
- **Azure deployment**: App Service, Application Gateway, Log Analytics

### Issue Workflow
1. Check blocking dependencies in GitHub
2. Move issue to "In Progress" in Project #18
3. Create feature branch
4. TDD cycle: Red → Green → Refactor
5. Close issue with coverage report

## Testing Guidelines

### TDD Workflow (MANDATORY)
```bash
# 1. RED - Write failing test first
dotnet test --filter "FullyQualifiedName~TestName"  # Should fail

# 2. GREEN - Implement minimum code to pass
dotnet test --filter "FullyQualifiedName~TestName"  # Should pass

# 3. REFACTOR - Improve code quality
dotnet test  # All tests should pass

# 4. Check coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Coverage Requirements by Layer
- **Domain**: 95% minimum
- **Application**: 85% minimum
- **Infrastructure**: 80% minimum
- **Web.Api**: 80% minimum
- **Web.Admin**: 80% minimum

### Test Organization
```
Tests/
├── NumbatWallet.Domain.Tests/        # Domain logic tests
├── NumbatWallet.Application.Tests/   # Handler tests
├── NumbatWallet.Infrastructure.Tests/# Repository tests
├── NumbatWallet.Web.Api.Tests/       # API integration tests
└── NumbatWallet.Web.Admin.Tests/     # UI component tests
```

## Security & Compliance

### Authentication & Authorization
- Azure Entra ID for officers
- ServiceWA for citizens
- Policy-based authorization with claims

### Data Protection
- AES-256 encryption at rest
- TLS 1.3 minimum for transport
- Azure Key Vault for secrets
- Per-tenant data isolation

### Compliance
- TDIF (Trusted Digital Identity Framework)
- Australian Privacy Act
- ISO 27001 alignment
- GDPR compliance ready

## Quick Commands

### Development
```bash
# Build with zero tolerance
dotnet build -warnaserror

# Run all tests
dotnet test

# Test with coverage
dotnet test --collect:"XPlat Code Coverage"

# Check for vulnerabilities
dotnet list package --vulnerable --include-transitive
```

### GitHub Management
```bash
# Check milestone progress
gh issue list --milestone "016-Backend-API" --state open

# Update issue status
gh issue edit XXX --add-label "in-progress"

# Close issue with comment
gh issue close XXX --comment "Completed with XX% coverage"

# Check project status
gh project item-list 18 --owner Credenxia --limit 10
```

### Session Management
```bash
# Start new session (use SESSION_START_PROMPT.md)
git status && dotnet test

# End session checklist
dotnet build -warnaserror  # No warnings
dotnet test                # All passing
git status                 # Clean or committed
```

## Technology Stack

### Core
- **.NET 10** - Runtime
- **C# 14** - Language
- **ASP.NET Core 10** - Web framework
- **Entity Framework Core 10** - ORM
- **PostgreSQL 17** - Database

### Libraries
- **HotChocolate 15** - GraphQL (⚠️ types are singletons: scoped services ONLY via [Service] resolver params, never ctor)
- **FluentValidation 11** - Validation
- **Serilog** - Structured logging
- **AutoMapper 13** - Object mapping
- **Polly 8** - Resilience

### Testing
- **xUnit** - Test framework
- **Moq** - Mocking
- **FluentAssertions** - Assertions
- **TestContainers** - Integration testing

### Infrastructure
- **Azure** - Cloud platform (AU regions)
- **Docker** - Containerization
- **Bicep** - Infrastructure as Code
- **GitHub Actions** - CI/CD

## Performance Requirements
- Response time: <500ms p95
- Concurrent users: 100+ minimum
- Availability: 99.5% SLA
- Database connections: Pooled (min: 10, max: 100)

## Important Notes
1. **NO MediatR** - Use custom CQRS implementation (issue #154)
2. **TDD is mandatory** - No code without tests
3. **Zero tolerance** - No warnings, no skipped tests
4. **Multi-tenancy** - Complete isolation at all layers
5. **Australian sovereignty** - Data must remain in AU regions

## Related Documentation
- Wiki: `/repo/NumbatWallet.wiki/Home.md` - Master PRD
- Standards: `/docs/standards/backend/` - Detailed coding standards
- POA: `/docs/POA/` - Proof of Authority documentation
- Session Guide: `SESSION_START_PROMPT.md` - Development workflow

---
*Last Updated: June 2026 | Version: 2.1 | milestone table above is HISTORICAL (Sep 2025) — trust the State note + session memory*