# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This is a tender/proposal documentation repository for a Digital Wallet and Verifiable Credentials Solution for Western Australia (WA). The repository contains technical specifications, requirements, and architectural documentation for DPC2142 - a request for a managed service provider to deliver, host, and support a digital wallet solution integrated with the ServiceWA mobile application.

## Repository Structure

```
/repo/
├── NumbatWallet/                 # Main repository
│   ├── CLAUDE.md                 # This file - AI assistant context
│   ├── README.md                 # Repository overview
│   ├── index.html                # Interactive Azure pricing calculator (GitHub Pages)
│   └── docs/                     # Documentation folder
│       └── Tender/               # Original tender documents (PDFs)
│
└── NumbatWallet.wiki/            # Wiki repository (separate clone)
    ├── Home.md                   # Master PRD (main documentation)
    ├── Solution-Architecture.md  # Technical architecture
    ├── Security-Privacy-Compliance.md
    ├── Technical-Specification.md
    ├── API-Documentation.md
    ├── SDK-Documentation.md
    ├── SDK-Flutter-Guide.md      # Flutter SDK implementation
    ├── SDK-DotNet-Guide.md       # .NET SDK implementation
    ├── SDK-JavaScript-Guide.md   # Web SDK implementation
    ├── Testing-Strategy.md
    ├── Deployment-Guide.md
    ├── Support-Model.md
    ├── Pricing-Assumptions.md
    ├── Detailed-Cost-Breakdown.md
    ├── Azure-Justification-Pricing.md
    ├── Azure-Calculator-Guide.md # Azure pricing calculator inputs
    ├── Team-Resources.md
    ├── Risk-Matrix.md
    ├── Compliance-Matrix.md
    ├── _Sidebar.md               # Wiki navigation
    └── _Footer.md                # Wiki footer
```

## Documentation Access

- **GitHub Wiki**: https://github.com/Credenxia/NumbatWallet/wiki
- **Azure Calculator**: https://credenxia.github.io/NumbatWallet/
- **Wiki Repository**: The wiki is a separate Git repository at the same level as NumbatWallet
- **All documentation is maintained in the `NumbatWallet.wiki` repository**

## Key Documentation Files

- **Home.md**: Master PRD - Comprehensive product requirements document outlining the digital wallet solution
- **Solution-Architecture.md**: Technical architecture including system components, deployment topology, and integration points
- **API-Documentation.md**: OpenAPI 3.0 specifications, endpoint details, authentication flows
- **SDK-Documentation.md**: Overview of SDK offerings with links to detailed guides
- **SDK-Flutter-Guide.md**: Complete Flutter SDK documentation and examples
- **SDK-DotNet-Guide.md**: Enterprise .NET SDK documentation
- **SDK-JavaScript-Guide.md**: TypeScript/JavaScript SDK for web applications
- **Security-Privacy-Compliance.md**: Security controls, privacy requirements, and compliance mappings
- **Technical-Specification.md**: Data models, state machines, and component specifications
- **Azure-Calculator-Guide.md**: Detailed Azure service configurations for cost estimation

## Project Context

### Solution Overview
- **Cloud-native, multi-tenant wallet platform** built on Microsoft .NET and C#
- PostgreSQL for persistence, running in Azure AU regions
- Implements W3C verifiable credential standards with DIDs and OpenID Connect flows
- Flutter SDK for ServiceWA integration, .NET SDK for agencies, TypeScript/JS SDK for verifiers

### Key Standards and Protocols
- ISO/IEC 18013-5/7 (mobile driving licence)
- ISO/IEC 23220 (mobile eID architecture)
- W3C VC Data Model and DIDs
- OID4VCI/OIDC4VP for credential operations
- Trusted Digital Identity Framework (TDIF)

### Multi-tenancy Architecture
- Option A: Per-tenant database (recommended for pilot)
- Option B: Shared database with Row-Level Security (RLS)

## Important Notes

1. This is a **documentation-only repository** - no source code is present
2. The tender is for a **Proof-of-Operation** followed by a 12-month Pilot Phase
3. All data and operations must remain within **Australian sovereign boundaries**
4. The solution must integrate with existing WA government infrastructure including ServiceWA app and WA Identity Exchange
5. Security and compliance are paramount with requirements for ISO 27001, TDIF, GDPR, and Australian Privacy Act compliance

## POA Development Workflow

### GitHub Integration
- **Project #18**: Central POA tracking board with 60+ issues (including testing)
- **Milestones**: 10 milestones with order prefixes (001-010) aligned with weekly deliverables
- **Issue Mapping**: All POA tasks have corresponding GitHub issues (see `/docs/POA/00-task-breakdown.md`)
- **Project Template**: Follow `/docs/POA/github-project-template.md` for all GitHub management

### Before Starting Development
**ALWAYS check these before beginning any task:**

1. **Check GitHub Issue**
   - Review issue #<number> for latest requirements
   - Check blocking dependencies in issue comments
   - Verify milestone dates align with your timeline
   - Review acceptance criteria checkboxes

2. **Verify Prerequisites**
   - Ensure blocking issues are resolved
   - Confirm environment access
   - Check for duplicate work
   - Review related PRs and discussions

3. **Update Status**
   - Move issue to "In Progress" in Project #18
   - Add comment indicating you've started work
   - Note estimated completion time

### During Development

1. **Follow Standards**
   - Use .NET 9 with C# 13 for POA (migrating to .NET 10 in December 2025)
   - Implement Clean Architecture principles
   - Use HotChocolate for GraphQL, REST adapter only for DTP
   - Follow Azure Entra ID for authentication (not B2C)

2. **Update Progress**
   - Comment on issue with significant milestones
   - Link PRs using #<issue-number>
   - Request reviews when ready
   - Document any blockers immediately

### Upon Completion

1. **Close GitHub Issue**
   - Add completion summary
   - Verify all acceptance criteria met
   - Update project board status

2. **Check Milestone**
   - If all milestone issues closed, close the milestone
   - Update timeline documentation if needed

3. **Document Results**
   - Add learnings to issue comments
   - Update relevant documentation
   - Create follow-up issues if needed

## POA Technical Standards

### Architecture
- **Clean Architecture/Onion Architecture** with Domain-Driven Design
- **CQRS** with MediatR for command/query separation
- **Repository pattern** with Unit of Work
- **Dependency injection** using Microsoft.Extensions.DependencyInjection

### Technology Stack (.NET 9 POA)
- **Runtime**: .NET 9.0 (LTS)
- **Language**: C# 13
- **Web Framework**: ASP.NET Core 9
- **ORM**: Entity Framework Core 9 with PostgreSQL
- **GraphQL**: HotChocolate 13
- **Logging**: Serilog with Application Insights
- **Validation**: FluentValidation 11
- **Testing**: xUnit, Moq, FluentAssertions

### Coding Standards
- **Naming**: PascalCase for classes/methods, camelCase for parameters/variables
- **Async**: Use async/await for all I/O operations
- **Nullability**: Enable nullable reference types
- **Global usings**: Use for common namespaces
- **File-scoped namespaces**: Prefer over block-scoped
- **Primary constructors**: Use for simple DTOs and entities

### Performance Requirements
- **Response Time**: <500ms p95
- **Concurrent Users**: Support 100+ concurrent
- **Availability**: 99.5% minimum
- **Test Coverage**: >85% minimum (90% for new code)

### Security Requirements
- **Authentication**: Azure Entra ID (officers), ServiceWA (citizens)
- **Authorization**: Policy-based with claims
- **Data Protection**: AES-256 encryption at rest
- **TLS**: 1.3 minimum for all communications
- **Secrets**: Azure Key Vault for all secrets

### Git Workflow
- **Commit Messages**: Reference issue with "Fixes #<number>" or "Relates to #<number>"
- **Branch Names**: feature/POA-XXX-description
- **PR Requirements**: Must pass CI, have review, link to issue
- **Merge Strategy**: Squash and merge for feature branches

### GitHub Milestone Creation
When creating new milestones, ALWAYS:
1. Use order prefix format: `XXX-Category-Name` (see `/docs/POA/github-project-template.md`)
2. Set appropriate due date
3. Add clear description with deliverables
4. Example: `001-POA-Week1-Foundation`, `041-Testing-Coverage-v1`

### GitHub Project Date Management
When updating dates in GitHub Projects:
1. **Use GraphQL API** for bulk updates (see `/docs/POA/github-project-update-guide.md`)
2. **Get Project IDs**: 
   - Project: PVT_kwDOBBJaks4BCwXX
   - Start Date: PVTF_lADOBBJaks4BCwXXzg04RmU
   - Target Date: PVTF_lADOBBJaks4BCwXXzg04Sug
3. **Update via mutation**:
   ```bash
   gh api graphql -f query='mutation { updateProjectV2ItemFieldValue(...) }'
   ```
4. **Verify no weekend dates**: All work Mon-Fri only
5. **Check dependencies**: Start/End dates are inclusive

## POA Testing Requirements

### Test-Driven Development (TDD) - MANDATORY
**All POA code MUST be developed using TDD methodology:**

1. **Write Test First (RED)**: Create failing test before any implementation
2. **Minimal Implementation (GREEN)**: Write just enough code to pass
3. **Refactor (REFACTOR)**: Improve code while keeping tests green
4. **Commit Together**: Test and implementation in same commit

### Testing Workflow
```bash
# 1. Create feature branch
git checkout -b feature/POA-XXX-tdd

# 2. Write failing test
dotnet new xunit -n MyFeature.Tests
# Write test that fails

# 3. Verify test fails
dotnet test # Should fail

# 4. Implement feature
# Write minimal code to pass test

# 5. Verify test passes
dotnet test # Should pass

# 6. Refactor if needed
# Improve code quality

# 7. Commit test + code together
git add tests/ src/
git commit -m "POA-XXX: Implement feature with TDD"
```

### Test Coverage Requirements
- **Domain Layer**: 95% minimum
- **Application Layer**: 85% minimum  
- **Infrastructure Layer**: 80% minimum
- **API Layer**: 80% minimum
- **SDKs**: 85% minimum
- **Overall**: 85% minimum (blocks PR if below)

### Testing Standards Location
- **Development Standards**: `/docs/POA/development-standards.md`
- **Testing Standards**: `/docs/POA/testing-standards.md`
- **Test Plan**: `/docs/POA/test-plan.md`

### CI/CD Test Enforcement
- Tests must pass before merge
- Coverage must meet minimums
- No ignored or skipped tests
- Performance tests for critical paths
- Security tests for sensitive operations