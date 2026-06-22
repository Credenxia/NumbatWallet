---
title: "NumbatWallet Admin Portal - Test Strategy & Methodology"
subtitle: "QA Planning Document"
author: "QA & Development Team"
date: "2026-03-13"
---

# NumbatWallet Admin Portal - Test Strategy & Methodology

## Document Control

| Field | Value |
|---|---|
| **Document Title** | Test Strategy & Methodology |
| **Version** | 1.0 |
| **Status** | Approved |
| **Created** | 2026-03-13 |
| **Author** | QA & Development Team |
| **Project** | NumbatWallet Admin Portal |

---

## 1. Executive Summary

The NumbatWallet Admin Portal is a Blazor Server application providing administrative management of digital wallets, credentials, tenants, and audit logs for the Government of Western Australia. This document defines the test strategy, approach, and methodology for validating the system's functional, security, and business requirements.

The testing strategy adopts a risk-based approach prioritising authentication/authorization enforcement, data protection, and core workflow availability. Automated end-to-end tests provide continuous regression coverage, while manual exploratory testing addresses edge cases and usability concerns.

**Key Metrics (Current Execution):**

| Metric | Value |
|---|---|
| Total Automated Test Cases | 56 |
| Pass Rate | 100% |
| Execution Time | ~90 seconds |
| Test Categories | 11 (Auth, Nav, Dashboard, Wallets, Credentials, Tenants, Audit, API, Placeholder, CRUD Integration, CRUD UI) |
| Critical Priority Tests | 19 |
| High Priority Tests | 20 |
| Medium Priority Tests | 13 |
| Low Priority Tests | 4 |

---

## 2. Five Ws (5W Analysis)

### 2.1 WHO

| Role | Responsibility |
|---|---|
| **Test Owner** | QA & Development Team |
| **Stakeholders** | Product Owner, Development Lead, Security Team |
| **Test Executor** | Automated (Playwright headless Chromium) |
| **Reviewers** | Development Lead, QA Lead |
| **Sign-off Authority** | Product Owner |

### 2.2 WHAT

- End-to-end functional testing of the Admin Portal UI
- Authentication and authorization flow verification
- API security endpoint validation
- UI navigation and page rendering verification
- Dashboard data loading with graceful fallback
- CRUD page availability (Wallets, Credentials, Tenants, Audit Logs)
- Role-based access control validation

### 2.3 WHEN

| Trigger | Description |
|---|---|
| **Initial Execution** | 2026-03-13 |
| **Sprint/Phase** | Authentication Implementation & Hardening |
| **Trigger Event** | Post-implementation of cookie-based auth, test-login endpoint, dashboard fixes |
| **CI/CD Frequency** | On every PR to `Tests` branch (planned) |
| **Regression** | Before each release to Test/Production environment |

### 2.4 WHERE

| Component | Technology | URL |
|---|---|---|
| **Admin Portal** | Blazor Server (.NET 10) | `https://localhost:7275` |
| **Web API** | ASP.NET Core REST API | `https://localhost:7190` |
| **Database** | PostgreSQL 17.x (Docker) | Aspire-managed |
| **Cache** | Redis (Docker) | Aspire-managed |
| **Orchestration** | .NET Aspire | Dashboard at `https://localhost:17161` |
| **Browser** | Chromium (headless) | Playwright-bundled |

### 2.5 WHY

- Validate authentication enforcement on all protected pages
- Ensure authorised users can access all admin functionality
- Verify API endpoints reject unauthorised requests
- Confirm dashboard renders correctly with fallback data
- Establish baseline test suite for regression testing
- Provide auditable evidence for compliance and stakeholder review
- Support Government of Western Australia security requirements

---

## 3. Scope

### 3.1 In Scope

| Area | Description | Test Types |
|---|---|---|
| **Authentication** | Login, logout, session management, redirect-to-login | E2E, Security |
| **Authorisation** | Role-based access (Admin, Officer), protected routes | E2E, Security |
| **Navigation** | Sidebar sections, nav links, header controls | E2E, Functional |
| **Dashboard** | Metric cards, recent activity, data loading fallback | E2E, Business |
| **Page Rendering** | Wallets, Credentials, Tenants, Audit Logs, Placeholder pages | E2E, Functional |
| **UI Controls** | Search, filters, create/action buttons, export | E2E, Functional |
| **API Security** | Health check, login validation, auth enforcement, HTTP methods | API, Security |
| **Database Schema** | Table structure verification (22 tables) | Infrastructure |

### 3.2 Out of Scope

| Area | Reason | Future Phase |
|---|---|---|
| **CRUD Operations** | Create/update/delete flows pending full implementation | Phase 2 |
| **Multi-tenant Isolation** | Requires multiple tenant test data setup | Phase 2 |
| **Performance/Load Testing** | Requires dedicated performance environment | Phase 3 |
| **Mobile/Responsive** | Desktop-only in this phase | Phase 2 |
| **Production Environment** | Tests run against local development only | Phase 3 |
| **Azure AD Integration** | Production auth tested separately | Phase 2 |
| **API Business Logic** | Covered by unit and integration test suites | Separate |

---

## 4. Approach & Methodology

### 4.1 Test Strategy

**Approach:** Risk-based automated end-to-end browser testing using Playwright, simulating real user interactions against a fully orchestrated development stack.

**Test Pyramid Position:** Top layer (E2E/UI tests) - validates integration of all components.

```
         /\
        /  \     E2E (Playwright) <-- THIS STRATEGY
       /----\
      / Integ \   Integration Tests (API-level)
     /--------\
    / Unit Tests \  Unit Tests (Domain/Application)
   /--------------\
```

### 4.2 Test Architecture

```
Playwright Test Runner (xUnit)
    |
    +-- PlaywrightFixture (shared browser lifecycle)
    |   +-- Chromium Browser (headless)
    |   +-- LoginAsync() via /auth/test-login endpoint
    |   +-- NewPageAsync() with HTTPS error bypass
    |
    +-- Auth Tests --------> Login/Logout/Redirect flows
    +-- Navigation Tests --> Sidebar/Header rendering
    +-- Dashboard Tests ---> Metric cards, activity feed
    +-- Page Tests --------> Wallets/Credentials/Tenants/Audit
    +-- API Tests ---------> Health/Auth/Security endpoints
    +-- Placeholder Tests -> Stub page rendering
    +-- Evidence Collector -> Automated screenshot capture
```

### 4.3 Authentication Strategy for Tests

The tests use a **test-only login endpoint** (`/auth/test-login`) available only in Development mode:

1. Creates an auth cookie directly without API validation
2. Injects claims (email, role, tenant_id) into the cookie
3. Transfers cookies from .NET HttpClient to Playwright browser context
4. Avoids dependency on the full API login chain during UI testing

**Security:** This endpoint is only available when `Auth:DevelopmentBypass=true` is explicitly set in configuration. It is never available in Test or Production environments.

### 4.4 Risk-Based Prioritisation

| Priority | Risk Level | Test Count | Examples |
|---|---|---|---|
| **Critical** | High impact, high likelihood | 14 | Auth enforcement, session security, API auth |
| **High** | High impact, moderate likelihood | 15 | Page loading, nav functionality, CRUD buttons |
| **Medium** | Moderate impact | 12 | Filter controls, audit features, data display |
| **Low** | Low impact | 3 | Coming soon alerts, stub pages |

### 4.5 Tools & Resources

| Tool | Version | Purpose |
|---|---|---|
| .NET | 10.0 | Runtime |
| Playwright | Latest | Browser automation |
| xUnit | v3 | Test framework |
| FluentAssertions | Latest | Assertion library |
| .NET Aspire | 13.x | Service orchestration |
| Chromium | Playwright-bundled | Test browser |
| PostgreSQL | 17.x | Database (Docker) |
| Redis | Latest | Cache (Docker) |

### 4.6 Test Data Strategy

| Aspect | Approach |
|---|---|
| **Authentication** | Test-login bypass with configurable email/role |
| **Database** | Empty schema (22 tables via EF Core migrations) |
| **Dashboard Stats** | Graceful fallback to zero values when API unavailable |
| **CRUD Pages** | Verify UI controls render; data operations out of scope |

### 4.7 Defect Management

| Severity | Response Time | Example |
|---|---|---|
| **Blocker** | Immediate | Auth bypass, data exposure |
| **Critical** | Same sprint | Page crash, broken workflow |
| **Major** | Next sprint | UI rendering issues, missing controls |
| **Minor** | Backlog | Cosmetic issues, placeholder text |

---

## 5. Test Categories

### 5.1 Functional Tests
Verify that each page/feature behaves according to requirements. Includes page loading, UI control rendering, navigation, and data display.

### 5.2 Security Tests
Validate authentication enforcement, session management, API authorization, and protection against unauthorized access. Critical for government compliance.

### 5.3 Business Tests
Ensure business-critical workflows are accessible: dashboard metrics, tenant management, credential issuance, wallet creation.

### 5.4 Compliance Tests
Audit log functionality, data export capabilities, and role-based access control verification.

### 5.5 Regression Tests
Placeholder/stub pages verify that new development doesn't break existing routing or introduce Blazor circuit errors.

### 5.6 API Tests
Direct HTTP validation of API endpoints for health, authentication, and authorization without browser UI.

---

## 6. Entry & Exit Criteria

### 6.1 Entry Criteria
- .NET Aspire environment starts successfully with all services healthy
- PostgreSQL database schema created (22 tables)
- Admin Portal accessible at configured URL
- API accessible at configured URL
- Test-login endpoint available

### 6.2 Exit Criteria
- All Critical priority tests pass (14/14)
- All High priority tests pass (15/15)
- Overall pass rate >= 95%
- No Blocker or Critical defects open
- Evidence screenshots captured for all key flows
- Test results document generated

---

## 7. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Aspire services fail to start | Medium | High | Health checks, retry logic, fallback evidence |
| Test-login endpoint unavailable | Low | Critical | Verify configuration before test run |
| Blazor circuit errors in tests | Medium | Medium | Wait strategies, NetworkIdle state checks |
| Port conflicts | Low | Medium | Configurable URLs via environment variables |
| CSS not loading in screenshots | Low | Low | Wait for LoadState.NetworkIdle, add render delay |

---

## 8. Deliverables

| Deliverable | Format | Description |
|---|---|---|
| Test Strategy & Methodology | Word (.docx) | This document |
| Test Plan (Test Cases) | Spreadsheet (.csv) | All test cases with full details |
| Test Results & Evidence | Word (.docx) | Execution results with embedded screenshots |
| Evidence Files | PNG + TXT | Raw screenshots and API response captures |
| Console Output | TXT | Full test runner output with timing |
