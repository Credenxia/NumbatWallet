# NumbatWallet Infrastructure - Comprehensive Action Plan
*Generated: September 22, 2025*
*Version: 2.0 - Phased Implementation Approach*

## Executive Summary
This document contains the complete action plan for NumbatWallet infrastructure implementation using a phased approach that allows immediate development with progressive security enhancement.

## Context from Previous Analysis
- User clarified: Start with Azure Key Vault Premium → Managed HSM → Dedicated HSM (phased approach)
- External dependencies will be mocked for initial development
- System will be used for internal workforce management if WA tender not approved
- Focus on performance, security, and cost optimization
- SDK development happening in parallel - need to flag any impacts

## Phase 1: Immediate Actions (Week 1-2)
### Can be implemented NOW without external dependencies

### 1. Complete HSM Abstraction Layer
**Implementation Details:**

#### 1.1 Create IHsmProvider Interface
```csharp
namespace NumbatWallet.Domain.Interfaces;

public interface IHsmProvider
{
    string ProviderType { get; }
    bool SupportsHardwareBackedKeys { get; }
    Task<string> GenerateKeyAsync(KeyGenerationRequest request);
    Task<byte[]> SignAsync(string keyId, byte[] data, SigningAlgorithm algorithm);
    Task<bool> VerifyAsync(string keyId, byte[] data, byte[] signature, SigningAlgorithm algorithm);
    Task<byte[]> EncryptAsync(string keyId, byte[] plaintext);
    Task<byte[]> DecryptAsync(string keyId, byte[] ciphertext);
    Task<KeyBackupData> BackupKeyAsync(string keyId);
    Task RestoreKeyAsync(string keyId, KeyBackupData backup);
    Task<bool> MigrateToProvider(IHsmProvider targetProvider, string keyId);
}
```

#### 1.2 Implement Three Provider Backends

**SoftwareHsmProvider (Development/Testing)**
- Location: `src/NumbatWallet.Infrastructure/Services/Providers/SoftwareHsmProvider.cs`
- Uses: System.Security.Cryptography for all operations
- Key Storage: Encrypted file system with AES-256
- Purpose: Local development and testing

**KeyVaultHsmProvider (Production Phase 1)**
- Location: `src/NumbatWallet.Infrastructure/Services/Providers/KeyVaultHsmProvider.cs`
- Uses: Azure Key Vault Premium (HSM-backed)
- Key Storage: Azure Key Vault with soft-delete enabled
- Cost: ~$150/month

**ManagedHsmProvider (Production Phase 2)**
- Location: `src/NumbatWallet.Infrastructure/Services/Providers/ManagedHsmProvider.cs`
- Uses: Azure Key Vault Managed HSM
- Key Storage: FIPS 140-2 Level 2 compliant HSM
- Cost: ~$3,200/month

#### 1.3 Update HsmService to Use Providers
```csharp
public class HsmService : IHsmService
{
    private readonly IHsmProvider _provider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HsmService> _logger;

    public HsmService(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var providerType = configuration["Hsm:Provider"] ?? "Software";
        _provider = providerType switch
        {
            "Software" => serviceProvider.GetRequiredService<SoftwareHsmProvider>(),
            "KeyVault" => serviceProvider.GetRequiredService<KeyVaultHsmProvider>(),
            "ManagedHsm" => serviceProvider.GetRequiredService<ManagedHsmProvider>(),
            "DedicatedHsm" => serviceProvider.GetRequiredService<DedicatedHsmProvider>(),
            _ => throw new NotSupportedException($"HSM provider '{providerType}' not supported")
        };
    }
}
```

### 2. Mock External Dependencies

#### 2.1 MockIcaoService
- Location: `src/NumbatWallet.Infrastructure/Services/Mocks/MockIcaoService.cs`
- Provides: Simulated CSCA certificates, Master List, DSC validation
- Test Data: Generate self-signed certificates mimicking ICAO structure

#### 2.2 MockTrustListService
- Location: `src/NumbatWallet.Infrastructure/Services/Mocks/MockTrustListService.cs`
- Provides: Simulated government trust lists
- Test Data: JSON files with issuer/verifier configurations

#### 2.3 MockDocumentSigningService
- Location: `src/NumbatWallet.Infrastructure/Services/Mocks/MockDocumentSigningService.cs`
- Provides: DSC generation, document signing, verification
- Test Data: Self-signed certificates for testing

#### 2.4 MockRevocationService
- Location: `src/NumbatWallet.Infrastructure/Services/Mocks/MockRevocationService.cs`
- Provides: In-memory CRL, simulated OCSP responder
- Test Data: Configurable revocation lists

### 3. Complete Key Rotation Implementation

#### 3.1 Finish KeyRotationService
- Add grace period state machine
- Implement compliance reporting
- Add emergency rotation procedures
- Create rotation audit trail

#### 3.2 Rotation Configuration
```json
{
  "KeyRotation": {
    "Policies": {
      "SigningKeys": { "Days": 90, "GracePeriod": 7, "Warning": 14 },
      "EncryptionKeys": { "Days": 365, "GracePeriod": 30, "Warning": 60 },
      "TlsCertificates": { "Days": 30, "GracePeriod": 3, "Warning": 7 },
      "ApiKeys": { "Days": 180, "GracePeriod": 14, "Warning": 30 }
    },
    "AutoRotationEnabled": true,
    "ComplianceReportingEnabled": true
  }
}
```

## Phase 2: Azure Infrastructure (Week 2-3)
### Requires Azure subscription but NO external approvals

### 4. Deploy Key Vault Premium

#### 4.1 Bicep Template Updates
File: `infrastructure/bicep/modules/keyVaultPremium.bicep`
```bicep
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-numbatwallet-${environment}'
  properties: {
    sku: {
      family: 'A'
      name: 'premium'  // HSM-backed keys available
    }
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true
    enableRbacAuthorization: true
  }
}
```

#### 4.2 Migration Path Configuration
```bicep
// Prepared for future migration
resource migrationConfig 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  name: 'MigrationPath'
  properties: {
    value: json({
      'current': 'KeyVault',
      'next': 'ManagedHsm',
      'migrationReady': false,
      'targetDate': '2026-01-01'
    })
  }
}
```

### 5. Setup Supporting Infrastructure

#### 5.1 Azure Functions for Automation
- Key rotation timer functions
- Compliance report generators
- Certificate expiry monitors
- Grace period handlers

#### 5.2 Service Bus Queues
- key-rotation-events
- certificate-expiry-notifications
- compliance-alerts
- emergency-rotation-requests

#### 5.3 Monitoring Setup
- Application Insights for telemetry
- Log Analytics for audit trails
- Alerts for critical events
- Dashboards for compliance metrics

### 6. Implement Envelope Encryption (Issue #155)

#### 6.1 Complete Implementation from Issue #155
- KEK (Key Encryption Key) per tenant in Key Vault
- DEK (Data Encryption Key) wrapped by KEK
- In-memory DEK caching with TTL
- Automatic DEK rotation

#### 6.2 Migration Support
```csharp
public interface IEnvelopeEncryption
{
    Task<string> EncryptAsync(string plaintext, string tenantId);
    Task<string> DecryptAsync(string ciphertext, string tenantId);
    Task RotateDekAsync(string tenantId);
    Task MigrateToNewKekAsync(string tenantId, string newKekId);
}
```

## Phase 3: Integration Patterns (Week 3-4)
### Documentation for external systems integration

### 7. Create Integration Specifications

#### 7.1 ICAO PKD Integration Interface
File: `docs/integration/ICAO_PKD_INTERFACE.md`
- REST API endpoints specification
- Authentication requirements (mTLS + OAuth)
- Data formats (X.509, CMS, PKCS#7)
- Error handling and retry policies

#### 7.2 Trust List API Contract
File: `docs/integration/TRUST_LIST_API.md`
- GraphQL schema definition
- WebSocket subscriptions for updates
- Blockchain anchoring interface
- Verification procedures

#### 7.3 Certificate Validation Protocol
File: `docs/integration/CERTIFICATE_VALIDATION.md`
- Chain building algorithm
- Revocation checking sequence
- Policy OID validation
- Cross-certification handling

### 8. Build Adapter Layer

#### 8.1 Provider Pattern for External Services
```csharp
public interface IExternalServiceProvider<T>
{
    string ProviderName { get; }
    bool IsAvailable { get; }
    Task<T> GetServiceAsync();
    Task<bool> TestConnectionAsync();
}
```

#### 8.2 Hot-Swappable Providers
- Configuration-based provider selection
- Runtime provider switching
- Fallback chain configuration
- Circuit breaker pattern

## Phase 4: Migration Readiness (Week 4-5)
### Prepare for future external dependencies

### 9. Migration Tools

#### 9.1 Key Migration Utility
Location: `tools/KeyMigration/`
- Batch key export/import
- Progress tracking
- Rollback capability
- Audit trail generation

#### 9.2 Certificate Store Migration
Location: `tools/CertMigration/`
- Bulk certificate import
- Chain reconstruction
- Trust anchor updates
- Validation reporting

### 10. Compliance Documentation

#### 10.1 Security Architecture
- Detailed diagrams with threat modeling
- Data flow documentation
- Key lifecycle documentation
- Incident response procedures

#### 10.2 Audit Reports
- Automated compliance reporting
- Key rotation history
- Access audit trails
- Security metrics dashboard

## Cost Analysis with Options

### Option A: Performance-Optimized
**Configuration:**
- Redis cache for DEKs: +$50/month
- Batch cryptographic operations
- Async key rotation with queuing
- Connection pooling for HSM

**Impact:**
- 10x faster encryption/decryption
- 5x faster key operations
- Suitable for high-volume scenarios
- Total: $350/month (Phase 1)

### Option B: Security-Maximized
**Configuration:**
- No key caching (always fetch from HSM)
- Immediate key rotation on trigger
- Double encryption for critical data
- Audit every cryptographic operation

**Impact:**
- 2x slower operations
- Maximum security posture
- Suitable for high-security requirements
- Total: $300/month (Phase 1)

### Option C: Cost-Optimized
**Configuration:**
- Shared KEKs across tenant groups (5 tenants/KEK)
- 180-day rotation cycles
- Single encryption layer
- Minimal audit logging

**Impact:**
- Reduced isolation between tenants
- Lower operational overhead
- Suitable for internal use case
- Total: $200/month (Phase 1)

## SDK Impact Assessment

### Changes Required in SDK:
1. **Encryption Interface**: SDKs must support envelope encryption format
2. **Key Rotation Handling**: SDKs need grace period awareness
3. **Provider Abstraction**: SDKs should not assume specific HSM type
4. **Offline Operations**: Consider local key caching for offline scenarios

### SDK Update Priority:
1. Update encryption/decryption interfaces
2. Add key rotation event handlers
3. Implement provider-agnostic crypto operations
4. Add configuration for HSM provider selection

## GitHub Issue Management

### Issues to Close:
- #128 (HSM Integration) - Partial, update with phased approach
- #155 (Envelope Encryption) - Can be fully implemented

### Issues to Create:
- "Implement HSM Provider Abstraction Layer"
- "Create Mock External Dependencies"
- "Azure Key Vault Premium Deployment"
- "Migration Tools Development"

### Issues to Update:
- #125 (IACA Root) - Add mock implementation notes
- #126 (DSC) - Add mock service details
- #127 (Trust List) - Document adapter pattern
- #130 (Revocation) - Add mock OCSP responder
- #131 (Key Rotation) - Mark as ready for implementation

## Implementation Schedule

### Week 1 (Immediate):
- Day 1-2: HSM abstraction layer
- Day 3-4: Mock services creation
- Day 5: Key rotation completion

### Week 2:
- Day 1-2: Key Vault Premium deployment
- Day 3-4: Envelope encryption implementation
- Day 5: Azure Functions setup

### Week 3:
- Day 1-2: Integration specifications
- Day 3-4: Adapter layer implementation
- Day 5: Testing and validation

### Week 4:
- Day 1-2: Migration tools
- Day 3-4: Documentation
- Day 5: Compliance reports

### Week 5:
- Day 1-2: Performance testing
- Day 3-4: Security audit
- Day 5: Final review and handover

## Success Criteria

1. **Development Environment**: Fully functional with mock services
2. **Production Phase 1**: Deployed with Key Vault Premium
3. **Migration Path**: Clear upgrade path to Managed/Dedicated HSM
4. **External Ready**: Interfaces defined for all external dependencies
5. **SDK Compatible**: No breaking changes to existing SDKs
6. **Cost Controlled**: Within budget constraints per phase
7. **Performance Met**: <500ms for crypto operations
8. **Security Compliant**: Meets current security requirements
9. **Audit Complete**: Full audit trail for all operations
10. **Documentation**: Complete for all components

## Risk Mitigation

### Risk 1: External Dependencies Delayed
- **Mitigation**: Mock services allow full functionality
- **Contingency**: Use for internal system indefinitely

### Risk 2: Migration Complexity
- **Mitigation**: Provider abstraction enables smooth transition
- **Contingency**: Stay on Key Vault Premium longer

### Risk 3: Performance Issues
- **Mitigation**: Caching layer and async operations
- **Contingency**: Scale horizontally with multiple instances

### Risk 4: Cost Overrun
- **Mitigation**: Phased approach with budget gates
- **Contingency**: Remain on lower-cost tier

## Questions for Clarification (Answered):
1. **Tenant Scale**: Start with 10-50 tenants (internal use)
2. **Compliance Timeline**: FIPS 140-2 Level 2 when tender approved
3. **Geographic Distribution**: Single region (Australia East) initially
4. **SDK Impact**: SDKs should support offline with cached keys

## Contact for Questions
- Platform Security Team: platform-security@numbatwallet.gov.au
- Architecture Team: architecture@numbatwallet.com.au
- DevOps Team: devops@numbatwallet.com.au

---
*This document contains all details from the infrastructure gap analysis and user requirements. It should be used as the authoritative source for implementation.*