# NumbatWallet Database Migrations

## Protection Model Implementation

The unified data protection model implements the following database changes:

### JSONB Storage for Protected Fields

All fields marked with `DataClassification` attributes at OfficialSensitive or higher are stored as JSONB in PostgreSQL with the following structure:

```json
{
  "value": "plaintext_or_null",
  "encrypted": {
    "cipherText": "base64_encrypted_data",
    "keyId": "key_identifier",
    "algorithm": "AES-256-GCM",
    "encryptedAt": "2025-09-17T10:30:00Z"
  },
  "redacted": "J***n",
  "classification": "OfficialSensitive"
}
```

### Affected Tables and Columns

1. **Persons Table**
   - `FirstName` → JSONB
   - `LastName` → JSONB
   - `DateOfBirth` → JSONB
   - `Email` → JSONB
   - `PhoneNumber` → JSONB

2. **Credentials Table**
   - `CredentialData` → JSONB (already was JSONB)

3. **Issuers Table**
   - All fields remain as-is (Official classification only)

### Search Token Tables

For searchable protected fields, HMAC tokens are stored:

```sql
CREATE TABLE search_tokens (
    id UUID PRIMARY KEY,
    entity_type VARCHAR(128) NOT NULL,
    entity_id UUID NOT NULL,
    field_name VARCHAR(128) NOT NULL,
    token_hash VARCHAR(256) NOT NULL,
    search_strategy VARCHAR(50) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    INDEX idx_search_tokens (token_hash, entity_type, field_name)
);
```

### Audit Tables

Comprehensive audit logging for data access:

```sql
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY,
    entity_type VARCHAR(128) NOT NULL,
    entity_id VARCHAR(256) NOT NULL,
    action VARCHAR(50) NOT NULL,
    user_id VARCHAR(256) NOT NULL,
    tenant_id UUID NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL,
    max_classification VARCHAR(50) NOT NULL,
    changed_fields JSONB,
    ip_address VARCHAR(45),
    user_agent VARCHAR(512),
    INDEX idx_audit_entity (entity_type, entity_id, timestamp),
    INDEX idx_audit_user (user_id, timestamp),
    INDEX idx_audit_tenant (tenant_id, timestamp)
);

CREATE TABLE unmask_audit_logs (
    id UUID PRIMARY KEY,
    entity_type VARCHAR(128) NOT NULL,
    entity_id VARCHAR(256) NOT NULL,
    field_name VARCHAR(128) NOT NULL,
    classification VARCHAR(50) NOT NULL,
    reason TEXT NOT NULL,
    user_id VARCHAR(256) NOT NULL,
    tenant_id UUID NOT NULL,
    unmasked_at TIMESTAMPTZ NOT NULL,
    duration_seconds INT NOT NULL,
    required_mfa BOOLEAN NOT NULL,
    approved_by VARCHAR(256),
    INDEX idx_unmask_user (user_id, unmasked_at),
    INDEX idx_unmask_entity (entity_type, entity_id, unmasked_at)
);
```

### Tenant Security Policies

Tenant-specific protection configurations:

```sql
CREATE TABLE tenant_security_policies (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL UNIQUE,
    tenant_name VARCHAR(256) NOT NULL,
    policy_data JSONB NOT NULL,
    effective_from TIMESTAMPTZ NOT NULL,
    effective_to TIMESTAMPTZ,
    version INT NOT NULL DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    INDEX idx_policy_tenant (tenant_id, effective_from)
);
```

## Migration Commands

```bash
# Add migration
dotnet ef migrations add InitialProtectionModel -c NumbatWalletDbContext -p src/NumbatWallet.Infrastructure -s src/NumbatWallet.Web.Api

# Update database
dotnet ef database update -c NumbatWalletDbContext -p src/NumbatWallet.Infrastructure -s src/NumbatWallet.Web.Api

# Generate SQL script
dotnet ef migrations script -c NumbatWalletDbContext -p src/NumbatWallet.Infrastructure -s src/NumbatWallet.Web.Api -o migration.sql
```

## Rollback Considerations

1. **Data Recovery**: Before migrating, ensure backups exist as JSONB conversion is one-way
2. **Search Tokens**: Can be regenerated if lost
3. **Audit Logs**: Should be preserved separately before any rollback
4. **Unmask Sessions**: Will be invalidated on rollback

## Performance Considerations

1. **JSONB Indexes**: PostgreSQL GIN indexes on JSONB columns for query performance
2. **Search Tokens**: Indexed on token_hash for fast lookups
3. **Audit Tables**: Partitioned by month for large datasets
4. **Unmask Cache**: Redis-based for session management (not in database)