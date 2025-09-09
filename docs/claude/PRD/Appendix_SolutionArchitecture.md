# Appendix: Solution Architecture
## Digital Wallet and Verifiable Credentials Solution

**Document Version:** 1.0  
**Parent Document:** [Master PRD](./PRD_Master.md)  
**Last Updated:** December 2024

---

## 1. High-Level Architecture

### 1.1 System Context Diagram

```mermaid
C4Context
    title System Context - Digital Wallet Solution
    
    Person(citizen, "WA Citizen", "Uses ServiceWA app to manage digital credentials")
    Person(verifier, "Verifier", "Validates presented credentials")
    Person(admin, "Administrator", "Manages system configuration")
    
    System_Boundary(wallet_boundary, "Digital Wallet Platform") {
        System(credenxia, "Credenxia v2", "Managed service for credential lifecycle")
    }
    
    System_Ext(servicewa, "ServiceWA", "Mobile application")
    System_Ext(issuers, "Government Issuers", "Source systems for credentials")
    System_Ext(trust, "Trust Registry", "Root of trust for issuers/verifiers")
    System_Ext(azure, "Azure Cloud", "Infrastructure provider")
    
    Rel(citizen, servicewa, "Uses")
    Rel(servicewa, credenxia, "Integrates via SDK", "HTTPS/gRPC")
    Rel(credenxia, issuers, "Requests credentials", "OpenID4VCI")
    Rel(credenxia, trust, "Validates trust", "HTTPS")
    Rel(verifier, credenxia, "Verifies credentials", "OpenID4VP")
    Rel(admin, credenxia, "Manages", "HTTPS")
    Rel(credenxia, azure, "Deployed on", "Private Link")
```

### 1.2 Container Architecture

```mermaid
graph TB
    subgraph "Presentation Layer"
        SDK[ServiceWA SDK]
        ADMIN[Admin Portal]
        DEV[Developer Portal]
    end
    
    subgraph "API Gateway Layer"
        APIGW[Azure API Management]
        WAF[Web Application Firewall]
        FD[Azure Front Door]
    end
    
    subgraph "Application Services"
        AUTH[Auth Service<br/>.NET 8]
        WALLET[Wallet Service<br/>.NET 8]
        ISSUER[Issuer Service<br/>.NET 8]
        VERIFIER[Verifier Service<br/>.NET 8]
        REGISTRY[Registry Service<br/>.NET 8]
        WORKFLOW[Workflow Engine<br/>Elsa 3.0]
    end
    
    subgraph "Infrastructure Services"
        CACHE[Redis Cache]
        QUEUE[Service Bus]
        STORAGE[Blob Storage]
        HSM[Key Vault/HSM]
    end
    
    subgraph "Data Layer"
        PG_MAIN[(PostgreSQL<br/>Primary)]
        PG_READ[(PostgreSQL<br/>Read Replicas)]
        AUDIT[(Audit DB)]
    end
    
    subgraph "Observability"
        LOG[Log Analytics]
        METRIC[Application Insights]
        TRACE[Distributed Tracing]
    end
    
    SDK --> FD
    ADMIN --> FD
    DEV --> FD
    FD --> WAF
    WAF --> APIGW
    APIGW --> AUTH
    APIGW --> WALLET
    APIGW --> ISSUER
    APIGW --> VERIFIER
    APIGW --> REGISTRY
    WALLET --> WORKFLOW
    ISSUER --> WORKFLOW
    
    AUTH --> CACHE
    WALLET --> CACHE
    WALLET --> QUEUE
    ISSUER --> QUEUE
    WALLET --> STORAGE
    ISSUER --> HSM
    VERIFIER --> HSM
    
    WALLET --> PG_MAIN
    ISSUER --> PG_MAIN
    VERIFIER --> PG_READ
    REGISTRY --> PG_READ
    WORKFLOW --> AUDIT
    
    AUTH --> LOG
    WALLET --> METRIC
    ISSUER --> TRACE
```

---

## 2. Component Details

### 2.1 Core Services Architecture

```mermaid
classDiagram
    class IWalletService {
        <<interface>>
        +CreateWallet(tenantId, userId)
        +GetWallet(walletId)
        +StoreCredential(credential)
        +ListCredentials(filter)
        +DeleteCredential(credentialId)
        +BackupWallet()
        +RestoreWallet(backup)
    }
    
    class WalletService {
        -IDbContext context
        -IKeyManager keyManager
        -IStorageService storage
        -IEventBus eventBus
        +CreateWallet(tenantId, userId)
        +GetWallet(walletId)
        +StoreCredential(credential)
    }
    
    class IIssuerService {
        <<interface>>
        +IssueCredential(request)
        +GetIssuanceStatus(id)
        +RevokeCredential(id)
    }
    
    class IssuerService {
        -ISigningService signer
        -ITrustRegistry registry
        -IWorkflowEngine workflow
        +IssueCredential(request)
        +ValidateIssuer()
        +SignCredential()
    }
    
    class IVerifierService {
        <<interface>>
        +CreatePresentationRequest()
        +VerifyPresentation(vp)
        +GetVerificationResult(id)
    }
    
    class VerifierService {
        -ICryptoService crypto
        -ITrustRegistry registry
        -IStatusService status
        +VerifyPresentation(vp)
        +CheckRevocation()
        +ValidateProof()
    }
    
    IWalletService <|.. WalletService
    IIssuerService <|.. IssuerService
    IVerifierService <|.. VerifierService
    
    WalletService --> IKeyManager
    WalletService --> IStorageService
    IssuerService --> ITrustRegistry
    VerifierService --> ITrustRegistry
```

### 2.2 Multi-Tenant Data Architecture

```mermaid
graph TD
    subgraph "Tenant Routing"
        REQ[Incoming Request]
        RESOLVER[Tenant Resolver]
        CONTEXT[Tenant Context]
    end
    
    subgraph "Connection Management"
        POOL[Connection Pool Manager]
        CONN1[Tenant 1 Connection]
        CONN2[Tenant 2 Connection]
        CONNN[Tenant N Connection]
    end
    
    subgraph "Databases"
        DB1[(Tenant 1 DB<br/>wallet_t1)]
        DB2[(Tenant 2 DB<br/>wallet_t2)]
        DBN[(Tenant N DB<br/>wallet_tn)]
        SHARED[(Shared DB<br/>trust_registry)]
    end
    
    REQ --> RESOLVER
    RESOLVER --> CONTEXT
    CONTEXT --> POOL
    POOL --> CONN1
    POOL --> CONN2
    POOL --> CONNN
    CONN1 --> DB1
    CONN2 --> DB2
    CONNN --> DBN
    POOL --> SHARED
```

---

## 3. Deployment Architecture

### 3.1 Azure Infrastructure

```mermaid
graph TB
    subgraph "Global"
        AFD[Azure Front Door<br/>Global Load Balancer]
        DNS[Azure DNS]
    end
    
    subgraph "Australia East (Primary)"
        subgraph "Hub VNET"
            FW1[Azure Firewall]
            APIM1[API Management]
        end
        
        subgraph "Spoke VNET - Apps"
            AKS1[AKS Cluster]
            ACR1[Container Registry]
        end
        
        subgraph "Spoke VNET - Data"
            PG1[(PostgreSQL Flexible<br/>Primary)]
            REDIS1[Redis Cache]
        end
        
        subgraph "Spoke VNET - Security"
            KV1[Key Vault Premium]
            HSM1[Dedicated HSM]
        end
    end
    
    subgraph "Australia Southeast (DR)"
        subgraph "Hub VNET DR"
            FW2[Azure Firewall]
            APIM2[API Management]
        end
        
        subgraph "Spoke VNET - Apps DR"
            AKS2[AKS Cluster<br/>Standby]
            ACR2[Container Registry<br/>Replicated]
        end
        
        subgraph "Spoke VNET - Data DR"
            PG2[(PostgreSQL Flexible<br/>Read Replica)]
            REDIS2[Redis Cache<br/>Replica]
        end
    end
    
    AFD --> FW1
    AFD --> FW2
    FW1 --> APIM1
    FW2 --> APIM2
    APIM1 --> AKS1
    APIM2 --> AKS2
    AKS1 --> PG1
    AKS1 --> REDIS1
    AKS1 --> KV1
    AKS1 --> HSM1
    PG1 -.->|Async Replication| PG2
    REDIS1 -.->|Replication| REDIS2
```

### 3.2 Kubernetes Architecture

```mermaid
graph TB
    subgraph "AKS Cluster"
        subgraph "Ingress"
            ING[NGINX Ingress]
            CERT[Cert Manager]
        end
        
        subgraph "Application Namespace"
            subgraph "Auth Pods"
                AUTH1[auth-service-1]
                AUTH2[auth-service-2]
            end
            
            subgraph "Wallet Pods"
                WALLET1[wallet-service-1]
                WALLET2[wallet-service-2]
                WALLET3[wallet-service-3]
            end
            
            subgraph "Issuer Pods"
                ISS1[issuer-service-1]
                ISS2[issuer-service-2]
            end
            
            subgraph "Verifier Pods"
                VER1[verifier-service-1]
                VER2[verifier-service-2]
            end
        end
        
        subgraph "System Namespace"
            PROM[Prometheus]
            GRAF[Grafana]
            FLUID[Fluentd]
        end
        
        subgraph "Storage"
            PV1[Persistent Volume 1]
            PV2[Persistent Volume 2]
        end
    end
    
    ING --> AUTH1
    ING --> AUTH2
    ING --> WALLET1
    ING --> WALLET2
    ING --> WALLET3
    ING --> ISS1
    ING --> ISS2
    ING --> VER1
    ING --> VER2
```

---

## 4. Security Architecture

### 4.1 Defense in Depth

```mermaid
graph LR
    subgraph "Layer 1: Network"
        DDOS[DDoS Protection]
        WAF2[WAF Rules]
        NSG[Network Security Groups]
    end
    
    subgraph "Layer 2: Identity"
        OIDC[OIDC/OAuth2]
        MFA[Multi-Factor Auth]
        RBAC2[Role-Based Access]
    end
    
    subgraph "Layer 3: Application"
        AUTHZ[Authorization]
        VALIDATION[Input Validation]
        CRYPTO[Cryptography]
    end
    
    subgraph "Layer 4: Data"
        TDE[Encryption at Rest]
        TLS[Encryption in Transit]
        DLP[Data Loss Prevention]
    end
    
    subgraph "Layer 5: Monitoring"
        SIEM[SIEM/SOAR]
        THREAT[Threat Detection]
        AUDIT2[Audit Logging]
    end
    
    DDOS --> WAF2
    WAF2 --> NSG
    NSG --> OIDC
    OIDC --> MFA
    MFA --> RBAC2
    RBAC2 --> AUTHZ
    AUTHZ --> VALIDATION
    VALIDATION --> CRYPTO
    CRYPTO --> TDE
    TDE --> TLS
    TLS --> DLP
    DLP --> SIEM
    SIEM --> THREAT
    THREAT --> AUDIT2
```

### 4.2 Zero Trust Architecture

```mermaid
graph TB
    subgraph "Never Trust, Always Verify"
        USER[User/Device]
        APP[Application]
        DATA[Data Resource]
    end
    
    subgraph "Policy Engine"
        PEP[Policy Enforcement Point]
        PDP[Policy Decision Point]
        PIP[Policy Information Point]
    end
    
    subgraph "Security Signals"
        IDENTITY[Identity Provider]
        DEVICE[Device Compliance]
        THREAT2[Threat Intelligence]
        ANALYTICS[Behavior Analytics]
    end
    
    USER --> PEP
    APP --> PEP
    PEP --> PDP
    PDP --> PIP
    PIP --> IDENTITY
    PIP --> DEVICE
    PIP --> THREAT2
    PIP --> ANALYTICS
    PDP --> DATA
```

---

## 5. Integration Architecture

### 5.1 ServiceWA Integration

```mermaid
sequenceDiagram
    participant App as ServiceWA App
    participant SDK as Embedded SDK
    participant API as Credenxia API
    participant Auth as Auth Service
    participant Wallet as Wallet Service
    participant Cache as Redis Cache
    participant DB as PostgreSQL
    
    App->>SDK: Initialize with config
    SDK->>API: Register device
    API->>Auth: Authenticate device
    Auth->>Auth: Generate tokens
    Auth-->>SDK: Access + Refresh tokens
    
    App->>SDK: Request wallet creation
    SDK->>API: POST /wallet/create
    API->>Auth: Validate token
    Auth-->>API: Token valid
    API->>Wallet: Create wallet
    Wallet->>DB: Store wallet data
    Wallet->>Cache: Cache wallet metadata
    Wallet-->>API: Wallet created
    API-->>SDK: Wallet details
    SDK-->>App: Success callback
```

### 5.2 Issuer Integration Flow

```mermaid
sequenceDiagram
    participant Issuer as Government Issuer
    participant Registry as Trust Registry
    participant IssuerSvc as Issuer Service
    participant Crypto as Crypto Service
    participant HSM as HSM/Key Vault
    participant Storage as Blob Storage
    
    Issuer->>Registry: Register as issuer
    Registry->>Registry: Validate authority
    Registry-->>Issuer: Registration confirmed
    
    Issuer->>IssuerSvc: Request credential issuance
    IssuerSvc->>Registry: Verify issuer status
    Registry-->>IssuerSvc: Issuer verified
    
    IssuerSvc->>Crypto: Prepare credential
    Crypto->>HSM: Request signing key
    HSM-->>Crypto: Signing key
    Crypto->>Crypto: Sign credential
    Crypto-->>IssuerSvc: Signed credential
    
    IssuerSvc->>Storage: Store credential
    Storage-->>IssuerSvc: Storage confirmation
    IssuerSvc-->>Issuer: Credential issued
```

---

## 6. Data Flow Architecture

### 6.1 Credential Lifecycle Flow

```mermaid
stateDiagram-v2
    [*] --> Requested: User requests credential
    Requested --> Validating: Issuer validates request
    Validating --> Approved: Validation successful
    Validating --> Rejected: Validation failed
    Approved --> Issuing: Generate credential
    Issuing --> Issued: Credential signed
    Issued --> Active: Stored in wallet
    Active --> Presented: Used for verification
    Active --> Suspended: Temporarily disabled
    Suspended --> Active: Reactivated
    Active --> Revoked: Permanently revoked
    Presented --> Active: Verification complete
    Revoked --> [*]
    Rejected --> [*]
```

### 6.2 Verification Flow

```mermaid
graph TB
    subgraph "Online Verification"
        VR1[Verifier Request]
        VP1[Presentation]
        API1[API Validation]
        TRUST1[Trust Check]
        STATUS1[Status Check]
        RESULT1[Result]
    end
    
    subgraph "Offline Verification"
        VR2[Verifier Request]
        VP2[Presentation]
        LOCAL[Local Validation]
        CACHE2[Cached Trust]
        CRYPTO2[Crypto Verification]
        RESULT2[Result]
    end
    
    VR1 --> VP1
    VP1 --> API1
    API1 --> TRUST1
    API1 --> STATUS1
    TRUST1 --> RESULT1
    STATUS1 --> RESULT1
    
    VR2 --> VP2
    VP2 --> LOCAL
    LOCAL --> CACHE2
    LOCAL --> CRYPTO2
    CACHE2 --> RESULT2
    CRYPTO2 --> RESULT2
```

---

## 7. Scalability Architecture

### 7.1 Auto-Scaling Strategy

```mermaid
graph LR
    subgraph "Metrics"
        CPU[CPU Usage]
        MEM[Memory Usage]
        REQ[Request Rate]
        LAT[Latency]
    end
    
    subgraph "HPA Rules"
        RULE1[CPU > 70%]
        RULE2[Memory > 80%]
        RULE3[Requests > 1000/s]
        RULE4[P95 Latency > 500ms]
    end
    
    subgraph "Scaling Actions"
        SCALE_OUT[Scale Out<br/>Add Pods]
        SCALE_IN[Scale In<br/>Remove Pods]
        SCALE_UP[Scale Up<br/>Increase Resources]
    end
    
    CPU --> RULE1
    MEM --> RULE2
    REQ --> RULE3
    LAT --> RULE4
    
    RULE1 --> SCALE_OUT
    RULE2 --> SCALE_UP
    RULE3 --> SCALE_OUT
    RULE4 --> SCALE_OUT
```

### 7.2 Database Scaling

```mermaid
graph TD
    subgraph "Write Scaling"
        WRITE[Write Traffic]
        PRIMARY[(Primary DB)]
        SHARD1[(Shard 1)]
        SHARD2[(Shard 2)]
    end
    
    subgraph "Read Scaling"
        READ[Read Traffic]
        LB[Load Balancer]
        READ1[(Read Replica 1)]
        READ2[(Read Replica 2)]
        READ3[(Read Replica 3)]
    end
    
    subgraph "Cache Layer"
        CACHE3[Redis Cluster]
        NODE1[Node 1]
        NODE2[Node 2]
        NODE3[Node 3]
    end
    
    WRITE --> PRIMARY
    PRIMARY --> SHARD1
    PRIMARY --> SHARD2
    
    READ --> LB
    LB --> READ1
    LB --> READ2
    LB --> READ3
    
    READ --> CACHE3
    CACHE3 --> NODE1
    CACHE3 --> NODE2
    CACHE3 --> NODE3
```

---

## 8. Disaster Recovery Architecture

### 8.1 Backup and Recovery

```mermaid
graph TB
    subgraph "Backup Strategy"
        LIVE[Live System]
        SNAP[Snapshots<br/>Every 4 hours]
        DAILY[Daily Backups]
        WEEKLY[Weekly Archives]
        GEO[Geo-Redundant<br/>Storage]
    end
    
    subgraph "Recovery Scenarios"
        CORRUPT[Data Corruption]
        FAIL[Service Failure]
        REGION[Region Failure]
        DISASTER[Complete Disaster]
    end
    
    subgraph "Recovery Actions"
        RESTORE[Point-in-Time<br/>Restore]
        FAILOVER[Automatic<br/>Failover]
        MANUAL[Manual<br/>Recovery]
        REBUILD[Complete<br/>Rebuild]
    end
    
    LIVE --> SNAP
    SNAP --> DAILY
    DAILY --> WEEKLY
    WEEKLY --> GEO
    
    CORRUPT --> RESTORE
    FAIL --> FAILOVER
    REGION --> MANUAL
    DISASTER --> REBUILD
```

### 8.2 High Availability Design

```mermaid
graph LR
    subgraph "Active Region"
        LB1[Load Balancer]
        APP1[App Instance 1]
        APP2[App Instance 2]
        DB1[(Primary DB)]
    end
    
    subgraph "Standby Region"
        LB2[Load Balancer]
        APP3[App Instance 3<br/>Warm Standby]
        DB2[(Standby DB<br/>Read Replica)]
    end
    
    subgraph "Monitoring"
        HEALTH[Health Checks]
        ALERT[Alert System]
        AUTO[Auto-Failover]
    end
    
    LB1 --> APP1
    LB1 --> APP2
    APP1 --> DB1
    APP2 --> DB1
    
    DB1 -.->|Replication| DB2
    
    HEALTH --> LB1
    HEALTH --> LB2
    HEALTH --> AUTO
    AUTO --> LB2
    LB2 --> APP3
    APP3 --> DB2
```

---

## 9. Performance Architecture

### 9.1 Caching Strategy

```mermaid
graph TD
    subgraph "Cache Layers"
        CDN[CDN Cache<br/>Static Assets]
        API_CACHE[API Gateway Cache<br/>Response Cache]
        APP_CACHE[Application Cache<br/>Business Logic]
        DB_CACHE[Database Cache<br/>Query Results]
    end
    
    subgraph "Cache Patterns"
        ASIDE[Cache Aside]
        THROUGH[Write Through]
        BEHIND[Write Behind]
        REFRESH[Refresh Ahead]
    end
    
    subgraph "Invalidation"
        TTL[TTL Expiry]
        EVENT[Event-Based]
        MANUAL2[Manual Purge]
    end
    
    CDN --> API_CACHE
    API_CACHE --> APP_CACHE
    APP_CACHE --> DB_CACHE
    
    APP_CACHE --> ASIDE
    APP_CACHE --> THROUGH
    DB_CACHE --> BEHIND
    DB_CACHE --> REFRESH
    
    TTL --> CDN
    EVENT --> APP_CACHE
    MANUAL2 --> API_CACHE
```

### 9.2 Performance Optimization

```mermaid
graph LR
    subgraph "Frontend Optimization"
        LAZY[Lazy Loading]
        COMPRESS[Compression]
        BUNDLE[Bundle Optimization]
    end
    
    subgraph "Backend Optimization"
        ASYNC[Async Processing]
        BATCH[Batch Operations]
        POOL2[Connection Pooling]
    end
    
    subgraph "Database Optimization"
        INDEX[Indexing]
        PARTITION[Partitioning]
        VACUUM[Vacuum/Analyze]
    end
    
    LAZY --> ASYNC
    COMPRESS --> BATCH
    BUNDLE --> POOL2
    ASYNC --> INDEX
    BATCH --> PARTITION
    POOL2 --> VACUUM
```

---

## 10. Migration Architecture

### 10.1 Migration from Per-Tenant to Shared DB (If Needed)

```mermaid
graph TD
    subgraph "Phase 1: Preparation"
        ASSESS[Assessment]
        DESIGN[Schema Design]
        TEST[Test Migration]
    end
    
    subgraph "Phase 2: Dual-Write"
        DUAL[Dual-Write Logic]
        SYNC[Data Sync]
        VALIDATE[Validation]
    end
    
    subgraph "Phase 3: Cutover"
        READONLY[Read-Only Mode]
        FINAL[Final Sync]
        SWITCH[Switch Traffic]
    end
    
    subgraph "Phase 4: Cleanup"
        VERIFY[Verification]
        BACKUP[Backup Old]
        DECOM[Decommission]
    end
    
    ASSESS --> DESIGN
    DESIGN --> TEST
    TEST --> DUAL
    DUAL --> SYNC
    SYNC --> VALIDATE
    VALIDATE --> READONLY
    READONLY --> FINAL
    FINAL --> SWITCH
    SWITCH --> VERIFY
    VERIFY --> BACKUP
    BACKUP --> DECOM
```

---

## 11. Technology Stack Summary

### 11.1 Core Technologies

| Layer | Technology | Version | Purpose |
|-------|------------|---------|---------|
| Runtime | .NET | 8.0 LTS | Application framework |
| Language | C# | 12.0 | Primary language |
| API | ASP.NET Core | 8.0 | REST/gRPC APIs |
| Database | PostgreSQL | 15+ | Primary datastore |
| Cache | Redis | 7.0 | Distributed cache |
| Message Queue | Azure Service Bus | - | Async messaging |
| Workflow | Elsa | 3.0 | Business workflows |
| Container | Docker | Latest | Containerization |
| Orchestration | Kubernetes | 1.28+ | Container orchestration |
| Service Mesh | Linkerd | 2.14 | Service communication |

### 11.2 Azure Services

| Service | SKU | Purpose |
|---------|-----|---------|
| AKS | Standard | Container hosting |
| PostgreSQL Flexible | General Purpose | Database |
| Key Vault | Premium | Key management |
| HSM | Dedicated | Hardware security |
| Front Door | Premium | Global load balancing |
| API Management | Standard | API gateway |
| Service Bus | Standard | Messaging |
| Storage | Standard GRS | Object storage |
| Monitor | - | Observability |
| Sentinel | - | SIEM/SOAR |

---

## 12. Architecture Decision Records (ADRs)

### ADR-001: Multi-Tenancy Strategy

**Decision:** Use per-tenant database isolation  
**Status:** Accepted  
**Context:** Need to balance security, performance, and operational complexity  
**Decision:** Implement one database per tenant on shared infrastructure  
**Consequences:** Higher operational overhead but better security isolation  

### ADR-002: API Protocol

**Decision:** REST as primary, gRPC for inter-service  
**Status:** Accepted  
**Context:** Need to support various client types and ensure performance  
**Decision:** REST for external APIs, gRPC for internal service communication  
**Consequences:** Broader compatibility with some complexity in protocol management  

### ADR-003: Workflow Engine

**Decision:** Use Elsa Workflows  
**Status:** Accepted  
**Context:** Need flexible, code-first workflow engine  
**Decision:** Implement Elsa 3.0 for credential issuance workflows  
**Consequences:** Modern workflow capabilities with .NET integration  

---

## Architecture Governance

### Review Schedule
- Weekly architecture review during POA
- Bi-weekly during Pilot setup
- Monthly during steady-state operations

### Change Control
- All architectural changes require ADR
- Review board approval for significant changes
- Impact assessment for all modifications

### Documentation
- Architecture diagrams updated with each release
- Decision log maintained in Git
- Runbooks updated quarterly

---

**END OF SOLUTION ARCHITECTURE APPENDIX**