# POA Task Breakdown - GitHub Issues & Milestones

**Generated:** September 10, 2025  
**POA Start:** October 1, 2025  
**POA End:** November 4, 2025

## ⚠️ IMPORTANT: GitHub Integration Instructions

### Before Starting Any Task:
1. **Check GitHub Issue** - Review the issue for latest requirements and dependencies
2. **Verify Blocks** - Check if your task is blocked by other issues
3. **Update Status** - Move issue to "In Progress" in Project #18
4. **Review Comments** - Check for any additional context or changes

### When Completing a Task:
1. **Close GitHub Issue** - Mark issue as closed with completion comment
2. **Update Project** - Ensure Project #18 reflects completion
3. **Check Milestone** - If all milestone issues closed, close the milestone
4. **Document Results** - Add any relevant documentation or learnings

## GitHub Projects Structure

### Projects to Create
1. **NumbatWallet-POA** - Master project tracking all POA tasks
2. **NumbatWallet-Backend** - Backend API development
3. **NumbatWallet-SDKs** - Flutter, .NET, TypeScript SDKs
4. **NumbatWallet-Infrastructure** - Azure infrastructure and DevOps

### Milestones

| ID | Milestone Name | Target Date | Project | Description |
|----|---------------|-------------|---------|-------------|
| M01 | POA-Week1-Foundation | Oct 4, 2025 | POA | Infrastructure setup and SDK delivery |
| M02 | POA-Week2-Integration | Oct 11, 2025 | POA | Authentication and credential operations |
| M03 | POA-Week3-Demo | Oct 18, 2025 | POA | Feature completion and demonstration |
| M04 | POA-Week4-Testing | Oct 25, 2025 | POA | DGov testing support |
| M05 | POA-Week5-Evaluation | Nov 1, 2025 | POA | Final evaluation and handover |
| M06 | Backend-Core-API-v1 | Oct 11, 2025 | Backend | Core API implementation |
| M07 | SDK-Flutter-v1 | Oct 4, 2025 | SDKs | Flutter SDK for ServiceWA |
| M08 | SDK-DotNet-v1 | Oct 11, 2025 | SDKs | .NET SDK for agencies |
| M09 | SDK-TypeScript-v1 | Oct 18, 2025 | SDKs | TypeScript SDK for web |
| M10 | Infra-Azure-Setup | Oct 4, 2025 | Infrastructure | Azure environment ready |

## Task List by Week

### WEEK 1: Foundation (October 1-4, 2025)

| Task ID | GitHub | Title | Assignee | Priority | Size | Labels | Milestone | Dependencies |
|---------|--------|-------|----------|----------|------|--------|-----------|--------------|
| POA-001 | #1 | Set up Azure subscription and resource groups | DevOps | P0 | M | `infrastructure`, `azure` | M10 | - |
| POA-002 | #2 | Configure Azure PostgreSQL Flexible Server | DevOps | P0 | M | `infrastructure`, `database` | M10 | POA-001 |
| POA-003 | #3 | Set up Azure Container Registry | DevOps | P0 | S | `infrastructure`, `containers` | M10 | POA-001 |
| POA-004 | #26 | Configure Virtual Network and subnets | DevOps | P0 | M | `infrastructure`, `networking` | M10 | POA-001 |
| POA-005 | #4 | Set up Key Vault for secrets management | DevOps | P0 | S | `infrastructure`, `security` | M10 | POA-001 |
| POA-006 | #27 | Configure Application Gateway with WAF | DevOps | P1 | M | `infrastructure`, `security` | M10 | POA-004 |
| POA-007 | #28 | Set up App Service Plan (Linux) | DevOps | P0 | S | `infrastructure`, `hosting` | M10 | POA-001 |
| POA-008 | #29 | Configure Log Analytics workspace | DevOps | P1 | S | `infrastructure`, `monitoring` | M10 | POA-001 |
| POA-009 | #30 | Create CI/CD pipelines in GitHub Actions | DevOps | P0 | L | `devops`, `ci-cd` | M10 | POA-003 |
| POA-009a | #5 | Create Bicep main orchestrator template | DevOps | P0 | M | `infrastructure`, `iac`, `bicep` | M10 | POA-001 |
| POA-009b | #6 | Create Bicep networking module | DevOps | P0 | S | `infrastructure`, `iac`, `bicep` | M10 | POA-009a |
| POA-009c | #7 | Create Bicep database module | DevOps | P0 | S | `infrastructure`, `iac`, `bicep` | M10 | POA-009a |
| POA-009d | #8 | Create Bicep container apps module | DevOps | P0 | S | `infrastructure`, `iac`, `bicep` | M10 | POA-009a |
| POA-009e | #9 | Create Bicep deployment script | DevOps | P0 | M | `infrastructure`, `iac`, `deployment` | M10 | POA-009a-d |
| POA-010 | #31 | Database schema design and ERD documentation | Backend | P0 | L | `backend`, `database` | M06 | - |
| POA-011 | #32 | Create initial database migrations | Backend | P0 | M | `backend`, `database` | M06 | POA-010 |
| POA-012 | #10 | Backend project structure setup (.NET 9) | Backend | P0 | M | `backend`, `architecture` | M06 | - |
| POA-013 | #11 | Implement health check endpoints | Backend | P0 | S | `backend`, `api` | M06 | POA-012 |
| POA-014 | #33 | Configure Swagger/OpenAPI documentation | Backend | P0 | S | `backend`, `documentation` | M06 | POA-012 |
| POA-015 | #12 | Flutter SDK project initialization | Mobile | P0 | M | `sdk`, `flutter` | M07 | - |
| POA-016 | #34 | Flutter SDK core models implementation | Mobile | P0 | M | `sdk`, `flutter` | M07 | POA-015 |
| POA-017 | #35 | Flutter SDK HTTP client setup | Mobile | P0 | S | `sdk`, `flutter` | M07 | POA-015 |
| POA-018 | #36 | Create Docker containers for backend | DevOps | P0 | M | `devops`, `containers` | M10 | POA-012 |
| POA-019 | #37 | Deploy backend to Azure Container Apps | DevOps | P0 | M | `deployment`, `azure` | M10 | POA-018, POA-007 |
| POA-020 | #38 | Week 1 checkpoint documentation | PM | P0 | S | `documentation`, `milestone` | M01 | All Week 1 |
| POA-081 | #42 | Set up test framework and CI pipeline | DevOps | P0 | M | `testing`, `infrastructure` | M10 | POA-001 |
| POA-082 | #43 | Create unit test structure for Domain layer | Backend | P0 | M | `testing`, `tdd` | M06 | POA-012 |
| POA-083 | #44 | Create integration test harness | Backend | P0 | M | `testing`, `integration` | M06 | POA-012 |
| POA-084 | #45 | Set up test database and fixtures | DevOps | P0 | S | `testing`, `database` | M10 | POA-002 |
| POA-085 | #46 | Implement test data builders | Backend | P0 | S | `testing`, `utilities` | M06 | POA-082 |
| POA-086 | #47 | Flutter SDK unit test suite | Mobile | P0 | M | `testing`, `flutter`, `tdd` | M07 | POA-015 |
| POA-087 | #48 | Infrastructure layer unit tests | Backend | P0 | M | `testing`, `infrastructure` | M06 | POA-012 |

**Subtasks for POA-012 (Backend project structure - .NET 9):**
- POA-012.1: Create solution file and project structure (.NET 9)
- POA-012.2: Set up Clean Architecture layers (Domain, Application, Infrastructure, API)
- POA-012.3: Configure dependency injection container with C# 13 features
- POA-012.4: Add Entity Framework Core 9 and configure DbContext
- POA-012.5: Set up AutoMapper profiles
- POA-012.6: Configure Serilog for structured logging
- POA-012.7: Configure HotChocolate GraphQL server
- POA-012.8: Set up REST adapter for DTP compatibility

### WEEK 2: Integration (October 7-11, 2025)

| Task ID | GitHub | Title | Assignee | Priority | Size | Labels | Milestone | Dependencies |
|---------|--------|-------|----------|----------|------|--------|-----------|--------------|
| POA-021 | #13 | Implement OIDC authentication middleware | Backend | P0 | L | `backend`, `security` | M06 | POA-013 |
| POA-022 | #14 | Create mock WA IdX integration | Backend | P0 | M | `backend`, `integration` | M06 | POA-021 |
| POA-023 | #15 | Implement tenant resolution middleware | Backend | P0 | M | `backend`, `multi-tenant` | M06 | POA-021 |
| POA-024 | - | Create Credential domain model | Backend | P0 | M | `backend`, `domain` | M06 | POA-011 |
| POA-025 | #16 | Implement CQRS for credential operations | Backend | P0 | L | `backend`, `cqrs` | M06 | POA-024 |
| POA-026 | #17 | Create credential issuance API endpoints | Backend | P0 | M | `backend`, `api` | M06 | POA-025 |
| POA-027 | - | Implement credential verification endpoints | Backend | P0 | M | `backend`, `api` | M06 | POA-025 |
| POA-028 | - | Set up PKI test certificates | Security | P0 | M | `security`, `pki` | M02 | POA-005 |
| POA-029 | - | Implement certificate validation logic | Security | P0 | M | `security`, `pki` | M02 | POA-028 |
| POA-030 | - | Create mDL schema implementation (ISO 18013-5) | Backend | P0 | M | `backend`, `standards` | M06 | POA-024 |
| POA-031 | #18 | Flutter SDK authentication module | Mobile | P0 | M | `sdk`, `flutter` | M07 | POA-021 |
| POA-032 | - | Flutter SDK credential storage implementation | Mobile | P0 | M | `sdk`, `flutter` | M07 | POA-016 |
| POA-033 | #39 | .NET SDK project setup | Backend | P1 | M | `sdk`, `dotnet` | M08 | - |
| POA-034 | #40 | .NET SDK client implementation | Backend | P1 | M | `sdk`, `dotnet` | M08 | POA-033 |
| POA-035 | #41 | TypeScript SDK project setup | Frontend | P2 | M | `sdk`, `typescript` | M09 | - |
| POA-036 | - | Integration test suite setup | QA | P0 | M | `testing`, `integration` | M02 | POA-026 |
| POA-037 | - | Performance baseline tests | QA | P1 | M | `testing`, `performance` | M02 | POA-036 |
| POA-038 | - | Security scan implementation | Security | P0 | S | `security`, `testing` | M02 | POA-026 |
| POA-039 | - | API documentation generation | Backend | P1 | S | `documentation`, `api` | M02 | POA-026 |
| POA-040 | - | Week 2 checkpoint and demo preparation | PM | P0 | S | `documentation`, `milestone` | M02 | All Week 2 |
| POA-088 | #49 | Unit tests for credential operations | Backend | P0 | M | `testing`, `tdd`, `domain` | M06 | POA-025 |
| POA-089 | #50 | Integration tests for authentication | Backend | P0 | M | `testing`, `security` | M06 | POA-021 |
| POA-090 | #51 | API endpoint integration tests | Backend | P0 | L | `testing`, `api` | M06 | POA-026 |
| POA-091 | #52 | Multi-tenant isolation tests | Backend | P0 | M | `testing`, `security` | M06 | POA-023 |
| POA-092 | #53 | Security validation test suite | Security | P0 | M | `testing`, `security` | M02 | POA-029 |
| POA-093 | #54 | Cross-SDK integration tests | Team | P1 | M | `testing`, `integration` | M02 | POA-034 |
| POA-094 | #55 | Performance baseline tests | QA | P1 | M | `testing`, `performance` | M02 | POA-090 |

**Subtasks for POA-025 (CQRS implementation):**
- POA-025.1: Install and configure MediatR
- POA-025.2: Create command handlers for credential operations
- POA-025.3: Create query handlers for credential retrieval
- POA-025.4: Implement domain events
- POA-025.5: Set up MediatR pipeline behaviors
- POA-025.6: Add validation behaviors
- POA-025.7: Implement audit logging behavior

### WEEK 3: Demo Preparation (October 14-18, 2025)

| Task ID | GitHub | Title | Assignee | Priority | Size | Labels | Milestone | Dependencies |
|---------|--------|-------|----------|----------|------|--------|-----------|--------------|
| POA-041 | #24 | Create demo mobile application | Mobile | P0 | L | `demo`, `flutter` | M03 | POA-032 |
| POA-042 | - | QR code generation for credentials | Backend | P0 | M | `backend`, `feature` | M03 | POA-026 |
| POA-043 | - | QR code scanning in Flutter SDK | Mobile | P0 | M | `sdk`, `flutter` | M07 | POA-032 |
| POA-044 | - | Implement selective disclosure | Backend | P0 | L | `backend`, `privacy` | M03 | POA-030 |
| POA-045 | - | Create demo wallet app UI | Mobile | P0 | L | `demo`, `flutter` | M03 | POA-032 |
| POA-046 | - | Implement biometric authentication | Mobile | P1 | M | `demo`, `security` | M03 | POA-045 |
| POA-047 | - | Create admin portal (basic) | Frontend | P0 | L | `demo`, `web` | M03 | POA-026 |
| POA-048 | #19 | Prepare demo presentation | PM | P0 | M | `demo`, `documentation` | M03 | POA-045 |
| POA-049 | - | Multi-device support implementation | Backend | P1 | M | `backend`, `feature` | M03 | POA-026 |
| POA-050 | - | Load testing (100 concurrent users) | QA | P0 | M | `testing`, `performance` | M03 | POA-037 |
| POA-051 | - | Security penetration testing | Security | P0 | L | `security`, `testing` | M03 | POA-038 |
| POA-052 | - | Create demo scenarios and scripts | PM | P0 | M | `demo`, `documentation` | M03 | POA-045 |
| POA-053 | - | Prepare presentation materials | PM | P0 | M | `demo`, `documentation` | M03 | POA-052 |
| POA-054 | - | Conduct internal demo dry run | Team | P0 | M | `demo`, `testing` | M03 | POA-053 |
| POA-055 | - | Fix critical issues from dry run | Team | P0 | M | `bugfix`, `critical` | M03 | POA-054 |
| POA-056 | - | Final demo environment setup | DevOps | P0 | S | `demo`, `infrastructure` | M03 | POA-055 |
| POA-057 | - | Create backup demo environment | DevOps | P1 | S | `demo`, `backup` | M03 | POA-056 |
| POA-058 | - | **Live demonstration to DGov** | Team | P0 | L | `demo`, `milestone` | M03 | POA-057 |
| POA-059 | - | Gather feedback from demonstration | PM | P0 | S | `feedback`, `documentation` | M03 | POA-058 |
| POA-060 | - | Week 3 documentation update | PM | P0 | S | `documentation`, `milestone` | M03 | POA-059 |
| POA-095 | #56 | E2E tests for critical user journeys | QA | P0 | L | `testing`, `e2e` | M03 | POA-045 |
| POA-096 | #57 | Load testing suite (100 concurrent users) | QA | P0 | M | `testing`, `performance` | M03 | POA-094 |
| POA-097 | #58 | Security penetration testing | Security | P0 | L | `testing`, `security` | M03 | POA-092 |
| POA-098 | #59 | Compliance validation tests | QA | P1 | M | `testing`, `compliance` | M03 | POA-095 |
| POA-099 | #60 | Test coverage reporting and analysis | QA | P1 | S | `testing`, `reporting` | M03 | All tests |

**Subtasks for POA-045 (Demo wallet app UI):**
- POA-045.1: Design UI/UX mockups
- POA-045.2: Implement onboarding flow
- POA-045.3: Create credential list view
- POA-045.4: Build credential detail view
- POA-045.5: Implement share/present flow
- POA-045.6: Add settings and profile screens
- POA-045.7: Implement dark mode support

### WEEK 4: Testing Support (October 21-25, 2025)

| Task ID | GitHub | Title | Assignee | Priority | Size | Labels | Milestone | Dependencies |
|---------|--------|-------|----------|----------|------|--------|-----------|--------------|
| POA-061 | #25 | Support DGov UAT testing | DevOps | P0 | M | `support`, `testing` | M04 | - |
| POA-062 | - | Provide test data generation scripts | Backend | P0 | S | `support`, `testing` | M04 | - |
| POA-063 | - | Daily standup with DGov test team | PM | P0 | S | `support`, `communication` | M04 | Daily |
| POA-064 | - | Issue triage and prioritization | PM | P0 | M | `support`, `management` | M04 | Ongoing |
| POA-065 | - | Critical bug fixes (P0 issues) | Team | P0 | L | `bugfix`, `critical` | M04 | As found |
| POA-066 | #22 | Execute performance testing | QA | P0 | M | `testing`, `performance` | M04 | POA-050 |
| POA-067 | - | Documentation updates from testing feedback | Team | P1 | M | `documentation`, `support` | M04 | Ongoing |
| POA-068 | - | Additional test scenario support | QA | P1 | M | `testing`, `support` | M04 | As requested |
| POA-069 | - | Security finding remediation | Security | P0 | M | `security`, `bugfix` | M04 | As found |
| POA-070 | - | Integration issue resolution | Backend | P0 | M | `bugfix`, `integration` | M04 | As found |

### WEEK 5: Evaluation & Handover (October 28 - November 1, 2025)

| Task ID | GitHub | Title | Assignee | Priority | Size | Labels | Milestone | Dependencies |
|---------|--------|-------|----------|----------|------|--------|-----------|--------------|
| POA-071 | - | Final bug fixes from Week 4 testing | Team | P0 | M | `bugfix`, `final` | M05 | POA-065 |
| POA-072 | - | Performance optimization | Backend | P1 | M | `optimization`, `performance` | M05 | POA-066 |
| POA-073 | - | Final security scan and report | Security | P0 | S | `security`, `final` | M05 | POA-069 |
| POA-074 | - | Update all documentation | Team | P0 | L | `documentation`, `final` | M05 | All tasks |
| POA-075 | #21 | Create handover package | PM | P0 | M | `documentation`, `handover` | M05 | POA-074 |
| POA-076 | - | Knowledge transfer sessions | Team | P0 | L | `knowledge-transfer`, `training` | M05 | POA-075 |
| POA-077 | #23 | Final evaluation presentation | PM | P0 | M | `presentation`, `milestone` | M05 | All tasks |
| POA-078 | - | Gather evaluation results | PM | P0 | S | `feedback`, `evaluation` | M05 | POA-077 |
| POA-079 | - | Create pilot phase plan | PM | P0 | L | `planning`, `next-phase` | M05 | POA-078 |
| POA-080 | - | Archive POA artifacts and close project | DevOps | P1 | S | `closure`, `archive` | M05 | All tasks |

## Task Labels

### Priority Labels
- `P0` - Critical, blocks other work
- `P1` - High priority, needed for milestone
- `P2` - Medium priority, nice to have
- `P3` - Low priority, can defer

### Size Labels
- `XS` - < 2 hours
- `S` - 2-4 hours
- `M` - 1-2 days
- `L` - 3-5 days
- `XL` - > 5 days

### Category Labels
- `infrastructure` - Azure and cloud setup
- `backend` - API and server development
- `sdk` - SDK development
- `demo` - Demo applications
- `testing` - All testing activities
- `security` - Security related
- `documentation` - Documentation tasks
- `bugfix` - Bug fixes
- `support` - Support activities
- `milestone` - Milestone deliverables

### Status Labels
- `blocked` - Waiting on dependencies
- `in-progress` - Currently being worked
- `ready-for-review` - Awaiting review
- `ready-to-deploy` - Approved and ready
- `completed` - Done

## GitHub Project Automation Rules

### Column Configuration
1. **Backlog** - All new issues
2. **Ready** - Dependencies met, ready to start
3. **In Progress** - Actively being worked
4. **In Review** - PR created, awaiting review
5. **Testing** - In QA/testing
6. **Done** - Completed and closed

### Automation Rules
- When issue created → Add to Backlog
- When assignee added → Move to Ready
- When PR linked → Move to In Review
- When PR merged → Move to Testing
- When issue closed → Move to Done
- When `blocked` label added → Move to Backlog
- When `blocked` label removed → Move to Ready

## Issue Templates

### Feature Template
```markdown
## Description
Brief description of the feature

## Acceptance Criteria
- [ ] Criteria 1
- [ ] Criteria 2
- [ ] Criteria 3

## Technical Notes
Any technical considerations

## Dependencies
- Depends on: #XX
- Blocks: #YY

## Testing Requirements
- Unit tests required
- Integration tests required
- Performance impact assessment
```

### Bug Template
```markdown
## Description
What's broken?

## Steps to Reproduce
1. Step 1
2. Step 2
3. Step 3

## Expected Behavior
What should happen

## Actual Behavior
What actually happens

## Environment
- OS: 
- Browser/App version:
- API version:

## Priority
P0/P1/P2/P3

## Screenshots
If applicable
```

### Task Template
```markdown
## Description
What needs to be done

## Definition of Done
- [ ] Implementation complete
- [ ] Tests written and passing
- [ ] Documentation updated
- [ ] Code reviewed
- [ ] Deployed to test environment

## Time Estimate
XS/S/M/L/XL

## Dependencies
- Depends on: #XX
- Blocks: #YY
```

## Success Metrics

### Week 1 Metrics
- 20 tasks completed
- Infrastructure operational
- SDKs delivered
- 0 blockers

### Week 2 Metrics
- 20 tasks completed
- APIs functional
- Authentication working
- <500ms response time

### Week 3 Metrics
- 20 tasks completed
- Demo successful
- All features working
- Positive feedback

### Week 4 Metrics
- 10 tasks + support
- <4 hour issue resolution
- DGov testing unblocked
- Issues decreasing daily

### Week 5 Metrics
- 10 tasks completed
- All documentation delivered
- Knowledge transfer complete
- >95% acceptance criteria met

## Resource Allocation

### Task Distribution by Role
- **DevOps:** 15 tasks
- **Backend:** 25 tasks
- **Mobile:** 10 tasks
- **Frontend:** 5 tasks
- **Security:** 8 tasks
- **QA:** 7 tasks
- **PM:** 10 tasks

### Critical Path Tasks
These tasks block multiple others:
- POA-001: Azure setup
- POA-012: Backend structure
- POA-015: Flutter SDK init
- POA-021: Authentication
- POA-025: CQRS implementation
- POA-045: Demo app UI
- POA-058: Live demonstration

## Notes

1. All task IDs will become GitHub issue numbers
2. Milestones map to GitHub milestones
3. Projects provide cross-repository tracking
4. Daily standups update task status
5. Weekly reports summarize milestone progress
6. All code must pass CI/CD before merge
7. Security and performance are P0 priorities