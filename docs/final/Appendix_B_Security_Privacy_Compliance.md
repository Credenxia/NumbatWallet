# Appendix B – Security, Privacy & Compliance
## Digital Wallet and Verifiable Credentials Solution

**Document Version:** 2.0 FINAL  
**Parent Document:** [Master PRD](./PRD_Master.md)  
**Last Updated:** December 2024

---

## Table of Contents
1. [Security Architecture](#1-security-architecture)
2. [Cryptographic Framework](#2-cryptographic-framework)
3. [Identity and Access Management](#3-identity-and-access-management)
4. [Privacy and Data Protection](#4-privacy-and-data-protection)
5. [Compliance Framework](#5-compliance-framework)
6. [Threat Model and Risk Assessment](#6-threat-model-and-risk-assessment)
7. [Incident Response and Recovery](#7-incident-response-and-recovery)
8. [Security Operations](#8-security-operations)

---

## 1. Security Architecture

### 1.1 Defense-in-Depth Security Model

```mermaid
graph TB
    subgraph "Layer 1: Physical Security"
        PHYS[Azure AU Datacenters<br/>SOC 2 Certified]
    end
    
    subgraph "Layer 2: Network Security"
        DDOS[DDoS Protection Standard]
        WAF[Web Application Firewall<br/>OWASP Core Rules]
        NSG[Network Security Groups]
        VNET[Virtual Network Isolation]
    end
    
    subgraph "Layer 3: Identity & Access"
        AZURE_AD[Azure AD B2C]
        MFA[Multi-Factor Authentication]
        RBAC[Role-Based Access Control]
        PIM[Privileged Identity Management]
    end
    
    subgraph "Layer 4: Application Security"
        SAST[Static Analysis<br/>SonarQube]
        DAST[Dynamic Analysis<br/>OWASP ZAP]
        SCA[Dependency Scanning<br/>Snyk]
        SECRETS[Secret Management<br/>Key Vault]
    end
    
    subgraph "Layer 5: Data Security"
        ENCRYPT_REST[AES-256-GCM at Rest]
        ENCRYPT_TRANSIT[TLS 1.3 in Transit]
        DLP[Data Loss Prevention]
        CLASS[Data Classification]
    end
    
    subgraph "Layer 6: Operational Security"
        MONITOR[Azure Monitor]
        SENTINEL[Azure Sentinel SIEM]
        AUDIT[Immutable Audit Logs]
        RESPONSE[24/7 SOC]
    end
    
    PHYS --> DDOS
    DDOS --> WAF
    WAF --> NSG
    NSG --> VNET
    VNET --> AZURE_AD
    AZURE_AD --> MFA
    MFA --> RBAC
    RBAC --> PIM
    PIM --> SAST
    SAST --> DAST
    DAST --> SCA
    SCA --> SECRETS
    SECRETS --> ENCRYPT_REST
    ENCRYPT_REST --> ENCRYPT_TRANSIT
    ENCRYPT_TRANSIT --> DLP
    DLP --> CLASS
    CLASS --> MONITOR
    MONITOR --> SENTINEL
    SENTINEL --> AUDIT
    AUDIT --> RESPONSE
```

### 1.2 Security Controls Matrix

| Control Category | Implementation | Standard | Validation |
| --- | --- | --- | --- |
| **Preventive** | WAF, Network segmentation, Encryption | ISO 27001 | Quarterly review |
| **Detective** | SIEM, IDS/IPS, Logging | NIST 800-53 | Real-time alerts |
| **Corrective** | Automated remediation, Patches | CIS Controls | Monthly updates |
| **Deterrent** | Security headers, Rate limiting | OWASP | Penetration testing |
| **Compensating** | Backup systems, DR sites | IRAP | Annual DR drill |

---

## 2. Cryptographic Framework

### 2.1 Key Management Hierarchy

```mermaid
graph TD
    subgraph "Hardware Security Module"
        HSM[Azure Dedicated HSM<br/>FIPS 140-2 Level 3]
        ROOT_CA[Root CA Key<br/>RSA-4096, Offline]
    end
    
    subgraph "Key Vault Premium"
        INT_CA[Intermediate CA<br/>RSA-3072]
        MEK[Master Encryption Key<br/>AES-256]
    end
    
    subgraph "Tenant Keys"
        KEK_T1[Tenant 1 KEK<br/>AES-256]
        KEK_T2[Tenant 2 KEK<br/>AES-256]
        KEK_TN[Tenant N KEK<br/>AES-256]
    end
    
    subgraph "Data Keys"
        DEK_CRED[Credential DEKs<br/>AES-256-GCM]
        DEK_PII[PII DEKs<br/>AES-256-GCM]
        DEK_AUDIT[Audit DEKs<br/>AES-256-GCM]
    end
    
    subgraph "Signing Keys"
        SIGN_CRED[Credential Signing<br/>ECDSA P-256]
        SIGN_PRES[Presentation Signing<br/>EdDSA Ed25519]
        SIGN_REV[Revocation Signing<br/>RSA-PSS 2048]
    end
    
    HSM --> ROOT_CA
    ROOT_CA --> INT_CA
    INT_CA --> MEK
    MEK --> KEK_T1
    MEK --> KEK_T2
    MEK --> KEK_TN
    KEK_T1 --> DEK_CRED
    KEK_T1 --> DEK_PII
    KEK_T1 --> DEK_AUDIT
    INT_CA --> SIGN_CRED
    INT_CA --> SIGN_PRES
    INT_CA --> SIGN_REV
```

### 2.2 Cryptographic Operations

```mermaid
sequenceDiagram
    participant User as User Device
    participant SDK as Wallet SDK
    participant API as API Gateway
    participant KMS as Key Management
    participant HSM as HSM
    participant Store as Encrypted Storage
    
    Note over User,Store: Credential Storage Flow
    User->>SDK: Store Credential
    SDK->>SDK: Generate ephemeral key
    SDK->>API: Request storage (encrypted)
    API->>KMS: Request DEK
    KMS->>HSM: Generate DEK
    HSM-->>KMS: Wrapped DEK
    KMS-->>API: DEK (encrypted with KEK)
    API->>API: Encrypt credential with DEK
    API->>Store: Store encrypted credential
    Store-->>API: Confirmation
    API-->>SDK: Storage receipt
    SDK-->>User: Success
    
    Note over User,Store: Credential Signing Flow
    User->>SDK: Issue Credential
    SDK->>API: Signing request
    API->>KMS: Request signing
    KMS->>HSM: Sign operation
    HSM->>HSM: Sign with private key
    HSM-->>KMS: Signature
    KMS-->>API: Signed credential
    API-->>SDK: Credential + signature
    SDK-->>User: Signed credential
```

### 2.3 Cryptographic Standards Compliance

| Operation | Algorithm | Key Size | Standard | Purpose |
| --- | --- | --- | --- | --- |
| **Symmetric Encryption** | AES-256-GCM | 256-bit | FIPS 197 | Data at rest |
| **Asymmetric Encryption** | RSA-OAEP | 3072-bit | PKCS#1 v2.2 | Key wrapping |
| **Digital Signatures** | ECDSA | P-256/P-384 | FIPS 186-4 | Credential signing |
| **Key Agreement** | ECDH | P-256 | SP 800-56A | Secure channels |
| **Hashing** | SHA-256/SHA-384 | - | FIPS 180-4 | Integrity |
| **Key Derivation** | PBKDF2/Argon2 | - | SP 800-132 | Password-based |
| **Random Generation** | CTR_DRBG | 256-bit | SP 800-90A | Nonces, IVs |
| **Zero-Knowledge** | zk-SNARKs | - | Research | Privacy (Future) |

---

## 3. Identity and Access Management

### 3.1 Authentication Architecture

```mermaid
graph LR
    subgraph "User Authentication"
        BIO[Biometric<br/>Face/Touch ID]
        PIN[Device PIN/Pattern]
        FIDO[FIDO2/WebAuthn]
    end
    
    subgraph "Federation"
        IDX[WA Identity Exchange]
        OIDC[OIDC Provider]
        SAML[SAML 2.0]
    end
    
    subgraph "Token Management"
        ACCESS[Access Token<br/>15 min TTL]
        REFRESH[Refresh Token<br/>7 days TTL]
        ID_TOKEN[ID Token<br/>Signed JWT]
    end
    
    subgraph "Authorization"
        RBAC2[Role-Based<br/>Admin, Issuer, Verifier]
        ABAC[Attribute-Based<br/>Tenant, Resource]
        CLAIMS[Claims-Based<br/>Permissions]
    end
    
    BIO --> IDX
    PIN --> IDX
    FIDO --> IDX
    IDX --> OIDC
    OIDC --> ACCESS
    OIDC --> REFRESH
    OIDC --> ID_TOKEN
    ACCESS --> RBAC2
    ID_TOKEN --> ABAC
    ABAC --> CLAIMS
```

### 3.2 Access Control Matrix

| Role | Wallet Operations | Credential Operations | Admin Operations | Audit Access |
| --- | --- | --- | --- | --- |
| **Citizen** | Read/Write Own | Present Own | None | Own History |
| **Issuer** | None | Issue/Revoke | Manage Templates | Issue History |
| **Verifier** | None | Verify | None | Verification Logs |
| **Admin** | None | None | All Config | Full Audit |
| **Support** | Read Only | Read Status | Read Config | Read Logs |
| **Security** | None | None | Security Config | All Logs |

---

## 4. Privacy and Data Protection

### 4.1 Privacy Architecture

```mermaid
graph TD
    subgraph "Data Minimization"
        COLLECT[Minimal Collection]
        PURPOSE[Purpose Limitation]
        RETENTION[Time-Limited Retention]
    end
    
    subgraph "Privacy Techniques"
        PSEUDO[Pseudonymization<br/>UUID identifiers]
        ANON[Anonymization<br/>Statistical data]
        SELECTIVE[Selective Disclosure<br/>Attribute selection]
        ZKP[Zero-Knowledge Proofs<br/>Future capability]
    end
    
    subgraph "User Control"
        CONSENT[Explicit Consent]
        ACCESS_RIGHT[Access Rights]
        PORTABILITY[Data Portability]
        ERASURE[Right to Erasure]
    end
    
    subgraph "Protection Measures"
        ENCRYPT[Encryption]
        SEGREGATE[Data Segregation]
        AUDIT_PRIVACY[Privacy Audits]
    end
    
    COLLECT --> PSEUDO
    PURPOSE --> ANON
    RETENTION --> SELECTIVE
    SELECTIVE --> ZKP
    PSEUDO --> CONSENT
    ANON --> ACCESS_RIGHT
    SELECTIVE --> PORTABILITY
    ZKP --> ERASURE
    CONSENT --> ENCRYPT
    ACCESS_RIGHT --> SEGREGATE
    PORTABILITY --> AUDIT_PRIVACY
```

### 4.2 Data Classification and Handling

| Classification | Examples | Encryption | Retention | Access |
| --- | --- | --- | --- | --- |
| **Highly Sensitive** | Biometrics, Medical | AES-256-GCM + HSM | 90 days | Need-to-know |
| **Sensitive** | PII, Credentials | AES-256-GCM | 7 years | Role-based |
| **Internal** | Configs, Logs | AES-256 | 2 years | Authorized staff |
| **Public** | Schemas, Docs | Optional | Indefinite | Public |

### 4.3 Privacy Compliance

| Requirement | Implementation | Validation |
| --- | --- | --- |
| **GDPR Article 5** | Data minimization by design | Privacy audit |
| **Privacy Act 1988** | Australian data residency | Compliance review |
| **APP Guidelines** | Privacy policy, consent flows | Legal review |
| **TDIF Privacy** | Attribute disclosure controls | TDIF assessment |
| **ISO 29100** | Privacy framework implementation | ISO audit |

---

## 5. Compliance Framework

### 5.1 Standards Compliance Matrix

| Category | Standard | Status | Certification Target | Evidence |
| --- | --- | --- | --- | --- |
| **Digital Identity** | W3C VC 2.0 | ✅ Compliant | Immediate | Test suite passed |
| **Mobile Credentials** | ISO/IEC 18013-5 | 🔄 In Progress | End of Pilot | Conformance testing |
| **Wallet Interop** | ISO/IEC 23220 | 🔄 In Progress | Month 6 | Interop testing |
| **Security** | ISO 27001 | ✅ Compliant | Month 3 | Certification |
| **Cryptography** | FIPS 140-2 | ✅ Compliant | Immediate | HSM certified |
| **Privacy** | ISO 29100 | ✅ Compliant | Month 2 | Assessment report |
| **Australian** | TDIF | 🔄 In Progress | Month 9 | Accreditation path |
| **Accessibility** | WCAG 2.1 AA | ✅ Compliant | Immediate | Audit report |

### 5.2 Regulatory Compliance

```mermaid
graph LR
    subgraph "Australian Requirements"
        PRIV_ACT[Privacy Act 1988]
        IRAP[IRAP Assessment]
        PSPF[PSPF Compliance]
        ISM[ISM Controls]
    end
    
    subgraph "International Standards"
        GDPR[GDPR Equivalent]
        EIDAS[eIDAS 2.0 Ready]
        PCI[PCI DSS]
    end
    
    subgraph "Industry Standards"
        SOC2[SOC 2 Type II]
        ISO27K[ISO 27001/27002]
        CSA[CSA CCM]
    end
    
    subgraph "Validation"
        AUDIT_EXT[External Audits]
        ASSESS[Self-Assessment]
        CERT[Certifications]
    end
    
    PRIV_ACT --> AUDIT_EXT
    IRAP --> AUDIT_EXT
    GDPR --> ASSESS
    SOC2 --> CERT
    ISO27K --> CERT
```

---

## 6. Threat Model and Risk Assessment

### 6.1 STRIDE Threat Analysis

```mermaid
graph TD
    subgraph "Threats"
        S[Spoofing<br/>Fake identities]
        T[Tampering<br/>Credential modification]
        R[Repudiation<br/>Denying transactions]
        I[Information Disclosure<br/>Data breaches]
        D[Denial of Service<br/>System unavailability]
        E[Elevation of Privilege<br/>Unauthorized access]
    end
    
    subgraph "Mitigations"
        AUTH[Strong Auth<br/>MFA, Biometrics]
        INTEGRITY[Integrity Checks<br/>Digital signatures]
        AUDIT_LOG[Audit Logging<br/>Immutable ledger]
        ENCRYPT2[Encryption<br/>At rest & transit]
        SCALE[Auto-scaling<br/>Rate limiting]
        AUTHZ[Authorization<br/>Least privilege]
    end
    
    S --> AUTH
    T --> INTEGRITY
    R --> AUDIT_LOG
    I --> ENCRYPT2
    D --> SCALE
    E --> AUTHZ
```

### 6.2 Attack Surface Analysis

| Component | Attack Vectors | Risk Level | Mitigations |
| --- | --- | --- | --- |
| **Mobile SDK** | App tampering, Reverse engineering | High | Code obfuscation, Certificate pinning |
| **API Gateway** | DDoS, Injection attacks | High | WAF, Rate limiting, Input validation |
| **Database** | SQL injection, Data theft | Critical | Parameterized queries, Encryption |
| **HSM/Keys** | Key extraction, Side-channel | Critical | Hardware security, Access controls |
| **Admin Portal** | Privilege escalation | High | MFA, Audit logging, PIM |
| **Integration Points** | Man-in-middle, API abuse | Medium | mTLS, API keys, Monitoring |

### 6.3 Security Risk Register

| Risk ID | Description | Likelihood | Impact | Risk Score | Treatment |
| --- | --- | --- | --- | --- | --- |
| SEC-001 | Credential forgery | Low | Critical | High | Cryptographic signatures |
| SEC-002 | Data breach | Medium | Critical | High | Encryption, access controls |
| SEC-003 | DDoS attack | High | Medium | High | Auto-scaling, CDN |
| SEC-004 | Insider threat | Low | High | Medium | Audit, segregation |
| SEC-005 | Supply chain attack | Medium | High | High | SCA, vendor assessment |
| SEC-006 | Quantum computing threat | Low | Critical | Medium | Crypto-agility plan |

---

## 7. Incident Response and Recovery

### 7.1 Incident Response Process

```mermaid
stateDiagram-v2
    [*] --> Detection: Security Event
    Detection --> Triage: Alert Triggered
    Triage --> Containment: Confirmed Incident
    Triage --> [*]: False Positive
    Containment --> Investigation: Isolated
    Investigation --> Remediation: Root Cause Found
    Remediation --> Recovery: Fix Applied
    Recovery --> Lessons: Service Restored
    Lessons --> [*]: Improvements Made
    
    note right of Detection: 24/7 SOC Monitoring
    note right of Triage: 15 min SLA
    note right of Containment: 1 hour SLA
    note right of Recovery: 4 hour RTO
```

### 7.2 Business Continuity Planning

| Scenario | RTO | RPO | Strategy | Test Frequency |
| --- | --- | --- | --- | --- |
| **Data Center Failure** | 4 hours | 15 minutes | Failover to DR site | Quarterly |
| **Database Corruption** | 2 hours | 1 hour | Point-in-time recovery | Monthly |
| **Ransomware Attack** | 8 hours | 4 hours | Isolated backups | Bi-annual |
| **Key Compromise** | 1 hour | 0 minutes | Key rotation | Annual |
| **Supply Chain Breach** | 24 hours | N/A | Alternative providers | Annual |

### 7.3 Disaster Recovery Architecture

```mermaid
graph LR
    subgraph "Primary Site - AU East"
        PROD[Production Systems]
        PROD_DB[(Primary Database)]
        PROD_HSM[Primary HSM]
    end
    
    subgraph "DR Site - AU Southeast"
        DR[DR Systems<br/>Warm Standby]
        DR_DB[(Replica Database)]
        DR_HSM[Backup HSM]
    end
    
    subgraph "Backup Site - AU Central"
        BACKUP[Offline Backups]
        COLD[Cold Storage]
    end
    
    PROD -->|Sync Replication| DR
    PROD_DB -->|Async Replication| DR_DB
    PROD_HSM -->|Key Backup| DR_HSM
    DR -->|Daily Backup| BACKUP
    DR_DB -->|Archive| COLD
```

---

## 8. Security Operations

### 8.1 Security Monitoring Architecture

```mermaid
graph TB
    subgraph "Data Sources"
        APPS[Application Logs]
        INFRA[Infrastructure Logs]
        SECURITY[Security Events]
        AUDIT2[Audit Trails]
    end
    
    subgraph "Collection"
        AGENT[Log Agents]
        API_LOG[API Gateway Logs]
        FLOW[Network Flow Logs]
    end
    
    subgraph "Processing"
        SIEM[Azure Sentinel]
        ANALYTICS[Log Analytics]
        ML[ML Threat Detection]
    end
    
    subgraph "Response"
        ALERT[Alert Management]
        TICKET[Incident Tickets]
        AUTO[Automated Response]
    end
    
    subgraph "Reporting"
        DASH[Security Dashboard]
        REPORT[Compliance Reports]
        METRIC[KPI Metrics]
    end
    
    APPS --> AGENT
    INFRA --> AGENT
    SECURITY --> API_LOG
    AUDIT2 --> FLOW
    AGENT --> SIEM
    API_LOG --> SIEM
    FLOW --> SIEM
    SIEM --> ANALYTICS
    ANALYTICS --> ML
    ML --> ALERT
    ALERT --> TICKET
    ALERT --> AUTO
    TICKET --> DASH
    AUTO --> REPORT
    REPORT --> METRIC
```

### 8.2 Security Testing Schedule

| Test Type | Frequency | Scope | Performed By |
| --- | --- | --- | --- |
| **Vulnerability Scanning** | Weekly | All systems | Automated |
| **Penetration Testing** | Pilot: 1x, Prod: 2x/year | External facing | Third party |
| **Security Code Review** | Per release | New code | Dev team + tools |
| **Compliance Audit** | Annual | Full platform | External auditor |
| **DR Testing** | Bi-annual | Failover procedures | Operations team |
| **Social Engineering** | Annual | Staff awareness | Security team |
| **Red Team Exercise** | Annual (Production) | Full scope | Specialized firm |

### 8.3 Security Metrics and KPIs

| Metric | Target | Current | Status |
| --- | --- | --- | --- |
| **Mean Time to Detect (MTTD)** | <15 minutes | - | 🎯 |
| **Mean Time to Respond (MTTR)** | <1 hour | - | 🎯 |
| **Patch Compliance** | >95% within 30 days | - | 🎯 |
| **Security Training Completion** | 100% annually | - | 🎯 |
| **Critical Vulnerabilities** | 0 unpatched >7 days | - | 🎯 |
| **Failed Login Attempts** | <5% of total | - | 🎯 |
| **Encryption Coverage** | 100% sensitive data | - | 🎯 |

---

## Summary

This comprehensive security, privacy, and compliance framework ensures:

1. **Defense-in-depth** security across all layers
2. **Strong cryptography** with HSM-backed key management
3. **Privacy by design** with data minimization and user control
4. **Full compliance** with Australian and international standards
5. **Robust incident response** with defined RTO/RPO targets
6. **Continuous monitoring** and improvement

The security architecture supports the $1,866,250 pilot budget with appropriate controls while providing a clear path to production-grade security.

---
[Back to Master PRD](./PRD_Master.md) | [Next: Technical Specification](./Appendix_C_Technical_Specification.md)