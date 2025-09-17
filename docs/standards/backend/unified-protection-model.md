# Unified Data Protection Model Implementation

## Overview

The unified data protection model provides a comprehensive approach to data security that separates classification (what data is) from protection (how it's secured). This enables flexible, tenant-specific security policies while maintaining consistent data classification across the system.

## Core Principles

1. **Classification Drives Display**: Data classification determines how information is displayed (redacted, masked, etc.)
2. **Policy Drives Protection**: Tenant policies determine if/how data is encrypted
3. **Everything Redacted by Default**: All classified data appears redacted until explicitly unmasked
4. **Search Works Everywhere**: HMAC-based search tokens work regardless of encryption status
5. **Audit Everything**: Comprehensive logging of all access to classified data

## Architecture Components

### 1. Domain Layer (Classification)

Located in: `NumbatWallet.SharedKernel` and `NumbatWallet.Domain`

```csharp
// Data classification aligned with Australian government standards
public enum DataClassification
{
    Unofficial = 0,        // Public information
    Official = 1,          // Normal business data
    OfficialSensitive = 2, // Sensitive business/personal data
    Protected = 3,         // Highly sensitive requiring protection
    Secret = 4,            // Classified government data
    TopSecret = 5          // Highest classification
}

// Attribute for marking properties
[DataClassification(DataClassification.OfficialSensitive, "Identity")]
public string FirstName { get; private set; }
```

### 2. Application Layer (Interfaces)

Located in: `NumbatWallet.Application/Interfaces`

Key interfaces:
- `IProtectionService`: Core protection/unprotection operations
- `ISearchTokenService`: HMAC-based search token generation
- `ITenantPolicyService`: Tenant-specific security policies
- `IUnmaskCacheService`: Time-limited unmask sessions
- `IAuditService`: Comprehensive audit logging

### 3. Infrastructure Layer (Implementation)

Located in: `NumbatWallet.Infrastructure/Data`

#### Value Converters
- `ProtectedFieldConverter`: Converts strings to/from JSONB protected format

#### Interceptors
- `ProtectionInterceptor`: Applies protection on save based on policies
- `AuditInterceptor`: Logs all access to classified data

#### JSONB Structure
```json
{
  "value": "plaintext_or_null",
  "encrypted": {
    "cipherText": "base64_data",
    "keyId": "key_id",
    "algorithm": "AES-256-GCM",
    "encryptedAt": "2025-09-17T10:00:00Z"
  },
  "redacted": "J***n",
  "classification": "OfficialSensitive"
}
```

## Implementation Status

### ✅ Completed

1. **SharedKernel Enhancements**
   - DataClassification enum
   - DataClassificationAttribute
   - Protection-related enums (RedactionPattern, SearchStrategy, etc.)

2. **Domain Updates**
   - Added classification attributes to all entities
   - Person, Wallet, Credential, Issuer marked appropriately

3. **Application Interfaces**
   - IProtectionService
   - ISearchTokenService
   - ITenantPolicyService
   - IUnmaskCacheService
   - IAuditService

4. **Infrastructure Components**
   - ProtectedFieldConverter for JSONB
   - Entity configurations updated for JSONB
   - Protection and Audit interceptors
   - DbContext configuration

### 🚧 In Progress

5. **Database Migrations**
   - Migration scripts for JSONB columns
   - Audit and search token tables
   - Tenant policy tables

### 📋 Pending

6. **Service Implementations**
   - Concrete implementations of protection services
   - Azure Key Vault integration
   - Redis cache for unmask sessions

7. **Integration Testing**
   - End-to-end protection scenarios
   - Multi-tenant isolation tests
   - Performance benchmarks

## Usage Examples

### 1. Saving Protected Data

```csharp
// Data is automatically protected based on classification and policy
var person = Person.Create("John", "Doe", email, phone);
await _context.Persons.AddAsync(person);
await _context.SaveChangesAsync(); // Interceptors apply protection
```

### 2. Searching Protected Data

```csharp
// Search works via tokens, not direct comparison
var searchToken = await _searchTokenService.GenerateTokenAsync(
    "john.doe@example.com",
    SearchStrategy.Exact);

var person = await _context.Persons
    .Where(p => p.EmailToken == searchToken)
    .FirstOrDefaultAsync();
```

### 3. Unmasking for Display

```csharp
// Create time-limited unmask session
var session = await _unmaskService.CreateSessionAsync(
    userId: currentUser.Id,
    entityIds: new[] { person.Id },
    fields: new[] { "FirstName", "LastName" },
    reason: "Customer service request #12345",
    ttlSeconds: 300);

// Get unmasked value during session
var firstName = await _unmaskService.GetUnmaskedValueAsync<string>(
    session.SessionId,
    person.Id.ToString(),
    "FirstName");
```

## Security Considerations

1. **Key Management**: Uses Azure Managed HSM for DEK/KEK management
2. **Audit Trail**: All access to sensitive data is logged
3. **Time-Limited Access**: Unmask sessions expire automatically
4. **MFA Requirements**: Can require MFA for high-classification unmasks
5. **Approval Workflow**: Supports approval for Protected+ classifications

## Performance Optimizations

1. **JSONB Indexes**: GIN indexes for efficient JSONB queries
2. **Search Token Cache**: In-memory cache for frequent searches
3. **Batch Protection**: Process multiple fields in single operation
4. **Async Operations**: All I/O operations are async
5. **Connection Pooling**: Efficient database connection management

## Compliance Alignment

- **Australian PSPF**: Protective Security Policy Framework compliance
- **ISM**: Information Security Manual alignment
- **Privacy Act 1988**: Australian privacy legislation
- **GDPR**: Article 32 technical measures
- **ISO 27001**: Information security management

## Migration Path

### Phase 1: Foundation (Completed)
- Core enums and attributes
- Domain model updates
- Interface definitions

### Phase 2: Infrastructure (In Progress)
- Value converters
- Entity configurations
- Database interceptors

### Phase 3: Implementation (Pending)
- Service implementations
- Azure integrations
- Caching layer

### Phase 4: Testing (Pending)
- Unit tests
- Integration tests
- Performance tests

## Best Practices

1. **Always classify data** at the domain level
2. **Never log sensitive values** in plain text
3. **Use search tokens** instead of direct comparisons
4. **Implement unmask auditing** for compliance
5. **Test multi-tenant isolation** thoroughly
6. **Monitor performance** of JSONB operations
7. **Rotate encryption keys** regularly
8. **Archive audit logs** according to retention policies

## Troubleshooting

### Common Issues

1. **Search not working**: Ensure search tokens are generated with same strategy
2. **Data appears corrupted**: Check if JSONB converter is applied
3. **Performance degradation**: Review JSONB indexes
4. **Unmask not working**: Verify session is active and user has permissions
5. **Audit gaps**: Check interceptor registration

## References

- [Australian Government PSPF](https://www.protectivesecurity.gov.au/)
- [Australian Government ISM](https://www.cyber.gov.au/ism)
- [PostgreSQL JSONB Documentation](https://www.postgresql.org/docs/current/datatype-json.html)
- [Entity Framework Core Interceptors](https://docs.microsoft.com/ef/core/logging-events-diagnostics/interceptors)
- [Azure Key Vault](https://docs.microsoft.com/azure/key-vault/)