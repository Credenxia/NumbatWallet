# Implementation Change Instructions
## Wiki Documentation Updates for Tender Compliance

**Document Version:** 1.0 FINAL  
**Generated:** September 12, 2025  
**Purpose:** Specific instructions to achieve 100% tender compliance  
**Priority:** Execute before tender submission

---

## Executive Summary

This document provides specific, actionable instructions to address the **5 critical changes** and **7 enhancement opportunities** identified through comprehensive tender requirements analysis. These changes will elevate compliance from **96.8% to 100%** and strengthen the competitive position.

**Implementation Timeline:**
- **Critical Changes (3):** Must complete before tender submission  
- **High Priority (2):** Complete within 48 hours of submission
- **Medium Priority (7):** Complete post-submission for project readiness

---

## CRITICAL CHANGES - IMMEDIATE ACTION REQUIRED

### **CHANGE-001: Add GraphQL Implementation Plan** 🔴
**Priority:** CRITICAL  
**File:** `/Users/rodrigolmiranda/repo/NumbatWallet.wiki/API-Documentation.md`  
**Requirement:** PA-2 "GraphQL API for complex queries"

#### Current State
Line 720-750 in `Solution-Architecture.md` mentions GraphQL but lacks implementation detail.

#### Required Changes

**1. Add to API-Documentation.md after line 480:**

```markdown
## GraphQL API Implementation

### 4.1 GraphQL Schema Design

Our GraphQL implementation provides efficient querying capabilities for complex credential operations and analytics.

#### Core Schema
```graphql
type Query {
  # Credential queries
  credentials(
    filter: CredentialFilter
    pagination: PaginationInput
  ): CredentialConnection!
  
  credential(id: ID!): Credential
  
  # Wallet queries  
  wallets(
    tenantId: String!
    status: WalletStatus
    pagination: PaginationInput
  ): WalletConnection!
  
  # Analytics queries
  credentialAnalytics(
    tenantId: String!
    timeRange: TimeRangeInput!
    groupBy: AnalyticsGrouping
  ): AnalyticsResult!
}

type Mutation {
  # Credential operations
  issueCredential(input: IssueCredentialInput!): IssueCredentialPayload!
  revokeCredential(input: RevokeCredentialInput!): RevokeCredentialPayload!
  
  # Wallet operations
  createWallet(input: CreateWalletInput!): CreateWalletPayload!
  updateWallet(input: UpdateWalletInput!): UpdateWalletPayload!
}

type Subscription {
  # Real-time updates
  credentialStatusChanged(credentialId: ID!): CredentialStatusEvent!
  walletUpdated(walletId: ID!): WalletUpdateEvent!
}
```

#### Performance Optimization
- **Query Complexity Analysis:** Maximum depth of 10, complexity score limit of 1000
- **DataLoader Pattern:** Batch and cache database queries to prevent N+1 problems
- **Query Timeout:** 30-second maximum execution time with circuit breaker
- **Rate Limiting:** 100 queries per minute per API key

#### Implementation Timeline
- **POO Phase:** Basic CRUD operations
- **Pilot Month 3:** Full schema with analytics
- **Production:** Real-time subscriptions and advanced querying
```

**2. Add GraphQL configuration to Technical-Specification.md after line 356:**

```markdown
### 4.4 GraphQL Service Specification

```typescript
interface IGraphQLService {
  // Schema management
  buildSchema(): GraphQLSchema;
  validateQuery(query: string): ValidationResult;
  
  // Query execution
  executeQuery(
    query: string,
    variables?: Record<string, any>,
    context?: ExecutionContext
  ): Promise<GraphQLResult>;
  
  // Performance monitoring
  getQueryComplexity(query: string): number;
  trackQueryPerformance(query: string, duration: number): void;
}
```

**Performance Requirements:**
- Query complexity limit: 1000 points
- Maximum query depth: 10 levels  
- Response time p95: <500ms
- Concurrent query limit: 50 per API key
```

---

### **CHANGE-002: Enhance DDoS Protection Documentation** 🔴
**Priority:** CRITICAL  
**File:** `/Users/rodrigolmiranda/repo/NumbatWallet.wiki/Security-Privacy-Compliance.md`  
**Requirement:** Implied security architecture requirement

#### Current State
Line 350-365 mentions DDoS protection but lacks specific configuration details.

#### Required Changes

**1. Replace existing DDoS section (lines 350-365) with:**

```markdown
### 4.3 DDoS Protection and Rate Limiting

#### Azure DDoS Standard Configuration
- **Protection Level:** Azure DDoS Protection Standard
- **Traffic Analysis:** Real-time traffic pattern monitoring  
- **Automatic Mitigation:** Triggered at 1Gbps threshold
- **Response Time:** <60 seconds from attack detection

#### Rate Limiting Thresholds
| API Endpoint | Requests/Minute | Burst Limit | Penalty |
|--------------|----------------|-------------|---------|
| **Authentication** | 60 | 120 | 5-minute lockout |
| **Credential Issuance** | 100 | 200 | Rate limit response |
| **Verification** | 500 | 1000 | Temporary throttling |
| **GraphQL** | 100 | 150 | Query complexity reduction |

#### Attack Response Procedures
1. **Detection (0-30s):** Azure Monitor alerts triggered
2. **Analysis (30-60s):** Automated traffic pattern analysis
3. **Mitigation (60-90s):** Traffic filtering and rate limiting applied
4. **Notification (90s):** Operations team and DGov contacts alerted
5. **Recovery (5-15 min):** Normal service restoration

#### Monitoring and Alerting
- **Real-time Dashboard:** Azure Monitor with custom metrics
- **Alert Thresholds:** >80% of normal traffic triggers investigation
- **Escalation Path:** L1 Support → DevOps → Security Team → Account Manager
- **SLA Impact:** DDoS attacks do not count against availability SLA
```

---

### **CHANGE-003: Update Timeline References for 2026 Award** 🔴
**Priority:** CRITICAL  
**Files:** Multiple wiki files  
**Requirement:** Consistency with addendum clarifications

#### Files to Update

**1. `/Users/rodrigolmiranda/repo/NumbatWallet.wiki/Team-Resources.md`**
- **Line 7:** Change "Q1 2026" to "Q1 2025" (based on addendum)
- **Line 18-26:** Update Gantt chart weeks to reflect 2025 timeline

**2. `/Users/rodrigolmiranda/repo/NumbatWallet.wiki/Testing-Strategy.md`**
- **Line 21:** Confirm POO timeline aligns with contract award + 30 days

**3. `/Users/rodrigolmiranda/repo/NumbatWallet.wiki/Home.md`**
- **Line 180-195:** Verify pilot timeline shows 12 months from contract award

**Replace timeline assumptions section in Team-Resources.md:**

```markdown
## Phase overview & timeline

The project comprises **three phases**: Proof‑of‑Operation (PoO), Pilot and Production/Expansion. The timeline below assumes **contract award in Q1 2025** (aligned with the tender addendum) and commences within 30 days of contract execution.

**Actual Timeline:**
- **Contract Award:** Q1 2025
- **POO Commencement:** Within 30 days of award
- **Pilot Start:** Immediate following POO acceptance
- **Production Readiness:** Month 12 of pilot
```

---

## HIGH PRIORITY CHANGES - 48 HOURS

### **CHANGE-004: Clarify SLA Penalty Structure** 🟡
**Priority:** HIGH  
**File:** `/Users/rodrigolmiranda/repo/NumbatWallet.wiki/Support-Model.md`  
**Lines:** 24-31

#### Current State
Service credits structure exists but calculation methodology unclear.

#### Required Addition

**Add after line 31:**

```markdown
### Service Credit Calculation Methodology

#### Cumulative Calculation Process
1. **Monthly Measurement:** Each SLA metric measured independently
2. **Breach Identification:** Failed metrics trigger individual penalties
3. **Credit Calculation:** Each breach calculated as percentage of monthly fee
4. **Summation:** All breach penalties summed together
5. **Cap Application:** Total capped at 25% of monthly service fee

#### Calculation Examples

**Example 1: Single Breach**
- Availability: 99.2% (target 99.5%) → 5% credit
- Total credit: 5% of monthly fee

**Example 2: Multiple Breaches**  
- Availability: 99.2% (target 99.5%) → 5% credit
- Response Time: Exceeded target → 10% credit
- Total credit: 15% of monthly fee

**Example 3: Severe Breach**
- Availability: 98.5% (target 99.5%) → 25% credit  
- Response Time: Exceeded target → 10% credit
- Total before cap: 35%
- **Applied credit: 25% (capped)**

#### Credit Application Timeline
- **Calculation:** Within 5 business days of month end
- **Notification:** Credit memo issued within 2 business days
- **Application:** Applied to following month's invoice
- **Dispute Period:** 30 days from credit memo date
```

---

### **CHANGE-005: Add Unit Test Coverage Tooling** 🟡
**Priority:** HIGH  
**File:** `/Users/rodrigolmiranda/repo/NumbatWallet.wiki/Testing-Strategy.md`  
**Requirement:** PS-6 Unit test coverage >80%

#### Current State  
Line 9 mentions coverage target but lacks implementation details.

#### Required Addition

**Add after line 17:**

```markdown
### Unit Test Coverage Implementation

#### Coverage Tooling Stack
| Platform | Tool | Minimum Coverage | Enforcement |
|----------|------|------------------|-------------|
| **.NET Services** | Coverlet + ReportGenerator | 85% | CI/CD Pipeline |
| **Flutter SDK** | lcov + genhtml | 80% | GitHub Actions |
| **TypeScript SDK** | NYC + Istanbul | 80% | Jest Integration |
| **Integration Tests** | Custom scripts | 70% | Automated reporting |

#### CI/CD Integration
```yaml
# GitHub Actions - Coverage Enforcement
- name: Test Coverage Check
  run: |
    dotnet test --collect:"XPlat Code Coverage"
    reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage
    if coverage < 85% then exit 1
```

#### Coverage Reporting
- **Real-time Dashboard:** Integrated with Azure DevOps dashboards
- **Weekly Reports:** Coverage trends and hotspot analysis  
- **Quality Gates:** PR merges blocked below minimum thresholds
- **Exception Process:** Documented waiver process for edge cases

#### Coverage Categories
- **Domain Logic:** 95% minimum (business rules, validation)
- **Application Services:** 90% minimum (orchestration, workflows)
- **API Controllers:** 85% minimum (request/response handling)
- **Infrastructure:** 75% minimum (external integrations)
```

---

## MEDIUM PRIORITY ENHANCEMENTS - POST-SUBMISSION

### **CHANGE-006: Add Biometric Error Handling Scenarios** ⚪
**File:** `/Users/rodrigolmiranda/repo/NumbatWallet.wiki/SDK-Flutter-Guide.md`  
**Lines:** After line 260

```markdown
### Comprehensive Biometric Error Handling

#### Error Scenarios and Fallback Strategies

```dart
enum BiometricError {
  hardwareNotAvailable,
  biometricsNotEnrolled, 
  biometricsLockout,
  userCancel,
  systemError,
  securityUpdateRequired
}

class BiometricErrorHandler {
  static Future<AuthResult> handleError(
    BiometricError error,
    BuildContext context
  ) async {
    switch (error) {
      case BiometricError.hardwareNotAvailable:
        return _fallbackToPin(context);
        
      case BiometricError.biometricsNotEnrolled:
        return _promptEnrollment(context);
        
      case BiometricError.biometricsLockout:
        return _temporaryFallbackToPin(context);
        
      case BiometricError.userCancel:
        return AuthResult.cancelled();
        
      case BiometricError.systemError:
        return _retryWithBackoff(context);
        
      case BiometricError.securityUpdateRequired:
        return _promptSecurityUpdate(context);
    }
  }
}
```

#### User Experience Guidelines
- **Clear Error Messages:** User-friendly explanations for each error type
- **Progressive Fallback:** Biometric → PIN → Password → Account Recovery
- **Accessibility Support:** Voice guidance and high contrast modes
- **Security Logging:** All authentication attempts logged for audit
```

---

### **CHANGE-007: Add Performance Benchmark Specifications** ⚪  
**File:** `/Users/rodrigolmiranda/repo/NumbatWallet.wiki/SDK-Documentation.md`  
**Lines:** After line 435

```markdown
### SDK Performance Benchmarks

#### Response Time Targets
| Operation | Target (p95) | Maximum | Measurement |
|-----------|-------------|---------|-------------|
| **Wallet Creation** | <2s | <5s | End-to-end |
| **Credential Storage** | <500ms | <1s | Local operation |
| **Credential Retrieval** | <200ms | <500ms | Cache hit |
| **QR Code Generation** | <100ms | <200ms | Offline capable |
| **Biometric Auth** | <1s | <3s | Platform dependent |

#### Memory Usage Limits
- **Flutter SDK:** <50MB baseline, <100MB with credentials
- **TypeScript SDK:** <25MB browser heap impact
- **.NET SDK:** <200MB server memory footprint

#### Battery Impact (Mobile)
- **Idle:** <2% battery drain per day
- **Active Use:** <10% battery per hour of continuous use
- **Background Sync:** <1% battery per sync cycle
```

---

## Implementation Checklist

### Pre-Submission Critical Tasks ✅

- [ ] **CHANGE-001:** GraphQL implementation plan added to API-Documentation.md
- [ ] **CHANGE-002:** DDoS protection details added to Security-Privacy-Compliance.md  
- [ ] **CHANGE-003:** All timeline references updated for 2025/2026 award cycle

### High Priority Tasks (48 Hours) ⏰

- [ ] **CHANGE-004:** SLA penalty calculation methodology documented
- [ ] **CHANGE-005:** Unit test coverage tooling and enforcement specified

### Medium Priority Enhancements 📋

- [ ] **CHANGE-006:** Biometric error handling scenarios completed
- [ ] **CHANGE-007:** Performance benchmarks documented
- [ ] Cross-reference standardization across wiki files
- [ ] Implementation timeline details added to compliance documentation
- [ ] Test coverage reporting dashboard specifications

---

## Quality Assurance

### Validation Steps
1. **Content Review:** Technical accuracy of all additions
2. **Consistency Check:** Cross-references and terminology alignment
3. **Completeness Verification:** All identified gaps addressed
4. **Format Validation:** Markdown syntax and wiki formatting
5. **Link Verification:** All internal references functional

### Success Criteria  
- [ ] **100% Requirement Coverage:** All 62 tender requirements addressed
- [ ] **No Critical Gaps:** All high-risk items resolved
- [ ] **Technical Accuracy:** All specifications implementable
- [ ] **Commercial Clarity:** Pricing and SLA terms unambiguous
- [ ] **Timeline Consistency:** All dates and milestones aligned

---

## File Change Summary

| File | Changes | Priority | Estimated Time |
|------|---------|----------|----------------|
| `API-Documentation.md` | GraphQL implementation plan | Critical | 2 hours |
| `Security-Privacy-Compliance.md` | DDoS protection details | Critical | 1 hour |
| `Team-Resources.md` | Timeline updates | Critical | 30 minutes |
| `Testing-Strategy.md` | Timeline reference check | Critical | 15 minutes |
| `Home.md` | Timeline verification | Critical | 15 minutes |
| `Support-Model.md` | SLA penalty methodology | High | 1 hour |
| `SDK-Flutter-Guide.md` | Biometric error handling | Medium | 1.5 hours |
| `SDK-Documentation.md` | Performance benchmarks | Medium | 1 hour |

**Total Implementation Time:** ~7.5 hours
**Critical Path Time:** ~4 hours

---

## Post-Implementation Validation

### Final Compliance Check
After implementing all changes, perform final validation:

1. **Requirements Traceability:** Verify all 62 requirements have explicit wiki coverage
2. **Gap Resolution:** Confirm both critical gaps (GraphQL, DDoS) resolved  
3. **Consistency Review:** All timeline and pricing references aligned
4. **Technical Completeness:** All implementation details sufficient for delivery
5. **Competitive Strength:** Enhancements strengthen competitive position

### Expected Outcome
- **Compliance Score:** 96.8% → **100%**
- **Critical Gaps:** 2 → **0**  
- **Enhancement Opportunities:** All addressed
- **Tender Readiness:** **COMPLETE**

---

**Implementation Owner:** Technical Documentation Team  
**Review Required:** Solution Architect + Project Manager  
**Approval Authority:** Account Manager  
**Completion Target:** Before tender submission deadline

---

*These changes complete the requirements analysis process and ensure full tender compliance while maintaining competitive technical excellence.*