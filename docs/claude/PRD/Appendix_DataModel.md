# Appendix: Data Model
## Digital Wallet and Verifiable Credentials Solution

**Document Version:** 1.0  
**Parent Document:** [Master PRD](./PRD_Master.md)  
**Last Updated:** December 2024

---

## 1. Data Architecture Overview

### 1.1 High-Level Data Architecture

```mermaid
graph TB
    subgraph "Data Sources"
        USER[User Data]
        CRED[Credentials]
        ISSUER_DATA[Issuer Data]
        VERIFY[Verification Data]
        AUDIT[Audit Data]
    end
    
    subgraph "Data Storage Layers"
        HOT[(Hot Storage<br/>PostgreSQL)]
        WARM[(Warm Storage<br/>Redis Cache)]
        COLD[(Cold Storage<br/>Blob Storage)]
        ARCHIVE[(Archive<br/>Long-term)]
    end
    
    subgraph "Data Processing"
        ETL[ETL Pipeline]
        STREAM[Stream Processing]
        BATCH[Batch Processing]
    end
    
    subgraph "Data Services"
        API_DATA[API Layer]
        ANALYTICS[Analytics]
        REPORTING[Reporting]
    end
    
    USER --> HOT
    CRED --> HOT
    ISSUER_DATA --> HOT
    VERIFY --> WARM
    AUDIT --> COLD
    
    HOT --> ETL
    WARM --> STREAM
    COLD --> BATCH
    
    ETL --> API_DATA
    STREAM --> ANALYTICS
    BATCH --> REPORTING
```

### 1.2 Multi-Tenant Data Isolation

```mermaid
graph TD
    subgraph "Option A: Database per Tenant"
        TENANT1_DB[(Tenant 1 Database<br/>wallet_tenant1)]
        TENANT2_DB[(Tenant 2 Database<br/>wallet_tenant2)]
        TENANTN_DB[(Tenant N Database<br/>wallet_tenantN)]
    end
    
    subgraph "Option B: Shared Database with RLS"
        SHARED_DB[(Shared Database)]
        RLS[Row Level Security]
        TENANT_COL[tenant_id column]
    end
    
    subgraph "Shared Infrastructure"
        SHARED_TRUST[(Trust Registry<br/>Shared)]
        SHARED_CONFIG[(Configuration<br/>Shared)]
        SHARED_AUDIT[(Audit Log<br/>Partitioned)]
    end
    
    TENANT1_DB --> SHARED_TRUST
    TENANT2_DB --> SHARED_TRUST
    TENANTN_DB --> SHARED_TRUST
    
    SHARED_DB --> RLS
    RLS --> TENANT_COL
    SHARED_DB --> SHARED_TRUST
```

---

## 2. Core Data Models

### 2.1 Entity Relationship Diagram

```mermaid
erDiagram
    TENANT ||--o{ ORGANIZATION : contains
    ORGANIZATION ||--o{ USER : has
    USER ||--o{ WALLET : owns
    WALLET ||--o{ DID : manages
    WALLET ||--o{ CREDENTIAL : stores
    WALLET ||--o{ KEY_PAIR : contains
    
    CREDENTIAL ||--|| CREDENTIAL_TYPE : is_of
    CREDENTIAL ||--|| ISSUER : issued_by
    CREDENTIAL ||--o{ CREDENTIAL_STATUS : has
    CREDENTIAL ||--o{ PRESENTATION : used_in
    
    PRESENTATION ||--|| VERIFIER : verified_by
    PRESENTATION ||--o{ PRESENTATION_PROOF : contains
    
    DID ||--o{ DID_DOCUMENT : resolves_to
    DID ||--o{ SERVICE_ENDPOINT : has
    
    KEY_PAIR ||--|| KEY_METADATA : described_by
    KEY_PAIR ||--|| HSM_KEY : backed_by
    
    ISSUER ||--|| TRUST_REGISTRY : registered_in
    VERIFIER ||--|| TRUST_REGISTRY : registered_in
    
    USER ||--o{ AUDIT_LOG : generates
    CREDENTIAL ||--o{ AUDIT_LOG : tracked_in
    PRESENTATION ||--o{ AUDIT_LOG : logged_in
```

### 2.2 Tenant Model

```mermaid
classDiagram
    class Tenant {
        +UUID id
        +String name
        +String display_name
        +TenantStatus status
        +JSON configuration
        +DateTime created_at
        +DateTime updated_at
        +String created_by
        +String database_name
        +String connection_string
        +SubscriptionTier tier
        +TenantLimits limits
    }
    
    class TenantLimits {
        +Int max_users
        +Int max_wallets
        +Int max_credentials
        +Int max_storage_gb
        +Int max_api_calls_per_month
        +Int max_verifications_per_month
    }
    
    class TenantConfiguration {
        +FeatureFlags features
        +BrandingConfig branding
        +SecurityConfig security
        +IntegrationConfig integrations
        +NotificationConfig notifications
    }
    
    class Organization {
        +UUID id
        +UUID tenant_id
        +String name
        +OrganizationType type
        +JSON metadata
        +Boolean is_active
    }
    
    Tenant "1" --> "*" Organization
    Tenant --> TenantLimits
    Tenant --> TenantConfiguration
```

### 2.3 User and Wallet Model

```mermaid
classDiagram
    class User {
        +UUID id
        +UUID tenant_id
        +UUID organization_id
        +String external_id
        +String email
        +String phone_number
        +UserStatus status
        +JSON profile
        +DateTime created_at
        +DateTime last_login
        +String[] roles
        +JSON preferences
    }
    
    class Wallet {
        +UUID id
        +UUID user_id
        +UUID tenant_id
        +String wallet_did
        +WalletType type
        +WalletStatus status
        +JSON metadata
        +DateTime created_at
        +DateTime updated_at
        +EncryptedData backup_data
        +RecoveryMethod recovery_method
    }
    
    class DID {
        +UUID id
        +UUID wallet_id
        +String did_string
        +DIDMethod method
        +JSON did_document
        +DIDStatus status
        +DateTime created_at
        +DateTime deactivated_at
    }
    
    class KeyPair {
        +UUID id
        +UUID wallet_id
        +String key_id
        +KeyType type
        +KeyPurpose purpose
        +String algorithm
        +String public_key
        +EncryptedData private_key_ref
        +DateTime created_at
        +DateTime expires_at
        +Boolean is_revoked
    }
    
    User "1" --> "*" Wallet
    Wallet "1" --> "*" DID
    Wallet "1" --> "*" KeyPair
```

---

## 3. Credential Data Model

### 3.1 Credential Structure

```mermaid
classDiagram
    class Credential {
        +UUID id
        +UUID wallet_id
        +String credential_id
        +CredentialType type
        +CredentialFormat format
        +JSON credential_subject
        +String issuer_did
        +DateTime issuance_date
        +DateTime expiration_date
        +JSON proof
        +CredentialStatus status
        +JSON metadata
        +String schema_id
        +String revocation_list_index
    }
    
    class CredentialType {
        +UUID id
        +String type_name
        +String schema_url
        +JSON schema_definition
        +String[] contexts
        +ValidationRules rules
        +Boolean is_active
    }
    
    class CredentialStatus {
        +UUID id
        +UUID credential_id
        +StatusType status
        +String status_list_url
        +Int status_list_index
        +DateTime updated_at
        +String updated_by
        +String reason
    }
    
    class CredentialSchema {
        +UUID id
        +String schema_id
        +String name
        +String version
        +JSON json_schema
        +String[] required_fields
        +JSON validation_rules
    }
    
    Credential "*" --> "1" CredentialType
    Credential "1" --> "*" CredentialStatus
    Credential "*" --> "1" CredentialSchema
```

### 3.2 Presentation and Verification Model

```mermaid
classDiagram
    class PresentationRequest {
        +UUID id
        +UUID verifier_id
        +String request_id
        +JSON requested_credentials
        +String[] required_fields
        +JSON constraints
        +String challenge
        +String domain
        +DateTime created_at
        +DateTime expires_at
    }
    
    class Presentation {
        +UUID id
        +UUID wallet_id
        +UUID request_id
        +String presentation_id
        +JSON verifiable_presentation
        +String[] credential_ids
        +DateTime presented_at
        +String holder_did
    }
    
    class PresentationProof {
        +UUID id
        +UUID presentation_id
        +ProofType type
        +String verification_method
        +JSON proof_value
        +DateTime created
        +String challenge
        +String domain
    }
    
    class VerificationResult {
        +UUID id
        +UUID presentation_id
        +UUID verifier_id
        +Boolean is_valid
        +JSON verification_checks
        +String[] errors
        +String[] warnings
        +DateTime verified_at
        +JSON metadata
    }
    
    PresentationRequest "1" --> "*" Presentation
    Presentation "1" --> "*" PresentationProof
    Presentation "1" --> "1" VerificationResult
```

---

## 4. Trust Registry Model

### 4.1 Trust Registry Structure

```mermaid
classDiagram
    class TrustRegistry {
        +UUID id
        +String name
        +String version
        +JSON governance_framework
        +DateTime created_at
        +DateTime updated_at
        +String maintained_by
    }
    
    class TrustAnchor {
        +UUID id
        +UUID registry_id
        +String did
        +EntityType entity_type
        +String name
        +String[] roles
        +JSON public_keys
        +JSON service_endpoints
        +TrustLevel trust_level
        +DateTime registered_at
        +DateTime expires_at
        +Boolean is_active
    }
    
    class TrustChain {
        +UUID id
        +UUID root_anchor_id
        +UUID[] chain_path
        +Int chain_depth
        +DateTime validated_at
        +Boolean is_valid
    }
    
    class RevocationRegistry {
        +UUID id
        +UUID issuer_id
        +String registry_url
        +RevocationType type
        +Int max_entries
        +Int current_index
        +JSON accumulator
        +DateTime created_at
        +DateTime last_updated
    }
    
    TrustRegistry "1" --> "*" TrustAnchor
    TrustAnchor "1" --> "*" TrustChain
    TrustAnchor "1" --> "*" RevocationRegistry
```

### 4.2 Issuer and Verifier Model

```mermaid
classDiagram
    class Issuer {
        +UUID id
        +UUID tenant_id
        +String issuer_did
        +String name
        +IssuerType type
        +String[] credential_types
        +JSON issuer_config
        +JSON public_keys
        +String[] service_endpoints
        +Boolean is_active
        +DateTime registered_at
    }
    
    class Verifier {
        +UUID id
        +UUID tenant_id
        +String verifier_did
        +String name
        +VerifierType type
        +String[] accepted_credentials
        +JSON verification_policies
        +Boolean is_active
        +DateTime registered_at
    }
    
    class IssuancePolicy {
        +UUID id
        +UUID issuer_id
        +String policy_name
        +JSON eligibility_criteria
        +JSON required_evidence
        +JSON approval_workflow
        +Boolean requires_payment
        +Decimal fee_amount
    }
    
    class VerificationPolicy {
        +UUID id
        +UUID verifier_id
        +String policy_name
        +JSON acceptance_rules
        +String[] required_fields
        +Int max_credential_age_days
        +Boolean check_revocation
        +JSON trust_list
    }
    
    Issuer "1" --> "*" IssuancePolicy
    Verifier "1" --> "*" VerificationPolicy
```

---

## 5. Audit and Compliance Model

### 5.1 Audit Log Structure

```mermaid
classDiagram
    class AuditLog {
        +UUID id
        +UUID tenant_id
        +UUID user_id
        +String event_type
        +String resource_type
        +UUID resource_id
        +String action
        +JSON before_state
        +JSON after_state
        +String ip_address
        +String user_agent
        +DateTime timestamp
        +JSON metadata
        +String signature
    }
    
    class DataAccessLog {
        +UUID id
        +UUID audit_log_id
        +String data_classification
        +String[] fields_accessed
        +AccessPurpose purpose
        +String legal_basis
        +Boolean user_consent
        +DateTime accessed_at
    }
    
    class SecurityEvent {
        +UUID id
        +UUID tenant_id
        +EventSeverity severity
        +String event_category
        +String description
        +JSON event_details
        +String source_ip
        +DateTime detected_at
        +String detection_method
        +ResponseAction action_taken
    }
    
    class ComplianceRecord {
        +UUID id
        +UUID tenant_id
        +String regulation
        +String requirement
        +ComplianceStatus status
        +JSON evidence
        +DateTime assessed_at
        +String assessed_by
        +DateTime next_review
    }
    
    AuditLog "1" --> "*" DataAccessLog
    AuditLog "1" --> "*" SecurityEvent
```

---

## 6. Database Schema Design

### 6.1 Core Tables (PostgreSQL)

```sql
-- Tenant Management
CREATE TABLE tenants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) UNIQUE NOT NULL,
    display_name VARCHAR(200) NOT NULL,
    status VARCHAR(20) NOT NULL CHECK (status IN ('active', 'suspended', 'disabled')),
    configuration JSONB NOT NULL DEFAULT '{}',
    database_name VARCHAR(63) UNIQUE NOT NULL,
    connection_string TEXT ENCRYPTED,
    subscription_tier VARCHAR(20) NOT NULL,
    limits JSONB NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    created_by VARCHAR(200)
);

-- Users
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    organization_id UUID,
    external_id VARCHAR(200),
    email VARCHAR(320) ENCRYPTED,
    phone_number VARCHAR(50) ENCRYPTED,
    status VARCHAR(20) NOT NULL,
    profile JSONB ENCRYPTED,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    last_login TIMESTAMPTZ,
    roles TEXT[],
    preferences JSONB,
    UNIQUE(tenant_id, email),
    INDEX idx_users_tenant (tenant_id),
    INDEX idx_users_external (external_id)
);

-- Wallets
CREATE TABLE wallets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    wallet_did VARCHAR(500) UNIQUE,
    type VARCHAR(20) NOT NULL,
    status VARCHAR(20) NOT NULL,
    metadata JSONB,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    backup_data BYTEA ENCRYPTED,
    recovery_method VARCHAR(50),
    INDEX idx_wallets_user (user_id),
    INDEX idx_wallets_did (wallet_did)
);

-- Credentials
CREATE TABLE credentials (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    wallet_id UUID NOT NULL REFERENCES wallets(id),
    credential_id VARCHAR(500) UNIQUE NOT NULL,
    type VARCHAR(100) NOT NULL,
    format VARCHAR(20) NOT NULL,
    credential_subject JSONB ENCRYPTED NOT NULL,
    issuer_did VARCHAR(500) NOT NULL,
    issuance_date TIMESTAMPTZ NOT NULL,
    expiration_date TIMESTAMPTZ,
    proof JSONB NOT NULL,
    status VARCHAR(20) NOT NULL,
    metadata JSONB,
    schema_id VARCHAR(200),
    revocation_list_index INTEGER,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    INDEX idx_credentials_wallet (wallet_id),
    INDEX idx_credentials_type (type),
    INDEX idx_credentials_issuer (issuer_did),
    INDEX idx_credentials_status (status)
);
```

### 6.2 Audit and Security Tables

```sql
-- Audit Logs (Append-only)
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    user_id UUID,
    event_type VARCHAR(50) NOT NULL,
    resource_type VARCHAR(50) NOT NULL,
    resource_id UUID,
    action VARCHAR(50) NOT NULL,
    before_state JSONB,
    after_state JSONB,
    ip_address INET,
    user_agent TEXT,
    timestamp TIMESTAMPTZ DEFAULT NOW() NOT NULL,
    metadata JSONB,
    signature TEXT NOT NULL,
    INDEX idx_audit_tenant_time (tenant_id, timestamp DESC),
    INDEX idx_audit_user (user_id),
    INDEX idx_audit_resource (resource_type, resource_id)
) PARTITION BY RANGE (timestamp);

-- Create monthly partitions
CREATE TABLE audit_logs_2025_01 PARTITION OF audit_logs
    FOR VALUES FROM ('2025-01-01') TO ('2025-02-01');

-- Key Management
CREATE TABLE key_pairs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    wallet_id UUID NOT NULL REFERENCES wallets(id),
    key_id VARCHAR(200) UNIQUE NOT NULL,
    type VARCHAR(20) NOT NULL,
    purpose VARCHAR(50) NOT NULL,
    algorithm VARCHAR(50) NOT NULL,
    public_key TEXT NOT NULL,
    private_key_ref VARCHAR(500) ENCRYPTED NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    expires_at TIMESTAMPTZ,
    is_revoked BOOLEAN DEFAULT FALSE,
    revoked_at TIMESTAMPTZ,
    INDEX idx_keys_wallet (wallet_id),
    INDEX idx_keys_expiry (expires_at)
);

-- Session Management
CREATE TABLE sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id),
    token_hash VARCHAR(64) UNIQUE NOT NULL,
    device_id VARCHAR(200),
    ip_address INET,
    user_agent TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL,
    last_activity TIMESTAMPTZ DEFAULT NOW(),
    is_active BOOLEAN DEFAULT TRUE,
    INDEX idx_sessions_user (user_id),
    INDEX idx_sessions_token (token_hash),
    INDEX idx_sessions_expiry (expires_at)
);
```

---

## 7. Data Access Patterns

### 7.1 Query Optimization Strategy

```mermaid
graph TD
    subgraph "Read Patterns"
        READ_WALLET[Get Wallet by ID]
        READ_CREDS[List Credentials]
        READ_VERIFY[Verify Credential]
        READ_STATUS[Check Status]
    end
    
    subgraph "Write Patterns"
        WRITE_ISSUE[Issue Credential]
        WRITE_REVOKE[Revoke Credential]
        WRITE_PRESENT[Create Presentation]
        WRITE_AUDIT[Log Event]
    end
    
    subgraph "Optimization"
        INDEX[Indexes]
        PARTITION[Partitioning]
        CACHE_OPT[Caching]
        DENORM[Denormalization]
    end
    
    READ_WALLET --> INDEX
    READ_CREDS --> CACHE_OPT
    READ_VERIFY --> INDEX
    READ_STATUS --> CACHE_OPT
    
    WRITE_ISSUE --> PARTITION
    WRITE_REVOKE --> INDEX
    WRITE_PRESENT --> DENORM
    WRITE_AUDIT --> PARTITION
```

### 7.2 Caching Strategy

```mermaid
classDiagram
    class CacheLayer {
        +Redis Cluster
        +TTL Management
        +Invalidation Strategy
    }
    
    class L1Cache {
        <<Application Memory>>
        +User Sessions: 5 min
        +Trust Registry: 1 hour
        +Schemas: 24 hours
    }
    
    class L2Cache {
        <<Redis>>
        +Credentials: 15 min
        +Verification Results: 5 min
        +Status Lists: 10 min
    }
    
    class L3Cache {
        <<Database>>
        +Query Result Cache
        +Materialized Views
        +Prepared Statements
    }
    
    CacheLayer --> L1Cache
    CacheLayer --> L2Cache
    CacheLayer --> L3Cache
```

---

## 8. Data Migration Strategy

### 8.1 Migration Path: Per-Tenant to Shared DB

```mermaid
graph LR
    subgraph "Phase 1: Analysis"
        ANALYZE[Analyze Data]
        SIZE[Size Assessment]
        DEPS[Dependencies]
    end
    
    subgraph "Phase 2: Preparation"
        SCHEMA[Schema Design]
        RLS_DESIGN[RLS Rules]
        SCRIPT[Migration Scripts]
    end
    
    subgraph "Phase 3: Migration"
        PILOT_MIG[Pilot Migration]
        VALIDATE_MIG[Validation]
        PROD_MIG[Production Migration]
    end
    
    subgraph "Phase 4: Cutover"
        DUAL_WRITE[Dual Write]
        VERIFY_DATA[Verify Data]
        SWITCH[Switch Over]
    end
    
    ANALYZE --> SIZE
    SIZE --> DEPS
    DEPS --> SCHEMA
    SCHEMA --> RLS_DESIGN
    RLS_DESIGN --> SCRIPT
    SCRIPT --> PILOT_MIG
    PILOT_MIG --> VALIDATE_MIG
    VALIDATE_MIG --> PROD_MIG
    PROD_MIG --> DUAL_WRITE
    DUAL_WRITE --> VERIFY_DATA
    VERIFY_DATA --> SWITCH
```

### 8.2 Row-Level Security Implementation

```sql
-- Enable RLS on shared tables
ALTER TABLE credentials ENABLE ROW LEVEL SECURITY;

-- Create tenant isolation policy
CREATE POLICY tenant_isolation ON credentials
    FOR ALL
    TO application_role
    USING (tenant_id = current_setting('app.current_tenant')::UUID);

-- Create read policy for verifiers
CREATE POLICY verifier_read ON credentials
    FOR SELECT
    TO verifier_role
    USING (
        status = 'active' 
        AND issuer_did IN (
            SELECT did FROM trust_registry 
            WHERE is_active = true
        )
    );

-- Function to set current tenant
CREATE FUNCTION set_current_tenant(tenant UUID)
RETURNS void AS $$
BEGIN
    PERFORM set_config('app.current_tenant', tenant::text, false);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
```

---

## 9. Data Lifecycle Management

### 9.1 Data Retention Policies

```mermaid
graph TD
    subgraph "Active Data"
        ACTIVE[Active Credentials<br/>Online Storage]
        SESSION[Sessions<br/>Memory/Redis]
        CURRENT[Current Logs<br/>Hot Storage]
    end
    
    subgraph "Archive Tiers"
        WARM_TIER[Warm Archive<br/>90 days]
        COOL_TIER[Cool Archive<br/>1 year]
        COLD_TIER[Cold Archive<br/>7 years]
    end
    
    subgraph "Deletion"
        SOFT_DEL[Soft Delete<br/>Mark inactive]
        HARD_DEL[Hard Delete<br/>Permanent removal]
        CRYPTO_DEL[Crypto Deletion<br/>Key destruction]
    end
    
    ACTIVE --> WARM_TIER
    SESSION --> SOFT_DEL
    CURRENT --> WARM_TIER
    
    WARM_TIER --> COOL_TIER
    COOL_TIER --> COLD_TIER
    
    COLD_TIER --> HARD_DEL
    HARD_DEL --> CRYPTO_DEL
```

### 9.2 Data Classification

| Classification | Description | Retention | Encryption | Access Control |
|---------------|-------------|-----------|------------|----------------|
| **Restricted** | Cryptographic keys, PII | Until revoked | HSM + AES-256 | Need-to-know |
| **Sensitive** | Credentials, personal data | 7 years | AES-256 | Role-based |
| **Internal** | Audit logs, metadata | 3 years | AES-256 | Authenticated |
| **Public** | Schemas, public keys | Indefinite | TLS only | Public read |

---

## 10. Performance Considerations

### 10.1 Index Strategy

```sql
-- Primary lookup indexes
CREATE INDEX idx_credentials_wallet_type 
    ON credentials(wallet_id, type) 
    WHERE status = 'active';

CREATE INDEX idx_credentials_issuer_date 
    ON credentials(issuer_did, issuance_date DESC);

-- Partial indexes for common queries
CREATE INDEX idx_active_credentials 
    ON credentials(wallet_id) 
    WHERE status = 'active' AND expiration_date > NOW();

-- JSONB indexes
CREATE INDEX idx_credential_subject 
    ON credentials USING GIN (credential_subject);

CREATE INDEX idx_metadata_search 
    ON credentials USING GIN (metadata);

-- Full-text search
CREATE INDEX idx_audit_search 
    ON audit_logs USING GIN (
        to_tsvector('english', 
            event_type || ' ' || 
            resource_type || ' ' || 
            action)
    );
```

### 10.2 Query Performance Patterns

```mermaid
graph LR
    subgraph "Optimization Techniques"
        PREPARED[Prepared Statements]
        BATCH_Q[Batch Queries]
        ASYNC_Q[Async Processing]
        CONN_POOL[Connection Pooling]
    end
    
    subgraph "Monitoring"
        SLOW_LOG[Slow Query Log]
        EXPLAIN[EXPLAIN ANALYZE]
        STATS[pg_stat_statements]
        METRICS[Performance Metrics]
    end
    
    subgraph "Tuning"
        VACUUM_OPT[Auto-vacuum]
        ANALYZE_OPT[Statistics]
        REINDEX[Reindexing]
        PARTITION_OPT[Partitioning]
    end
    
    PREPARED --> SLOW_LOG
    BATCH_Q --> EXPLAIN
    ASYNC_Q --> STATS
    CONN_POOL --> METRICS
    
    SLOW_LOG --> VACUUM_OPT
    EXPLAIN --> ANALYZE_OPT
    STATS --> REINDEX
    METRICS --> PARTITION_OPT
```

---

## 11. Backup and Recovery

### 11.1 Backup Strategy

```mermaid
graph TD
    subgraph "Backup Types"
        FULL[Full Backup<br/>Weekly]
        INCR[Incremental<br/>Daily]
        TRANS[Transaction Log<br/>Continuous]
        SNAP[Snapshots<br/>4-hourly]
    end
    
    subgraph "Storage Locations"
        LOCAL[Local Storage<br/>Fast recovery]
        REGIONAL[Regional Storage<br/>Same region]
        GEO[Geo-redundant<br/>Cross-region]
    end
    
    subgraph "Recovery Scenarios"
        POINT[Point-in-Time]
        CORRUPT_REC[Corruption]
        DISASTER_REC[Disaster]
        MIGRATION[Migration]
    end
    
    FULL --> LOCAL
    INCR --> LOCAL
    TRANS --> REGIONAL
    SNAP --> GEO
    
    LOCAL --> POINT
    REGIONAL --> CORRUPT_REC
    GEO --> DISASTER_REC
    GEO --> MIGRATION
```

### 11.2 Recovery Procedures

| Scenario | RTO | RPO | Method | Steps |
|----------|-----|-----|--------|-------|
| Data corruption | 1 hour | 15 min | Point-in-time restore | 1. Identify corruption time<br/>2. Restore from snapshot<br/>3. Apply transaction logs<br/>4. Validate data |
| Database failure | 30 min | 5 min | Failover to replica | 1. Detect failure<br/>2. Promote read replica<br/>3. Update connection strings<br/>4. Verify operations |
| Region failure | 2 hours | 30 min | Cross-region failover | 1. Activate DR site<br/>2. Restore from geo-backup<br/>3. Update DNS<br/>4. Full validation |
| Complete loss | 24 hours | 1 hour | Full restore | 1. Provision infrastructure<br/>2. Restore from backup<br/>3. Rebuild indexes<br/>4. Full testing |

---

## 12. Data Governance

### 12.1 Data Quality Framework

```mermaid
graph TD
    subgraph "Quality Dimensions"
        COMPLETE[Completeness]
        ACCURATE[Accuracy]
        CONSISTENT[Consistency]
        TIMELY[Timeliness]
        VALID[Validity]
    end
    
    subgraph "Quality Controls"
        VALIDATION[Input Validation]
        CLEANSING[Data Cleansing]
        DEDUP[Deduplication]
        STANDARD[Standardization]
    end
    
    subgraph "Monitoring"
        PROFILE[Data Profiling]
        QUALITY_METRICS[Quality Metrics]
        ALERTS[Quality Alerts]
        REPORTS[Quality Reports]
    end
    
    COMPLETE --> VALIDATION
    ACCURATE --> CLEANSING
    CONSISTENT --> DEDUP
    TIMELY --> STANDARD
    
    VALIDATION --> PROFILE
    CLEANSING --> QUALITY_METRICS
    DEDUP --> ALERTS
    STANDARD --> REPORTS
```

### 12.2 Data Stewardship

| Role | Responsibilities | Scope |
|------|-----------------|--------|
| **Data Owner** | Policy, access approval | Business data |
| **Data Steward** | Quality, compliance | Domain data |
| **Data Custodian** | Technical implementation | All data |
| **Data Architect** | Design, standards | Data models |
| **Privacy Officer** | Privacy compliance | Personal data |

---

## Database Maintenance

### Maintenance Schedule

| Task | Frequency | Impact | Duration |
|------|-----------|--------|----------|
| Auto-vacuum | Continuous | None | Ongoing |
| Statistics update | Daily | Minimal | 5 min |
| Index rebuild | Weekly | Low | 30 min |
| Full backup | Weekly | Low | 2 hours |
| Partition maintenance | Monthly | Low | 1 hour |
| Major version upgrade | Yearly | High | 4 hours |

### Performance Tuning Parameters

```sql
-- PostgreSQL configuration
shared_buffers = '8GB'
effective_cache_size = '24GB'
work_mem = '64MB'
maintenance_work_mem = '2GB'
max_connections = 200
max_parallel_workers = 8
wal_buffers = '16MB'
checkpoint_completion_target = 0.9
random_page_cost = 1.1  -- SSD storage
```

---

**END OF DATA MODEL APPENDIX**