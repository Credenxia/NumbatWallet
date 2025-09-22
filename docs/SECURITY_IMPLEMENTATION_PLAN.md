# 🔐 NumbatWallet Backend Security Enhancement Implementation Plan

## Executive Summary
The backend needs significant security infrastructure to support SDK certificate-based authentication, multi-tenancy, and advanced credential operations. Currently, only ~15% of the required security infrastructure is implemented.

## Current State Assessment

### ✅ Already Implemented (What We Have)
1. **Basic Infrastructure**
   - PostgreSQL database with EF Core
   - Redis cache service (basic)
   - Basic multi-tenancy middleware
   - GraphQL with HotChocolate (skeleton)
   - Carter REST endpoints (minimal)
   - Azure AD authentication (basic JWT)

2. **Domain Layer**
   - Person, Wallet, Credential entities
   - Basic repository pattern
   - Unit of Work pattern

3. **Application Layer**
   - CQRS pattern (custom, no MediatR)
   - Basic service implementations
   - Event handlers structure

### ✅ Recently Implemented (Session Work)
1. **Certificate Management Infrastructure**
   - ✅ TenantCertificate entity with full lifecycle
   - ✅ CertificateAuthority entity for trusted CAs
   - ✅ CertificateTrustStore for trust relationships
   - ✅ Certificate validation service with chain validation
   - ✅ OCSP/CRL checking infrastructure
   - ✅ RequestSignature value object for signing
   - ✅ Admin UI for certificate management
   - ✅ Database migrations for certificate tables

### ❌ Critical Gaps (What's Still Missing)
1. **Security Infrastructure**
   - NO mTLS middleware integration
   - NO request signing middleware
   - NO certificate pinning
   - NO replay attack prevention middleware

2. **Session Management**
   - NO distributed session store
   - NO session persistence
   - NO device tracking

3. **Advanced Features**
   - NO bulk operations
   - NO zero-knowledge proofs
   - NO selective disclosure
   - NO webhook support
   - NO real-time WebSocket

## Implementation Status

### ✅ Phase 1: Core Security Infrastructure (Week 1 - COMPLETED)

#### 1.1 Certificate Management System ✅ COMPLETED
```
📁 Implemented Components:
├── Domain/
│   ├── Entities/
│   │   ├── TenantCertificate.cs ✅
│   │   ├── CertificateAuthority.cs ✅
│   │   └── CertificateTrustStore.cs ✅
│   └── Services/
│       └── ICertificateValidationService.cs ✅
├── Infrastructure/
│   ├── Services/
│   │   └── CertificateValidationService.cs ✅
│   ├── Repositories/
│   │   ├── TenantCertificateRepository.cs ✅
│   │   ├── CertificateAuthorityRepository.cs ✅
│   │   └── CertificateTrustStoreRepository.cs ✅
│   └── Data/
│       └── Migrations/
│           └── 20250921_AddCertificateManagement.cs ✅
├── Web.Admin/
│   └── Components/Pages/Certificates/
│       └── CertificateManagement.razor ✅
└── Application/
    ├── Commands/Certificates/ 📅 PENDING
    │   ├── UploadCertificateCommand.cs
    │   ├── ValidateCertificateCommand.cs
    │   └── RevokeCertificateCommand.cs
    └── Services/ 📅 PENDING
        └── CertificateManagementService.cs
```

**GitHub Issues to Address:**
- #65: POA-126: Implement Document Signing Certificates ✅ IN PROGRESS
- #183: Implement Certificate Management System ✅ COMPLETED
- #184: Add mTLS Authentication 🔄 IN PROGRESS
- #185: Implement Request Signing/Verification 📅 PENDING

#### 1.2 Request Signing & Verification
```
📁 New Components:
├── Infrastructure/
│   ├── Security/
│   │   ├── RequestSigningService.cs
│   │   ├── SignatureValidator.cs
│   │   ├── NonceTracker.cs (Redis-based)
│   │   └── TimestampValidator.cs
│   └── Middleware/
│       ├── RequestSignatureMiddleware.cs
│       └── ReplayPreventionMiddleware.cs
└── Application/
    └── Interfaces/
        ├── IRequestSigningService.cs
        └── ISignatureValidator.cs
```

**Key Features:**
- SHA256/SHA512 signature validation
- 5-minute timestamp window
- Redis-based nonce tracking
- Multiple algorithm support

### Phase 2: Enhanced Multi-Tenancy & Session Management (Weeks 3-4)

#### 2.1 Tenant Configuration Management
```
📁 New Components:
├── Domain/
│   ├── Entities/
│   │   └── TenantConfiguration.cs
│   └── ValueObjects/
│       ├── RateLimitSettings.cs
│       └── RetentionPolicy.cs
├── Infrastructure/
│   ├── Data/
│   │   └── Configurations/
│   │       └── TenantConfigurationMap.cs
│   └── Services/
│       └── TenantConfigurationService.cs
└── Application/
    └── Commands/Tenants/
        ├── ConfigureTenantCommand.cs
        ├── UpdateRateLimitsCommand.cs
        └── SetRetentionPolicyCommand.cs
```

**GitHub Issues:**
- Enhance existing multi-tenancy implementation
- Add row-level security policies

#### 2.2 Distributed Session Management
```
📁 New Components:
├── Domain/
│   └── Entities/
│       └── UserSession.cs
├── Infrastructure/
│   ├── Redis/
│   │   ├── SessionStore.cs
│   │   ├── SessionSerializer.cs
│   │   └── SessionExpirationHandler.cs
│   └── Services/
│       └── DistributedSessionService.cs
└── Application/
    ├── Commands/Sessions/
    │   ├── CreateSessionCommand.cs
    │   ├── ExtendSessionCommand.cs
    │   └── InvalidateSessionCommand.cs
    └── Queries/Sessions/
        └── GetActiveSessionsQuery.cs
```

### Phase 3: Admin Portal Certificate Management UI (Week 5)

#### 3.1 Certificate Management Pages
```
📁 Admin Portal Components:
├── Web.Admin/
│   ├── Components/Pages/
│   │   ├── Certificates/
│   │   │   ├── CertificateList.razor
│   │   │   ├── CertificateUpload.razor
│   │   │   ├── CertificateValidation.razor
│   │   │   └── TrustStoreManagement.razor
│   │   └── Security/
│   │       ├── SecurityDashboard.razor
│   │       ├── SessionMonitor.razor
│   │       └── ThreatDetection.razor
│   └── Services/
│       ├── CertificateService.cs
│       └── SecurityMonitoringService.cs
```

**GitHub Issues to Create:**
- NEW: Create certificate management UI
- NEW: Implement certificate testing interface
- NEW: Add trust store management

### Phase 4: API Enhancement & GraphQL Implementation (Weeks 6-7)

#### 4.1 GraphQL Schema Enhancement
```graphql
type Mutation {
  # Certificate Operations
  uploadCertificate(input: UploadCertificateInput!): CertificatePayload!
  validateCertificate(input: ValidateCertificateInput!): ValidationResult!
  revokeCertificate(id: ID!, reason: String!): CertificatePayload!

  # Session Operations
  createSession(input: CreateSessionInput!): SessionPayload!
  invalidateSession(id: ID!): Boolean!

  # Bulk Operations
  bulkIssueCredentials(input: BulkIssueInput!): BulkOperationResult!
  bulkRevokeCredentials(ids: [ID!]!): BulkOperationResult!
}

type Subscription {
  certificateStatusChanged(tenantId: ID!): CertificateStatusUpdate!
  sessionActivity(userId: ID!): SessionEvent!
  bulkOperationProgress(operationId: ID!): ProgressUpdate!
}
```

**GitHub Issues:**
- #153: Create Admin GraphQL API for management operations
- Enhance existing GraphQL implementation

#### 4.2 WebSocket Support
```
📁 New Components:
├── Web.Api/
│   ├── Hubs/
│   │   ├── NotificationHub.cs
│   │   ├── ProgressHub.cs
│   │   └── SecurityAlertHub.cs
│   └── Services/
│       └── RealtimeNotificationService.cs
```

### Phase 5: Performance & Monitoring (Week 8)

#### 5.1 Caching Strategy
```
📁 New Components:
├── Infrastructure/
│   ├── Caching/
│   │   ├── MultiLevelCache.cs
│   │   ├── CacheInvalidationService.cs
│   │   └── CacheWarmupService.cs
│   └── Performance/
│       ├── QueryOptimizer.cs
│       └── DatabaseIndexManager.cs
```

#### 5.2 Monitoring & Observability
```
📁 New Components:
├── Infrastructure/
│   ├── Monitoring/
│   │   ├── MetricsCollector.cs
│   │   ├── DistributedTracing.cs
│   │   └── SecurityAuditLogger.cs
│   └── HealthChecks/
│       ├── CertificateHealthCheck.cs
│       ├── SessionStoreHealthCheck.cs
│       └── SecurityServicesHealthCheck.cs
```

**GitHub Issues:**
- #169: Create system health and metrics dashboard
- NEW: Implement OpenTelemetry tracing

### Phase 6: Compliance & Audit (Week 9)

#### 6.1 TDIF Compliance Implementation
```
📁 New Components:
├── Domain/
│   └── Services/
│       ├── IIdentityProofingService.cs
│       └── ICredentialAssuranceService.cs
├── Infrastructure/
│   └── Compliance/
│       ├── TdifComplianceValidator.cs
│       ├── IdentityProofingService.cs
│       └── AuditLogService.cs
```

**GitHub Issues:**
- NEW: Implement TDIF compliance validation
- NEW: Add comprehensive audit logging

## Database Migrations Required

```sql
-- 1. Certificate Management Tables
CREATE TABLE tenant_certificates (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    certificate_data TEXT NOT NULL,
    thumbprint VARCHAR(64) UNIQUE,
    subject_dn TEXT,
    issuer_dn TEXT,
    valid_from TIMESTAMPTZ,
    valid_to TIMESTAMPTZ,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 2. Session Store Table
CREATE TABLE user_sessions (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    user_id UUID NOT NULL,
    session_token VARCHAR(512) UNIQUE,
    device_id VARCHAR(256),
    ip_address INET,
    user_agent TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL,
    last_activity TIMESTAMPTZ
);

-- 3. Request Nonce Tracking (Redis, but backup in DB)
CREATE TABLE request_nonces (
    nonce VARCHAR(256) PRIMARY KEY,
    tenant_id UUID,
    used_at TIMESTAMPTZ DEFAULT NOW(),
    expires_at TIMESTAMPTZ
);

-- 4. Certificate Trust Store
CREATE TABLE certificate_authorities (
    id UUID PRIMARY KEY,
    name VARCHAR(256),
    certificate_data TEXT,
    is_trusted BOOLEAN DEFAULT false,
    trust_level INT DEFAULT 0
);

-- 5. Audit Log Enhanced
CREATE TABLE security_audit_log (
    id UUID PRIMARY KEY,
    timestamp TIMESTAMPTZ NOT NULL,
    tenant_id UUID NOT NULL,
    user_id UUID,
    event_type VARCHAR(100) NOT NULL,
    resource_type VARCHAR(50),
    resource_id UUID,
    action VARCHAR(50) NOT NULL,
    result VARCHAR(20) NOT NULL,
    ip_address INET,
    user_agent TEXT,
    certificate_thumbprint VARCHAR(64),
    signature_verified BOOLEAN,
    metadata JSONB,
    INDEX idx_audit_tenant_time (tenant_id, timestamp DESC),
    INDEX idx_audit_certificate (certificate_thumbprint)
);
```

## Security Checklist

### Critical Security Requirements
- [ ] mTLS certificate validation with chain verification
- [ ] OCSP and CRL checking implementation
- [ ] Request signature validation (SHA256/512)
- [ ] Replay attack prevention (nonce tracking)
- [ ] 5-minute timestamp window validation
- [ ] Certificate pinning for high-security operations
- [ ] Dynamic trust store updates
- [ ] Per-tenant certificate isolation
- [ ] Session hijacking prevention
- [ ] Rate limiting per tenant/endpoint
- [ ] Audit logging for all security events
- [ ] Certificate revocation list management
- [ ] Zero-knowledge proof implementation
- [ ] Selective disclosure for credentials

## Testing Strategy

### 1. Certificate Validation Tests
- Valid certificate chain verification
- Expired certificate rejection
- Revoked certificate detection
- Self-signed certificate handling
- Certificate pinning validation
- OCSP responder timeout handling
- CRL download and parsing

### 2. Security Integration Tests
- mTLS handshake simulation
- Request signing verification
- Replay attack prevention
- Session timeout handling
- Multi-tenant isolation
- Certificate rotation scenarios
- Trust store updates

### 3. Performance Tests
- 10,000 concurrent certificate validations
- Session store under load (100K active sessions)
- Cache hit ratio optimization
- Database query performance
- Bulk operation throughput
- WebSocket connection scaling

### 4. Penetration Testing Scenarios
- Certificate spoofing attempts
- Session hijacking attempts
- Replay attack simulations
- SQL injection prevention
- XSS prevention in admin portal
- CSRF token validation

## Risk Mitigation

### High Priority Risks
1. **Certificate Compromise**:
   - Immediate revocation and notification
   - Automatic trust store updates
   - Audit trail of all certificate operations

2. **Session Hijacking**:
   - Device fingerprinting and IP validation
   - Session binding to certificate
   - Automatic session invalidation on anomaly

3. **Replay Attacks**:
   - Redis-based nonce tracking with TTL
   - Request timestamp validation
   - Signature verification

4. **Performance Degradation**:
   - Multi-level caching strategy
   - Query optimization
   - Connection pooling

5. **Data Breach**:
   - Encryption at rest (AES-256)
   - TLS 1.3 minimum
   - Key rotation every 30 days

## Success Metrics

### Performance KPIs
- Certificate validation < 50ms (p95)
- Session creation < 100ms
- Bulk operations: 1000 credentials/minute
- API response time < 500ms (p95)
- Cache hit ratio > 80%

### Security KPIs
- Zero security breaches
- 100% audit trail coverage
- Certificate validation success rate > 99.9%
- Session timeout compliance 100%

### Operational KPIs
- 99.95% uptime (4.5 hours downtime/year)
- Error rate < 0.1%
- Recovery time < 1 hour
- Backup success rate 100%

## Implementation Priorities

### Week 1-2: Foundation
1. Fix Web.Admin compilation errors ✅
2. Create certificate domain entities
3. Database migration for certificate tables
4. Basic certificate validation service
5. Certificate upload UI

### Week 3-4: Core Security
1. mTLS middleware implementation
2. Request signing verification
3. Session management infrastructure
4. OCSP/CRL validation

### Week 5-6: Admin Portal
1. Certificate management UI
2. Security dashboard
3. Session monitoring
4. Audit log viewer

### Week 7-8: Advanced Features
1. Bulk operations
2. WebSocket support
3. GraphQL subscriptions
4. Webhook notifications

### Week 9: Hardening
1. Performance optimization
2. Security testing
3. Compliance validation
4. Documentation

## Cost Estimation

### Development Resources
- 2 Senior Backend Developers: 9 weeks
- 1 Security Engineer: 9 weeks
- 1 Frontend Developer: 4 weeks
- 1 DevOps Engineer: 3 weeks

### Infrastructure Costs (Monthly)
- Certificate validation service: $500
- Enhanced Redis cluster: $2,000
- Additional monitoring: $1,000
- Security scanning tools: $500
- Total additional: ~$4,000/month

## GitHub Issue Mapping

### Existing Issues to Address
- #65: POA-126: Implement Document Signing Certificates
- #53: POA-092: Security validation test suite
- #153: Create Admin GraphQL API for management operations
- #169: Create system health and metrics dashboard
- #170: Implement configuration management interface
- #174: Implement key rotation management interface

### New Issues to Create
1. Implement mTLS certificate validation infrastructure
2. Add OCSP and CRL checking services
3. Create request signing verification middleware
4. Implement distributed session management
5. Add certificate management UI to admin portal
6. Create security monitoring dashboard
7. Implement bulk credential operations
8. Add WebSocket support for real-time updates
9. Create certificate trust store management
10. Implement TDIF compliance validation

## Next Immediate Steps

1. **Fix Web.Admin compilation errors** (current blocking issue)
2. **Create certificate domain entities**:
   - TenantCertificate.cs
   - CertificateAuthority.cs
   - CertificateTrustStore.cs

3. **Create database migration** for certificate tables

4. **Implement CertificateValidationService** with:
   - X.509 parsing
   - Chain validation
   - Basic OCSP checking

5. **Add certificate upload UI** in Admin portal

6. **Create mTLS middleware** for API

7. **Implement request signing verification**

8. **Setup distributed session store**

9. **Add comprehensive security tests**

10. **Create security documentation**

---
*Document Version: 1.0*
*Created: September 2025*
*Status: Implementation Planning*
*Priority: CRITICAL*
*Owner: Backend Security Team*