# Backend Requirements for NumbatWallet SDK Support

## Overview
This document outlines the backend requirements needed to support all SDK features including security, multi-tenancy, and advanced capabilities identified in the SDK implementation plan.

## 1. API Architecture Requirements

### 1.1 Protocol Support
- **GraphQL API** (Primary)
  - Schema versioning support
  - Subscription support for real-time updates
  - Batch query optimization
  - Field-level authorization

- **REST API** (Legacy/Compatibility)
  - OpenAPI 3.0 specification
  - HATEOAS for discoverability
  - Pagination with cursor support
  - Rate limiting per endpoint

### 1.2 WebSocket Support
- Real-time credential updates
- Session status notifications
- Bulk operation progress
- Connection heartbeat/ping-pong

## 2. Security Infrastructure

### 2.1 mTLS Certificate Validation
**Backend Components Needed:**
```yaml
Certificate Validation Service:
  - X.509 certificate parsing
  - Certificate chain validation
  - Certificate revocation list (CRL) checking
  - OCSP (Online Certificate Status Protocol) support
  - Certificate pinning validation
  - Dynamic certificate trust store updates
```

### 2.2 Request Signing Verification
**Implementation Requirements:**
```yaml
Signature Verification:
  - SHA256/SHA512 signature validation
  - Timestamp validation (±5 minutes window)
  - Nonce tracking for replay prevention
  - Multiple signing algorithm support
  - Signature key rotation handling
```

### 2.3 Multi-Factor Authentication
**Auth Methods to Support:**
- Azure Entra ID (OIDC/SAML)
- ServiceWA OAuth 2.0
- Biometric verification (FIDO2/WebAuthn)
- mTLS client certificates
- API key authentication (service accounts)

## 3. Multi-Tenancy Requirements

### 3.1 Tenant Isolation
**Database Architecture:**
```sql
-- Tenant-aware tables
CREATE TABLE credentials (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    wallet_id UUID NOT NULL,
    -- ... other fields
    CONSTRAINT fk_tenant FOREIGN KEY (tenant_id)
        REFERENCES tenants(id) ON DELETE CASCADE
);

-- Row-level security
CREATE POLICY tenant_isolation ON credentials
    FOR ALL
    USING (tenant_id = current_tenant_id());
```

### 3.2 Tenant Configuration
**Per-Tenant Settings:**
```json
{
  "tenantId": "wa-government",
  "configuration": {
    "authProviders": ["azure-ad", "servicewa"],
    "certificateAuthorities": ["wa-ca", "national-ca"],
    "rateLimit": {
      "requestsPerMinute": 1000,
      "burstSize": 100
    },
    "features": {
      "bulkOperations": true,
      "offlineMode": true,
      "biometricAuth": true
    },
    "dataResidency": "australia-east",
    "retentionPolicy": {
      "credentials": "7 years",
      "auditLogs": "10 years"
    }
  }
}
```

## 4. Session Management

### 4.1 Distributed Session Store
**Redis Configuration:**
```yaml
Session Store:
  - Redis Cluster (6+ nodes)
  - Session TTL: 30 minutes (configurable)
  - Sliding expiration support
  - Session replication factor: 3
  - Encryption at rest
  - AOF persistence for durability
```

### 4.2 Session Data Model
```json
{
  "sessionId": "uuid",
  "tenantId": "tenant-uuid",
  "userId": "user-uuid",
  "authMethod": "azure-ad",
  "createdAt": "2025-01-01T10:00:00Z",
  "expiresAt": "2025-01-01T10:30:00Z",
  "metadata": {
    "ipAddress": "203.1.2.3",
    "userAgent": "NumbatWallet-SDK/2.0",
    "deviceId": "device-uuid"
  },
  "permissions": ["read", "write", "share"],
  "mfaCompleted": true
}
```

## 5. Credential Operations

### 5.1 Bulk Operations Support
**Batch Processing Requirements:**
```yaml
Bulk Issue Endpoint:
  - Maximum batch size: 1000 credentials
  - Async processing with job queue
  - Progress tracking via WebSocket
  - Partial failure handling
  - Rollback capability
  - Rate limiting: 10 batches/minute
```

### 5.2 Selective Disclosure
**Zero-Knowledge Proof Support:**
```yaml
Presentation Engine:
  - Field-level disclosure control
  - Derived credentials generation
  - Predicate proofs (age > 18)
  - Merkle tree for claim verification
  - BBS+ signatures for unlinkability
```

### 5.3 Revocation Management
**Revocation Infrastructure:**
```yaml
Revocation Service:
  - Revocation list publication
  - Real-time revocation status
  - Revocation reason codes
  - Audit trail for revocations
  - Batch revocation support
  - Grace period handling
```

## 6. Performance & Caching

### 6.1 Caching Strategy
**Multi-Level Cache:**
```yaml
L1 Cache (Application):
  - In-memory cache (Redis)
  - TTL: 5 minutes
  - Size: 10GB per node

L2 Cache (CDN):
  - CloudFront/Azure CDN
  - TTL: 1 hour
  - Geographic distribution

Cache Invalidation:
  - Event-driven invalidation
  - Tag-based invalidation
  - Partial cache updates
```

### 6.2 Database Optimization
**Performance Requirements:**
```sql
-- Indexing strategy
CREATE INDEX idx_credentials_wallet_tenant
    ON credentials(wallet_id, tenant_id)
    WHERE deleted_at IS NULL;

CREATE INDEX idx_credentials_type_status
    ON credentials(type, status)
    WHERE deleted_at IS NULL;

-- Partitioning by tenant
CREATE TABLE credentials_2025_q1
    PARTITION OF credentials
    FOR VALUES FROM ('2025-01-01') TO ('2025-04-01');
```

## 7. Monitoring & Observability

### 7.1 Metrics Collection
**Required Metrics:**
```yaml
Application Metrics:
  - API response times (p50, p95, p99)
  - Error rates by endpoint
  - Active sessions count
  - Certificate validation failures
  - Cache hit rates

Business Metrics:
  - Credentials issued/hour
  - Verifications performed/day
  - Unique active users/month
  - Tenant usage statistics
```

### 7.2 Logging Infrastructure
**Structured Logging:**
```json
{
  "timestamp": "2025-01-01T10:00:00Z",
  "level": "INFO",
  "service": "credential-api",
  "tenantId": "wa-government",
  "userId": "user-123",
  "requestId": "req-456",
  "operation": "IssueCredential",
  "duration": 245,
  "status": "success",
  "metadata": {
    "credentialType": "DriversLicense",
    "ipAddress": "203.1.2.3"
  }
}
```

### 7.3 Distributed Tracing
**OpenTelemetry Configuration:**
```yaml
Tracing:
  - Sampling rate: 10% (configurable)
  - Trace context propagation
  - Service dependency mapping
  - Database query tracing
  - External API call tracing
```

## 8. Data Management

### 8.1 Backup & Recovery
**Backup Strategy:**
```yaml
Database Backup:
  - Continuous replication to standby
  - Point-in-time recovery (30 days)
  - Daily snapshots
  - Cross-region backup replication
  - Encrypted backups

Recovery Targets:
  - RTO (Recovery Time Objective): 1 hour
  - RPO (Recovery Point Objective): 5 minutes
```

### 8.2 Data Retention
**Compliance Requirements:**
```yaml
Retention Policies:
  - Credentials: 7 years after expiry
  - Audit logs: 10 years
  - Session data: 30 days
  - Performance metrics: 90 days
  - Error logs: 180 days

Data Purging:
  - Automated deletion jobs
  - Soft delete with grace period
  - Compliance audit trail
```

## 9. Integration Requirements

### 9.1 External Services
**Required Integrations:**
```yaml
Identity Providers:
  - Azure Entra ID
  - ServiceWA OAuth
  - myGovID

Certificate Authorities:
  - Australian Government CA
  - State Government CAs

Notification Services:
  - Email (SendGrid/SES)
  - SMS (Twilio)
  - Push notifications (FCM/APNS)
```

### 9.2 Webhook Support
**Event Notifications:**
```json
{
  "eventType": "credential.issued",
  "timestamp": "2025-01-01T10:00:00Z",
  "tenantId": "wa-government",
  "data": {
    "credentialId": "cred-123",
    "walletId": "wallet-456",
    "type": "DriversLicense"
  },
  "signature": "sha256-signature"
}
```

## 10. Compliance & Audit

### 10.1 TDIF Compliance
**Required Capabilities:**
```yaml
Identity Proofing:
  - IPL2: Document verification + liveness
  - IPL3: In-person verification

Credential Assurance:
  - CAL2: Standard credentials
  - CAL3: High-assurance credentials

Federation:
  - Attribute exchange
  - Consent management
  - Privacy preservation
```

### 10.2 Audit Logging
**Audit Events:**
```sql
CREATE TABLE audit_log (
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
    metadata JSONB,
    INDEX idx_audit_tenant_time (tenant_id, timestamp DESC)
);
```

## 11. High Availability

### 11.1 Infrastructure Requirements
**Deployment Architecture:**
```yaml
Load Balancing:
  - Active-Active configuration
  - Health check endpoints
  - Automatic failover
  - Geographic distribution

Database:
  - Primary-Secondary replication
  - Automatic failover
  - Read replicas for queries
  - Connection pooling

Message Queue:
  - RabbitMQ/Kafka cluster
  - Message persistence
  - Dead letter queues
```

### 11.2 Service Level Objectives
**SLA Targets:**
- Availability: 99.95% (4.5 hours downtime/year)
- API Response Time: < 500ms (p95)
- Error Rate: < 0.1%
- Certificate Validation: < 50ms

## 12. Development Support

### 12.1 Testing Infrastructure
**Test Environment Requirements:**
```yaml
Sandbox Environment:
  - Isolated tenant for testing
  - Mock identity providers
  - Test certificates generation
  - Rate limit exemptions
  - Data reset capability

Mock Services:
  - Mock CA for certificates
  - Mock OAuth providers
  - Mock notification services
```

### 12.2 Developer Tools
**API Development:**
```yaml
GraphQL Playground:
  - Schema introspection
  - Query history
  - Variable management
  - Subscription testing

API Documentation:
  - OpenAPI/Swagger UI
  - GraphQL schema docs
  - Code examples
  - SDK integration guides
```

## Implementation Priority

### Phase 1 (Months 1-3)
1. mTLS certificate validation
2. Request signing verification
3. Basic multi-tenancy
4. Session management
5. Core credential operations

### Phase 2 (Months 4-6)
1. Bulk operations
2. Advanced caching
3. Monitoring infrastructure
4. WebSocket support
5. Webhook notifications

### Phase 3 (Months 7-9)
1. Zero-knowledge proofs
2. Advanced audit logging
3. High availability setup
4. Performance optimization
5. Compliance validation

### Phase 4 (Months 10-12)
1. Developer tools
2. Testing infrastructure
3. Documentation
4. Production hardening
5. Security audit

## Cost Estimation

### Infrastructure Costs (Monthly)
```yaml
Compute:
  - API Servers (8x m5.2xlarge): $2,500
  - Database (RDS Multi-AZ): $3,000
  - Redis Cluster: $1,500
  - Load Balancer: $500

Storage:
  - Database Storage (10TB): $1,000
  - Backup Storage (20TB): $500
  - Log Storage (5TB): $250

Network:
  - Data Transfer: $2,000
  - CDN: $1,000

Monitoring:
  - APM Tools: $1,500
  - Log Management: $1,000

Total: ~$14,750/month
```

## Risk Mitigation

### Security Risks
- Regular penetration testing
- Automated vulnerability scanning
- Security incident response plan
- Data encryption at rest and in transit

### Operational Risks
- Disaster recovery procedures
- Runbook automation
- On-call rotation
- Capacity planning

### Compliance Risks
- Regular compliance audits
- Data residency controls
- Privacy impact assessments
- Consent management

---
*Document Version: 1.0*
*Last Updated: September 2025*
*Status: Requirements Defined*