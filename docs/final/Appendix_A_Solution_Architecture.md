# Appendix A: Solution Architecture
## Digital Wallet and Verifiable Credentials Solution

**Document Version:** 2.0 FINAL  
**Parent Document:** [Master PRD](./PRD_Master.md)  
**Last Updated:** December 2024

---

## Table of Contents
1. [High-Level Architecture](#1-high-level-architecture)
2. [Component Architecture](#2-component-architecture)
3. [Azure Infrastructure Design](#3-azure-infrastructure-design)
4. [Data Architecture](#4-data-architecture)
5. [Security Architecture](#5-security-architecture)
6. [Integration Architecture](#6-integration-architecture)
7. [Deployment Architecture](#7-deployment-architecture)
8. [Scalability and Performance](#8-scalability-and-performance)

---

## 1. High-Level Architecture

### 1.1 System Context Diagram

```mermaid
C4Context
    title System Context - Digital Wallet Solution
    
    Person(citizen, "WA Citizen", "Uses ServiceWA app to manage digital credentials")
    Person(verifier, "Verifier", "Validates presented credentials")
    Person(admin, "Administrator", "Manages system configuration")
    
    System_Boundary(wallet_boundary, "Credenxia v2 Platform") {
        System(credenxia, "Digital Wallet Service", "Managed service for credential lifecycle")
    }
    
    System_Ext(servicewa, "ServiceWA", "Mobile application")
    System_Ext(dtp, "Digital Trust Platform", "Government credential gateway")
    System_Ext(idx, "WA Identity Exchange", "Identity federation")
    System_Ext(issuers, "Agency Issuers", "Source systems for credentials")
    System_Ext(azure, "Azure AU Cloud", "Infrastructure provider")
    
    Rel(citizen, servicewa, "Uses")
    Rel(servicewa, credenxia, "Integrates via SDK", "HTTPS/gRPC")
    Rel(credenxia, dtp, "Requests credentials", "REST/OpenID4VCI")
    Rel(credenxia, idx, "Authenticates", "OIDC")
    Rel(dtp, issuers, "Sources data", "Various")
    Rel(verifier, credenxia, "Verifies credentials", "OpenID4VP")
    Rel(admin, credenxia, "Manages", "HTTPS")
    Rel(credenxia, azure, "Deployed on", "Private Link")
```

### 1.2 Container Architecture

```mermaid
graph TB
    subgraph "Presentation Layer"
        SDK[ServiceWA SDK<br/>Flutter]
        ADMIN[Admin Portal<br/>React]
        DEV[Developer Portal<br/>Next.js]
        VERIFY[Verifier Web<br/>TypeScript]
    end
    
    subgraph "API Gateway Layer"
        FD[Azure Front Door<br/>Global CDN]
        WAF[Web Application Firewall<br/>OWASP Rules]
        APIGW[Azure API Management<br/>Rate Limiting]
    end
    
    subgraph "Application Services - .NET 8"
        AUTH[Authentication Service<br/>OIDC/OAuth2]
        WALLET[Wallet Service<br/>Credential Storage]
        ISSUER[Issuer Service<br/>Credential Creation]
        VERIFIER[Verifier Service<br/>Proof Validation]
        REGISTRY[Registry Service<br/>Trust Management]
        WORKFLOW[Workflow Engine<br/>Orchestration]
    end
    
    subgraph "Infrastructure Services"
        CACHE[Redis Cache<br/>Session/Data]
        QUEUE[Service Bus<br/>Events/Commands]
        STORAGE[Blob Storage<br/>Documents]
        HSM[Key Vault/HSM<br/>FIPS 140-2 L3]
    end
    
    subgraph "Data Layer"
        PG_MAIN[(PostgreSQL<br/>Primary - AU East)]
        PG_READ[(PostgreSQL<br/>Read Replicas)]
        PG_AUDIT[(Audit Database<br/>Immutable Logs)]
    end
    
    subgraph "Observability"
        LOG[Azure Monitor<br/>Logs]
        METRIC[Application Insights<br/>Metrics]
        TRACE[Distributed Tracing<br/>Jaeger]
        ALERT[Alert Manager<br/>PagerDuty]
    end
    
    SDK --> FD
    ADMIN --> FD
    DEV --> FD
    VERIFY --> FD
    FD --> WAF
    WAF --> APIGW
    APIGW --> AUTH
    APIGW --> WALLET
    APIGW --> ISSUER
    APIGW --> VERIFIER
    APIGW --> REGISTRY
    
    AUTH --> CACHE
    WALLET --> CACHE
    WALLET --> QUEUE
    ISSUER --> QUEUE
    VERIFIER --> QUEUE
    WALLET --> STORAGE
    ISSUER --> HSM
    VERIFIER --> HSM
    WALLET --> WORKFLOW
    ISSUER --> WORKFLOW
    
    WALLET --> PG_MAIN
    ISSUER --> PG_MAIN
    VERIFIER --> PG_READ
    REGISTRY --> PG_READ
    WORKFLOW --> PG_AUDIT
    
    AUTH --> LOG
    WALLET --> METRIC
    ISSUER --> TRACE
    VERIFIER --> ALERT
```

---

## 2. Component Architecture

### 2.1 Microservices Design

```mermaid
classDiagram
    class IWalletService {
        <<interface>>
        +CreateWallet(tenantId, userId) Wallet
        +GetWallet(walletId) Wallet
        +StoreCredential(credential) Result
        +ListCredentials(filter) List~Credential~
        +DeleteCredential(credentialId) Result
        +BackupWallet() BackupData
        +RestoreWallet(backup) Result
        +BindDevice(deviceId) Result
    }
    
    class WalletService {
        -IDbContext context
        -IKeyManager keyManager
        -IStorageService storage
        -IEventBus eventBus
        -ICache cache
        +CreateWallet(tenantId, userId)
        +EncryptCredential(credential)
        +PublishEvent(event)
    }
    
    class IIssuerService {
        <<interface>>
        +IssueCredential(request) Credential
        +GetIssuanceStatus(id) Status
        +RevokeCredential(id) Result
        +UpdateCredential(id, data) Result
    }
    
    class IssuerService {
        -ISigningService signer
        -ITrustRegistry registry
        -IWorkflowEngine workflow
        -IDTPClient dtpClient
        +IssueCredential(request)
        +ValidateIssuer()
        +SignCredential()
        +NotifyIssuance()
    }
    
    class IVerifierService {
        <<interface>>
        +CreatePresentationRequest() Request
        +VerifyPresentation(vp) Result
        +GetVerificationResult(id) Result
        +ValidateOffline(proof) Result
    }
    
    class VerifierService {
        -ICryptoService crypto
        -ITrustRegistry registry
        -IStatusService status
        -IRevocationService revocation
        +VerifyPresentation(vp)
        +CheckRevocation()
        +ValidateProof()
        +ValidateSelectiveDisclosure()
    }
    
    IWalletService <|.. WalletService
    IIssuerService <|.. IssuerService
    IVerifierService <|.. VerifierService
    
    WalletService --> IKeyManager
    WalletService --> IStorageService
    IssuerService --> ITrustRegistry
    VerifierService --> ITrustRegistry
```

### 2.2 Event-Driven Architecture

```mermaid
graph LR
    subgraph "Event Producers"
        WS[Wallet Service]
        IS[Issuer Service]
        VS[Verifier Service]
    end
    
    subgraph "Service Bus Topics"
        WT[wallet-events]
        IT[issuance-events]
        VT[verification-events]
        AT[audit-events]
    end
    
    subgraph "Event Consumers"
        AN[Analytics Service]
        AU[Audit Service]
        NO[Notification Service]
        WF[Workflow Engine]
    end
    
    WS --> WT
    IS --> IT
    VS --> VT
    
    WT --> AN
    WT --> AU
    IT --> NO
    IT --> WF
    VT --> AN
    VT --> AU
    
    style WT fill:#e1f5e1
    style IT fill:#e1f5e1
    style VT fill:#e1f5e1
    style AT fill:#ffe1e1
```

---

## 3. Azure Infrastructure Design

→ **See [Azure Platform Justification](./Azure_Justification_and_Pricing.md) for detailed analysis including Perth Extended Zone announcement (mid-2025)**

### 3.1 Network Architecture

```mermaid
graph TB
    subgraph "Internet"
        USER[Users]
        VER[Verifiers]
    end
    
    subgraph "Azure Front Door"
        AFD[Global Load Balancer<br/>DDoS Protection]
        WAF2[WAF Rules]
    end
    
    subgraph "Hub VNet 10.0.0.0/16"
        FW[Azure Firewall<br/>10.0.1.0/24]
        APIM[API Management<br/>10.0.2.0/24]
        BASTION[Bastion Host<br/>10.0.3.0/24]
    end
    
    subgraph "Application VNet 10.1.0.0/16"
        subgraph "AKS Subnet 10.1.0.0/20"
            AKS[Azure Kubernetes Service]
            PODS[Application Pods]
        end
        subgraph "Integration Subnet 10.1.16.0/24"
            PRIVATELINK[Private Endpoints]
        end
    end
    
    subgraph "Data VNet 10.2.0.0/16"
        subgraph "Database Subnet 10.2.0.0/24"
            PG[PostgreSQL Flexible Server]
        end
        subgraph "Cache Subnet 10.2.1.0/24"
            REDIS[Redis Cache]
        end
        subgraph "Storage Subnet 10.2.2.0/24"
            BLOB[Blob Storage]
        end
    end
    
    subgraph "Security VNet 10.3.0.0/16"
        subgraph "HSM Subnet 10.3.0.0/24"
            HSM2[Dedicated HSM]
            KV[Key Vault Premium]
        end
    end
    
    USER --> AFD
    VER --> AFD
    AFD --> FW
    FW --> APIM
    APIM --> AKS
    AKS --> PRIVATELINK
    PRIVATELINK --> PG
    PRIVATELINK --> REDIS
    PRIVATELINK --> BLOB
    PRIVATELINK --> KV
    BASTION --> AKS
```

### 3.2 High Availability Design

```mermaid
graph TB
    subgraph "Australia East (Primary)"
        subgraph "Zone 1"
            AKS_1A[AKS Node 1]
            PG_1A[PostgreSQL Primary]
        end
        subgraph "Zone 2"
            AKS_1B[AKS Node 2]
            PG_1B[PostgreSQL Standby]
        end
        subgraph "Zone 3"
            AKS_1C[AKS Node 3]
            REDIS_1[Redis Primary]
        end
    end
    
    subgraph "Australia Southeast (DR)"
        subgraph "Zone 1 DR"
            AKS_2A[AKS Node 1 - Standby]
            PG_2A[PostgreSQL Read Replica]
        end
        subgraph "Zone 2 DR"
            AKS_2B[AKS Node 2 - Standby]
            REDIS_2[Redis Replica]
        end
    end
    
    subgraph "Replication"
        PG_1A -.->|Async Replication| PG_2A
        PG_1B -.->|Sync Replication| PG_1A
        REDIS_1 -.->|Async Replication| REDIS_2
    end
    
    style PG_1A fill:#90EE90
    style AKS_1A fill:#90EE90
    style AKS_1B fill:#90EE90
    style AKS_1C fill:#90EE90
```

---

## 4. Data Architecture

### 4.1 Multi-Tenant Database Design

```mermaid
erDiagram
    TENANT {
        uuid tenant_id PK
        string name
        string domain
        json configuration
        timestamp created_at
        boolean is_active
    }
    
    WALLET {
        uuid wallet_id PK
        uuid tenant_id FK
        uuid user_id
        string did
        json public_keys
        timestamp created_at
        timestamp last_accessed
    }
    
    CREDENTIAL {
        uuid credential_id PK
        uuid wallet_id FK
        string type
        json data_encrypted
        string issuer_did
        timestamp issued_at
        timestamp expires_at
        string status
    }
    
    PRESENTATION {
        uuid presentation_id PK
        uuid wallet_id FK
        uuid credential_id FK
        json disclosed_attributes
        string verifier_did
        timestamp presented_at
    }
    
    REVOCATION {
        uuid revocation_id PK
        uuid credential_id FK
        string reason
        timestamp revoked_at
        string revoked_by
    }
    
    DEVICE {
        uuid device_id PK
        uuid wallet_id FK
        string device_name
        string platform
        json device_info
        timestamp registered_at
    }
    
    TENANT ||--o{ WALLET : owns
    WALLET ||--o{ CREDENTIAL : contains
    WALLET ||--o{ DEVICE : binds
    CREDENTIAL ||--o{ PRESENTATION : uses
    CREDENTIAL ||--o| REVOCATION : may_have
```

### 4.2 Data Isolation Strategy

| Aspect | Implementation | Benefit |
| --- | --- | --- |
| **Database** | Per-tenant PostgreSQL database | Complete data isolation |
| **Encryption** | Per-tenant Data Encryption Keys (DEK) | Cryptographic isolation |
| **Backup** | Independent backup schedules | Tenant-specific recovery |
| **Connection** | Dedicated connection pools | Performance isolation |
| **Audit** | Separate audit trails | Compliance isolation |

---

## 5. Security Architecture

### 5.1 Defense in Depth

```mermaid
graph TD
    subgraph "Layer 1: Network Security"
        DDOS[DDoS Protection]
        WAF3[Web Application Firewall]
        NSG[Network Security Groups]
    end
    
    subgraph "Layer 2: Identity & Access"
        OIDC[OIDC/OAuth 2.0]
        MFA[Multi-Factor Auth]
        RBAC[Role-Based Access]
    end
    
    subgraph "Layer 3: Application Security"
        INPUT[Input Validation]
        AUTHZ[Authorization Checks]
        AUDIT2[Audit Logging]
    end
    
    subgraph "Layer 4: Data Security"
        TLS[TLS 1.3 in Transit]
        AES[AES-256-GCM at Rest]
        KEY[Key Management HSM]
    end
    
    subgraph "Layer 5: Infrastructure Security"
        PATCH[Automated Patching]
        VULN[Vulnerability Scanning]
        PEN[Penetration Testing]
    end
    
    DDOS --> WAF3
    WAF3 --> NSG
    NSG --> OIDC
    OIDC --> MFA
    MFA --> RBAC
    RBAC --> INPUT
    INPUT --> AUTHZ
    AUTHZ --> AUDIT2
    AUDIT2 --> TLS
    TLS --> AES
    AES --> KEY
    KEY --> PATCH
    PATCH --> VULN
    VULN --> PEN
```

### 5.2 Cryptographic Architecture

```mermaid
graph LR
    subgraph "Key Hierarchy"
        ROOT[Root CA<br/>Offline HSM]
        INTER[Intermediate CA<br/>Online HSM]
        TENANT[Tenant Keys<br/>Key Vault]
        WALLET2[Wallet Keys<br/>Device Secure Element]
    end
    
    subgraph "Signing Operations"
        CRED[Credential Signing]
        PRES[Presentation Signing]
        REV[Revocation Signing]
    end
    
    subgraph "Encryption Operations"
        STORE[Storage Encryption]
        TRANS[Transport Encryption]
        BACKUP[Backup Encryption]
    end
    
    ROOT --> INTER
    INTER --> TENANT
    TENANT --> WALLET2
    
    TENANT --> CRED
    WALLET2 --> PRES
    INTER --> REV
    
    TENANT --> STORE
    TENANT --> TRANS
    TENANT --> BACKUP
```

---

## 6. Integration Architecture

### 6.1 External System Integration

```mermaid
sequenceDiagram
    participant C as Citizen
    participant S as ServiceWA
    participant W as Wallet Service
    participant D as DTP
    participant I as Agency Issuer
    participant X as WA IdX
    
    C->>S: Request Credential
    S->>W: InitiateIssuance(userId)
    W->>X: Authenticate User
    X-->>W: Auth Token
    W->>D: RequestCredential(token)
    D->>I: FetchAttributes
    I-->>D: Attributes
    D-->>W: Credential Data
    W->>W: Sign & Encrypt
    W-->>S: Credential
    S-->>C: Display Credential
```

### 6.2 API Gateway Pattern

```mermaid
graph TB
    subgraph "External Clients"
        MOB[Mobile SDK]
        WEB[Web SDK]
        API[API Clients]
    end
    
    subgraph "API Management"
        subgraph "Policies"
            RATE[Rate Limiting<br/>1000 req/min]
            CACHE2[Response Cache<br/>60 seconds]
            TRANS[Transform<br/>XML to JSON]
        end
        subgraph "Security"
            OAUTH[OAuth Validation]
            CORS[CORS Policy]
            IP[IP Whitelisting]
        end
        subgraph "Routing"
            ROUTE[Path-based Routing]
            VERSION[Version Management]
            BALANCE[Load Balancing]
        end
    end
    
    subgraph "Backend Services"
        SVC1[Wallet Service v2]
        SVC2[Issuer Service v2]
        SVC3[Verifier Service v1]
    end
    
    MOB --> RATE
    WEB --> RATE
    API --> RATE
    RATE --> OAUTH
    OAUTH --> ROUTE
    ROUTE --> SVC1
    ROUTE --> SVC2
    ROUTE --> SVC3
```

---

## 7. Deployment Architecture

### 7.1 Container Orchestration

```mermaid
graph TB
    subgraph "AKS Cluster"
        subgraph "System Namespace"
            INGRESS[NGINX Ingress]
            CERT[Cert Manager]
            MONITOR[Prometheus]
        end
        
        subgraph "Application Namespace"
            subgraph "Wallet Pods"
                WP1[wallet-pod-1<br/>2 CPU, 4GB RAM]
                WP2[wallet-pod-2<br/>2 CPU, 4GB RAM]
                WP3[wallet-pod-3<br/>2 CPU, 4GB RAM]
            end
            
            subgraph "Issuer Pods"
                IP1[issuer-pod-1<br/>1 CPU, 2GB RAM]
                IP2[issuer-pod-2<br/>1 CPU, 2GB RAM]
            end
            
            subgraph "Verifier Pods"
                VP1[verifier-pod-1<br/>1 CPU, 2GB RAM]
                VP2[verifier-pod-2<br/>1 CPU, 2GB RAM]
            end
        end
        
        subgraph "Data Namespace"
            REDIS3[Redis Master]
            REDIS_S[Redis Slave]
        end
    end
    
    INGRESS --> WP1
    INGRESS --> WP2
    INGRESS --> WP3
    WP1 --> REDIS3
    WP2 --> REDIS3
    WP3 --> REDIS3
```

### 7.2 CI/CD Pipeline

```mermaid
graph LR
    subgraph "Source Control"
        GIT[GitHub]
        PR[Pull Request]
    end
    
    subgraph "Build Stage"
        BUILD[.NET Build]
        TEST[Unit Tests]
        SCAN[Security Scan]
        DOCKER[Docker Build]
    end
    
    subgraph "Deploy Dev"
        DEV_ENV[Dev AKS]
        SMOKE[Smoke Tests]
    end
    
    subgraph "Deploy Test"
        TEST_ENV[Test AKS]
        E2E[E2E Tests]
        PERF[Perf Tests]
    end
    
    subgraph "Deploy Prod"
        APPROVE[Manual Approval]
        PROD_ENV[Prod AKS]
        HEALTH[Health Checks]
    end
    
    GIT --> PR
    PR --> BUILD
    BUILD --> TEST
    TEST --> SCAN
    SCAN --> DOCKER
    DOCKER --> DEV_ENV
    DEV_ENV --> SMOKE
    SMOKE --> TEST_ENV
    TEST_ENV --> E2E
    E2E --> PERF
    PERF --> APPROVE
    APPROVE --> PROD_ENV
    PROD_ENV --> HEALTH
```

---

## 8. Scalability and Performance

### 8.1 Scaling Strategy

| Component | Scaling Type | Trigger | Target |
| --- | --- | --- | --- |
| **API Gateway** | Horizontal | CPU > 70% | 3-10 instances |
| **Wallet Service** | Horizontal | Memory > 80% | 3-20 pods |
| **Issuer Service** | Horizontal | Queue depth > 100 | 2-10 pods |
| **Verifier Service** | Horizontal | Requests > 1000/min | 2-15 pods |
| **PostgreSQL** | Vertical + Read Replicas | CPU > 80% | Up to 32 vCPUs |
| **Redis Cache** | Cluster Mode | Memory > 75% | 3-6 shards |

### 8.2 Performance Optimization

```mermaid
graph TD
    subgraph "Caching Strategy"
        L1[L1: Application Memory<br/>10ms latency]
        L2[L2: Redis Cache<br/>50ms latency]
        L3[L3: Database<br/>200ms latency]
    end
    
    subgraph "Query Optimization"
        INDEX[Database Indexes]
        PART[Table Partitioning]
        MAT[Materialized Views]
    end
    
    subgraph "Async Processing"
        QUEUE2[Service Bus Queues]
        BATCH[Batch Processing]
        EVENTS[Event Sourcing]
    end
    
    L1 --> L2
    L2 --> L3
    L3 --> INDEX
    INDEX --> PART
    PART --> MAT
    MAT --> QUEUE2
    QUEUE2 --> BATCH
    BATCH --> EVENTS
```

### 8.3 Load Distribution

```mermaid
graph TB
    subgraph "Traffic Distribution"
        AFD2[Azure Front Door]
        subgraph "Australia East 70%"
            AE_LB[Load Balancer]
            AE_PODS[10 Pods]
        end
        subgraph "Australia Southeast 30%"
            AS_LB[Load Balancer]
            AS_PODS[5 Pods]
        end
    end
    
    subgraph "Database Load"
        WRITE[Write Master]
        READ1[Read Replica 1]
        READ2[Read Replica 2]
    end
    
    AFD2 -->|70%| AE_LB
    AFD2 -->|30%| AS_LB
    AE_LB --> AE_PODS
    AS_LB --> AS_PODS
    AE_PODS -->|Writes| WRITE
    AE_PODS -->|Reads| READ1
    AS_PODS -->|Reads| READ2
```

---

## Infrastructure Cost Implications

### Azure Services Monthly Estimate (Production)

| Service | Configuration | Monthly Cost |
| --- | --- | --- |
| **AKS** | 3 nodes D4s v5 (primary) + 2 nodes (DR) | $1,200 |
| **PostgreSQL** | 8 vCore, 256GB storage, HA | $1,800 |
| **Redis Cache** | Premium P2, 6GB | $450 |
| **API Management** | Standard tier, 2 units | $580 |
| **Key Vault/HSM** | Premium + 1 HSM pool | $3,500 |
| **Storage** | 1TB blob, 100GB tables | $150 |
| **Networking** | VNets, Private Links, Firewall | $800 |
| **Monitoring** | App Insights, Log Analytics | $300 |
| **Backup** | Automated backups, retention | $200 |
| **Total** | | **$8,980/month** |

*Note: These costs align with the infrastructure estimates in the pricing model and do not affect the overall pilot pricing of $1,866,250.*

---

## 8. Architecture Design Decisions

### 8.1 Application Gateway/DMZ Layer Consideration

#### Current Architecture Decision
The solution uses **Azure Front Door with integrated WAF** as the primary edge security layer, without an additional Application Gateway in a DMZ pattern. This decision was made after careful analysis of security, performance, and cost factors.

#### Rationale for Excluding Application Gateway

| Factor | Analysis | Decision Impact |
| --- | --- | --- |
| **Security** | Azure Front Door provides enterprise-grade WAF with OWASP Core Rule Set, DDoS protection, and bot protection | No security compromise |
| **Performance** | Eliminates additional network hop, reducing latency by 10-20ms | Better user experience |
| **Cost** | Saves $740-2,000/month per environment | More budget for core features |
| **Complexity** | Single WAF policy management, simpler troubleshooting | Reduced operational overhead |
| **Compliance** | Front Door + API Management meet all WA Government security requirements | Fully compliant |

#### Architecture Comparison

```mermaid
graph LR
    subgraph "Current Architecture"
        U1[User] --> FD1[Azure Front Door<br/>+ WAF]
        FD1 --> APIM1[API Management<br/>+ Policies]
        APIM1 --> SVC1[Services]
    end
    
    subgraph "Alternative with App Gateway"
        U2[User] --> FD2[Azure Front Door]
        FD2 --> AG[Application Gateway<br/>+ WAF in DMZ]
        AG --> APIM2[API Management]
        APIM2 --> SVC2[Services]
    end
```

#### When Application Gateway SHOULD be Added

Application Gateway can be added to the architecture if the client requires:

1. **Strict DMZ Requirements**: Regulatory mandate for explicit DMZ layer separation
2. **Legacy Integration**: On-premises systems requiring specific L7 routing
3. **Protocol Support**: WebSocket or non-HTTP protocols not supported by Front Door
4. **Enhanced Inspection**: Deep packet inspection beyond WAF capabilities
5. **Regional Isolation**: Strict network segmentation between regions

#### Implementation Path if Required

If Application Gateway is requested:
- **Additional Cost**: $740-2,000/month per environment
- **Implementation Time**: 2-3 weeks including testing
- **Architecture Change**: Add between Front Door and API Management
- **Configuration**: 
  - Deploy in dedicated subnet (DMZ)
  - Configure mTLS to backend
  - Synchronize WAF rules with Front Door
  - Update monitoring and alerts

#### Recommendation
The current architecture without Application Gateway is **recommended for this solution** as it:
- Meets all security and compliance requirements
- Provides better performance
- Reduces operational complexity
- Optimizes costs

However, the architecture is **flexible** and Application Gateway can be added post-deployment if specific requirements emerge.

---

## Summary

This solution architecture delivers:
1. **Enterprise-grade security** with defense-in-depth
2. **High availability** across Australian regions
3. **Scalability** to support 2M+ citizens
4. **Performance** with sub-200ms response times
5. **Compliance** with Australian data sovereignty requirements
6. **Cost-effectiveness** through Azure-native services

The architecture supports the complete digital wallet lifecycle while maintaining the pricing structure outlined in the Master PRD.

---
[Back to Master PRD](./PRD_Master.md) | [Next: Security & Compliance](./Appendix_B_Security_Privacy_Compliance.md)