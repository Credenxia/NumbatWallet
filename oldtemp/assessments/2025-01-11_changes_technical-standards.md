# Change Pack: technical-standards
Run: 2025-01-11 16:05:00 AWST  
Commit: a4c4931

## Wiki Edits

### 1. Global Platform Name Change

- **All Wiki Files**
  Operation: Replace throughout
  Find (exact): "Credenxia v2"
  With (exact): "CredEntry"
  Rationale: CredEntry is the authoritative platform name from input documents

### 2. Security-Privacy-Compliance.md - Add Technical Standards & Vulnerability SLAs

- File: `/Users/rodrigolmiranda/repo/NumbatWallet.wiki/Security-Privacy-Compliance.md`
  Section Anchor: "### 1.3 Vulnerability Management"
  Operation: Append after "#### Remediation Targets" table
  Find (exact):
  """
  | **Low** | 0.1-3.9 | Next release cycle | Risk accepted |
  """
  With (exact):
  """
  | **Low** | 0.1-3.9 | Next release cycle | Risk accepted |

  ### 1.4 Technical Standards Compliance (CredEntry Implementation)

  #### TS-1: Data Protection Measures
  **Implementation:** All PII and credential data encrypted using AES-256. Keys generated, stored, and rotated within Azure Key Vault HSMs (FIPS 140-3). All data transfers use TLS 1.3 with Perfect Forward Secrecy. SHA-256 hash signatures confirm immutability. Immutable audit logging via Azure Monitor/Sentinel.

  #### TS-2: Secure Communication Channels  
  **Implementation:** APIs exposed exclusively via HTTPS/TLS 1.3. Mutual TLS (mTLS) required for certificate authority operations and key material distribution. Modern cipher suites only (AES-GCM, CHACHA20-POLY1305). Application-layer encryption for biometrics and government identifiers.

  #### TS-3: Multi-Factor Authentication
  **Implementation:** Azure AD Conditional Access enforces MFA for all administrative access. Supported methods: FIDO2/WebAuthn, Microsoft Authenticator, certificate-based authentication. Step-up authentication for key management and issuance. Meets Digital ID Authentication Level 2.

  #### TS-4: Data Minimisation and Purpose Limitation
  **Implementation:** Zero-knowledge proof (ZKP) selective disclosure in wallet/verifier protocols. Explicit user consent screens before credential sharing. Purpose-bound API access controls. Role-based least-privilege access.

  #### TS-5: PKI Management  
  **Implementation:** Multi-tenant PKI with Issuer Authority Certificate Authorities (IACAs) per entity isolated in Azure HSMs. Automated certificate lifecycle with CRL/OCSP support. Document Signing Certificates comply with ISO/IEC 18013-5 and 23220.

  #### TS-6: Standardised Data Elements and Offline Presentation
  **Implementation:** Credentials follow ISO/IEC 18013-5 and 23220 schemas. Embedded digital signatures enable offline verification via QR codes or NFC. Selective disclosure preserved in offline mode.

  #### TS-7: Issuance APIs (OID4VCI)
  **Implementation:** All credential issuance follows OID4VCI standard. SDKs provide secure key binding and nonce validation. Formal conformance testing scheduled during Pilot Phase.

  #### TS-8: Presentation APIs (OIDC4VP)
  **Implementation:** Supports end-to-end OIDC4VP credential presentation flows. APIs require mutual authentication and enforce purpose-limited claims. Conformance validation during Pilot Phase.

  #### TS-9: User Transaction Log and Dashboard
  **Implementation:** Comprehensive dashboard showing all credential lifecycle events with timestamps. GDPR-compliant data erasure requests. Suspicious activity reporting with automated alerts. WCAG 2.2+ compliant interface.

  #### TS-10: Digital ID (Accreditation) Rules 2024
  **Implementation:** System policies reflect Chapter 4 requirements. Progressive compliance with full validation during Pilot Phase.

  #### TS-12: Mutable Credential Fields
  **Implementation:** Remote attribute updates supported without full reissuance where feasible. All updates cryptographically signed and audit logged per ISO/IEC 18013-5.

  #### TS-13: Adaptability to Evolving Standards
  **Implementation:** Modular architecture enables independent service updates. Versioned APIs/SDKs with backward compatibility. Standards tracking for ISO/IEC 23220-3/4.
  """
  Rationale: Add missing TS-1 through TS-13 technical standards from input

### 3. SDK-Documentation.md - Add Platform SDK Standards & OSI Licensing

- File: `/Users/rodrigolmiranda/repo/NumbatWallet.wiki/SDK-Documentation.md`
  Section Anchor: "### 1.2 Feature Matrix"
  Operation: Append after table
  Find (exact):
  """
  | QR Scanning | ✅ | ❌ | ✅ | 🔄 | 🔄 |
  """
  With (exact):
  """
  | QR Scanning | ✅ | ❌ | ✅ | 🔄 | 🔄 |

  ### 1.3 Platform SDK Standards (CredEntry Implementation)

  #### PS-1: Wallet Integration SDK
  **Status:** ✅ Fully documented SDK with APIs for issuance, presentation, revocation, and lifecycle operations. Demo-ready with developer onboarding packs.

  #### PS-2: Cryptographic Binding to Secure Area
  **Status:** ✅ Device secure elements (TEE, Secure Enclave, Android Keystore) leveraged for ISO/IEC 18013-5 compliant credential binding with non-repudiation.

  #### PS-3: Separation of Concerns
  **Status:** ✅ Clean architecture principles with core wallet functionality encapsulated separately from integration logic. Compliant with Digital ID Accreditation Rules 2024.

  #### PS-4: Well-Documented APIs and Extension Points
  **Status:** ✅ Comprehensive API documentation with code samples. Extension hooks for custom DID methods and credential schemas.

  #### PS-5: Automated Integration Tests
  **Status:** ✅ Test harness covering issuance, presentation, revocation, and selective disclosure for multiple credential types. CI/CD integration.

  #### PS-6: Automated Security Scanning
  **Status:** ✅ GitHub Advanced Security with SAST/DAST integrated. Security alerts tracked with defined SLAs.

  #### PS-7: Release Management and Updates
  **Status:** ✅ Documented release process with changelogs, migration guides, and backward compatibility considerations.

  #### PS-8: Selective Disclosure and User Transparency
  **Status:** ✅ UI components display verifier identity and requested attributes. Explicit approval per ISO/IEC 29100 privacy principles.

  #### PS-9: Inter-Jurisdictional Use Cases
  **Status:** ✅ ISO/IEC 18013-5/7 conformance enables nationwide credential validation.

  #### PS-10: Background Activities and Push Notifications
  **Status:** ✅ Native push notifications (APNS/FCM) with background sync for credential updates. Secure notification without data exposure.

  #### PS-11: SLA Framework for SDKs
  **Status:** ✅ Release cadence, vulnerability remediation timeframes aligned with ACSC Secure by Design.

  ### ⚠️ Critical Note: TS-11 OSI Licensing Requirement

  **Requirement:** Platform SDKs must be under an OSI-approved open-source license.
  
  **Current Status:** SDKs are provided to authorised integrators with full technical documentation, integration guides, and ongoing updates. However, **SDKs are NOT yet under an OSI-approved license**.
  
  **Commitment:** 
  - Open licensing options are actively under review
  - Full SDK access guaranteed to the Department and development partners for contract duration
  - Roadmap for OSI compliance to be provided during contract negotiation
  """
  Rationale: Add PS-1 through PS-11 and critical OSI licensing disclosure (mandatory requirement)

### 4. Support-Model.md - Add Compliance Requirements & Vulnerability SLAs

- File: `/Users/rodrigolmiranda/repo/NumbatWallet.wiki/Support-Model.md`
  Section Anchor: "## Service level objectives (SLOs)"
  Operation: Insert before section
  Find (exact):
  """
  ## Service level objectives (SLOs)
  """
  With (exact):
  """
  ## Compliance and Reporting Requirements (CredEntry)

  ### CR-1: ISO/IEC 18013 & ISO/IEC 23220 Conformance
  - Quarterly automated conformance testing integrated into CI/CD pipeline
  - Test harnesses aligned with ISO/IEC 18013-5 and ISO/IEC 23220 standards
  - Periodic compliance reports with deviation tracking under QMS

  ### CR-2: eIDAS 2.0 Conformance Testing
  - Architected for European Wallet Conformance (EWC) RFC100 profiles
  - Conformance testing during Pilot Phase with annual retesting
  - Independent auditor review and authority submission

  ### CR-3: Cyber Security Incident Reporting
  - 24-hour reporting aligned with WA Cyber Security Policy (2024)
  - Real-time SIEM monitoring with automated alerting
  - Post-incident reviews with root cause analysis

  ### CR-4: Maintenance of Information Security Certifications
  - Current: ISO/IEC 27001 (maintaining)
  - Roadmap: SOC 2 Type 2, ACSC IRAP assessment
  - Annual renewal commitment for contract duration

  ### CR-5: Entity Information Security
  - AES-256 encryption at rest, TLS 1.3 in transit
  - RBAC with least-privilege principles
  - Full audit logging and ISO/IEC 27001 compliance

  ### CR-6: Secure Disposal or Return of Information
  - NIST 800-88 compliant data sanitization
  - Secure encrypted export on termination
  - Disposal certificates issued

  ### CR-7: SLA Service Credits
  - Automatic credits for SLA breaches
  - Quarterly SLA performance reporting
  - (Details in Service Credits section below)

  ### CR-8: SLA Framework for Incident Response
  - ITIL 4 aligned framework
  - Severity levels: Critical/High/Medium/Low
  - Response times: 1 hour (Critical) to 2 days (Low)
  - Critical vulnerability patches within 48 hours

  ### Vulnerability Remediation SLAs

  | CVSS Score | Severity | Remediation Timeline | Compensating Controls |
  |------------|----------|---------------------|----------------------|
  | 9.0-10.0 | Critical | ≤5 business days | Within 24 hours |
  | 7.0-8.9 | High | ≤10 business days | Within 48 hours |
  | 4.0-6.9 | Medium | ≤30 business days | As required |
  | 0.1-3.9 | Low | Next release cycle | Risk accepted |

  All vulnerabilities logged in Azure DevOps with monthly status reports, quarterly trend analysis, and annual security assessment.

  ## Service level objectives (SLOs)
  """
  Rationale: Add CR-1 through CR-8 and vulnerability remediation timelines from input

### 5. Technical-Specification.md - Add Credential Management & User Dashboard

- File: `/Users/rodrigolmiranda/repo/NumbatWallet.wiki/Technical-Specification.md`
  Section Anchor: "## 3. Credential Lifecycle State Machine"
  Operation: Insert before section
  Find (exact):
  """
  ## 3. Credential Lifecycle State Machine
  """
  With (exact):
  """
  ### 2.5 Platform Credential Management (CredEntry)

  #### PCR-1: Event-Driven Credential Issuance and Storage
  **Implementation:** Webhook subscriptions trigger automatic credential generation and secure storage. Real-time responsiveness aligned with ISO/IEC 23220-2 and W3C VC event flows.

  #### PCR-2: Credential Revocation Polling and Updates
  **Implementation:** Hybrid approach with webhook notifications for real-time updates plus periodic polling for redundancy. Dual mechanism ensures validity per ISO/IEC 23220-2.

  #### PCR-3: In-Place Attribute Updates
  **Implementation:** Selective attribute updates without full reissuance where feasible. Cryptographically bound attributes require reissuance per eIDAS 2.0 and ISO/IEC 18013-5.

  #### PCR-4: Rapid Credential Updates and Revocations
  **Implementation:** Changes propagated to online wallets within 5 minutes. W3C Verifiable Credentials Data Model compliance maintained.

  #### PCR-5: Delegated Credential Use
  **Implementation:** User-controlled consent and selective disclosure currently enabled. Delegated use cases (guardianship, power of attorney) planned for future releases with W3C VC and GDPR alignment.

  #### PCR-6: Configurable Issuance Flows and PII Storage
  **Implementation:** Issuance workflows minimize data exposure through encryption. Roadmap includes "zero-PII" flows where sensitive data remains solely with issuer per ISO/IEC 18013-5/23220.

  #### PCR-7: Credential Refresh Mechanism
  **Implementation:** Wallets automatically refresh attributes when issuers publish updates. Event-driven webhooks and scheduled polling ensure accuracy per ISO/IEC 23220-2.

  ### 2.6 User Transaction Dashboard (TS-9)

  **Purpose:** Provide users with comprehensive, accessible transaction log enabling data control and GDPR compliance.

  **Features:**
  - Complete history of credential issuance, presentation, and verification events
  - Timestamps and verifier identities for all transactions
  - GDPR-compliant "right to erasure" request interface
  - Suspicious activity reporting with automated escalation
  - WCAG 2.2+ compliant accessibility

  **Implementation:**
  - Real-time transaction logging in append-only audit store
  - User-facing dashboard in wallet application
  - Administrative interface for erasure request processing
  - Integration with incident response workflows

  ## 3. Credential Lifecycle State Machine
  """
  Rationale: Add PCR-1 through PCR-7 and new TS-9 user dashboard requirement

### 6. API-Documentation.md - Add Platform API Standards

- File: `/Users/rodrigolmiranda/repo/NumbatWallet.wiki/API-Documentation.md`
  Section Anchor: "### 1.2 API Standards"
  Operation: Append after table
  Find (exact):
  """
  | ISO 8601 | Date/time format | Consistency |
  """
  With (exact):
  """
  | ISO 8601 | Date/time format | Consistency |

  ### 1.3 Platform API Requirements (CredEntry)

  #### PA-1: OpenAPI Documentation and Input Validation
  - All APIs documented in OpenAPI 3.0 format with versioned specifications
  - Comprehensive input validation at schema and application layers
  - Privileged access protected using RBAC and OAuth 2.0 with fine-grained scopes

  #### PA-2: API Testing and Security Flaws Coverage
  - Automated testing covering positive/negative use cases
  - Security testing aligned to OWASP ASVS and API Security Top 10
  - Continuous SAST/DAST scanning in CI/CD pipelines

  #### PA-3: API Segregation and Privilege Separation
  - APIs grouped by purpose (issuance, presentation, trust, revocation)
  - Scoped permissions per API function
  - Role-based separation for sensitive endpoints

  #### PA-4: Accessible Web Interface for Credential Issuance
  - WCAG 2.2 AA compliant portal for authorised users
  - Manual issuance, API integration, and OIDC claims support
  - ISO/IEC 18013-7 and OID4VCI workflow compliance

  #### PA-5: Support for In-Person and Remote Verification
  - In-person: QR code scanning and NFC-based verification
  - Remote: Secure links and OIDC4VP-compliant API calls
  - End-to-end cryptographic verification

  #### PA-6: Credential Status Interfaces
  - Real-time status API (Active/Suspended/Revoked)
  - Compliant with W3C VC, ISO/IEC 18013-5, ISO/IEC 23220-2

  #### PA-7: Digital Trust Service Configuration
  - Trust Registry for Issuers, Wallet Providers, and Verifiers
  - Certificate filtering by attributes and fingerprints
  - Aligned with ISO/IEC 18013-5 and ISO/IEC 23220

  #### PA-8: Export in Open and Interoperable Formats
  - JSON-LD, XML, and CSV export formats
  - Cryptographically signed exports
  - WA Cyber Security Policy (2024) compliance
  """
  Rationale: Add PA-1 through PA-8 API requirements from input

### 7. Solution-Architecture.md - Add Multi-Tenancy Standards

- File: `/Users/rodrigolmiranda/repo/NumbatWallet.wiki/Solution-Architecture.md`
  Section Anchor: "### 1.2 Data Storage Strategy"
  Operation: Append after table
  Find (exact):
  """
  | Session Data | Redis | TLS only | 4 hours | None |
  """
  With (exact):
  """
  | Session Data | Redis | TLS only | 4 hours | None |

  ### 1.3 Platform Multi-Tenancy Standards (CredEntry)

  #### PM-1: Partitioning into PKI and Identity Containers
  **Implementation:** Logical partitioning into independent PKI and Identity containers per tenant. Each container provides isolated trust boundaries ensuring data and cryptographic material separation. Aligned with eIDAS 2.0 and ISO/IEC 27001/27002.

  #### PM-2: Separate PKI Configuration
  **Implementation:** Each tenant container configures its own PKI including certificate authorities, trust anchors, and cryptographic policies. Independent key lifecycles and governance while maintaining compliance.

  #### PM-3: Separate Identity Provider Configuration  
  **Implementation:** Container-specific IdP configuration supporting unique OIDC/SAML providers per tenant. Logical isolation maintained across all tenants.

  #### PM-4: Container-Specific Branding and Customisation
  **Implementation:** Independent branding, theming, and interface customization at container level. Organisations deliver tailored user experience while maintaining OWASP ASVS compliance.
  """
  Rationale: Add PM-1 through PM-4 multi-tenancy requirements

### 8. Compliance-Matrix.md - Add Certification Timeline

- File: `/Users/rodrigolmiranda/repo/NumbatWallet.wiki/Compliance-Matrix.md`
  Section Anchor: Beginning of file
  Operation: Insert at beginning
  Find (exact):
  """
  # Appendix J – Regulatory & Requirement Mapping
  """
  With (exact):
  """
  # Appendix J – Regulatory & Requirement Mapping

  ## Certification Roadmap (CredEntry - As of September 2025)

  | Standard/Framework | Current Status | Gap | Target | Timeline |
  |-------------------|----------------|-----|---------|----------|
  | ISO/IEC 27001:2022 | Gap analysis complete | 30% | **Certified** | Month 6 |
  | SOC 2 Type 2 | Not started | 100% | **Certified** | Month 12 |
  | IRAP | Not started | 100% | **Assessed** | Month 4 |
  | ISO/IEC 18013-5:2021 | Self-assessment done | 15% | Compliant | Month 3 |
  | TDIF 4.8 | Preliminary review | 40% | Accredited | Month 9 |
  | GDPR/Privacy Act | Partially compliant | 20% | Compliant | Month 2 |
  | Digital ID Rules 2024 | Review complete | 35% | Compliant | Month 8 |
  | eIDAS 2.0 | Conformance testing planned | - | Compliant | Pilot End |
  | ISO/IEC 23220 | Conformance testing planned | - | Compliant | Pilot End |

  **Note:** Only ISO 27001, SOC 2, and IRAP will achieve formal certification/assessment. All other standards will meet compliance requirements but will not pursue formal certification.

  """
  Rationale: Add accurate certification timeline per baseline assessment

### 9. Deployment-Guide.md - Add Release Management Process

- File: `/Users/rodrigolmiranda/repo/NumbatWallet.wiki/Deployment-Guide.md`
  Section Anchor: End of file (if PRM-1 not already present)
  Operation: Append
  Find (exact): [Check if PRM-1 exists, if not append]
  With (exact):
  """
  
  ## Release and Onboarding Management (PRM-1)

  CredEntry follows a structured 7-step onboarding process aligned to ISO 9001 and ISO/IEC 27001:

  ### 1. Onboarding Preparation
  - Discovery workshops to establish requirements
  - Compliance obligations confirmation
  - Dedicated team allocation (PM, architect, security lead)
  - Formal onboarding plan with acceptance criteria

  ### 2. Environment Provisioning
  - Deploy segregated environments (Dev/Staging/Prod) in Azure AU
  - Apply baseline security controls
  - Configure multi-tenancy and PKI/identity partitions

  ### 3. Platform Configuration
  - Customise IdPs, PKI, credential schemas, branding
  - Implement RBAC and segregation of duties
  - API/connector integration with customer systems
  - Validate against configuration checklists

  ### 4. Testing and Validation
  - Functional testing (issuance, revocation, wallet)
  - Security validation (penetration testing, scanning)
  - Conformance testing (ISO/IEC 18013, 23220, eIDAS 2.0)
  - Document and remediate issues

  ### 5. Training and Knowledge Transfer
  - Administrator and end-user training
  - Operational guides and runbooks delivery
  - Security/compliance team incident response training

  ### 6. Go-Live and Production Readiness
  - Go-Live Readiness Review with stakeholders
  - Validate migration and rollback procedures
  - Change management board approval
  - Transition to steady-state operations

  ### 7. Post-Go-Live Support
  - Hypercare period monitoring (4-6 weeks)
  - Lessons learned capture
  - Ongoing patching and feature upgrades
  - Compliance and certification maintenance
  """
  Rationale: Add PRM-1 7-step onboarding process if not present

## Cross-Propagation Updates

The following files should also be reviewed for consistency:
- **Home.md** - Update platform name to CredEntry (keep .NET versions as-is)
- **POA-Plan.md** - Confirm .NET 9 for POA phase
- **Risk-Matrix.md** - Add OSI licensing risk (HIGH)
- **Testing-Strategy.md** - Add conformance testing schedules
- **Tender-Submission-Requirements.md** - Update certification claims

## Implementation Priority

1. **CRITICAL:** Add OSI licensing disclosure to SDK-Documentation.md (TS-11 mandatory)
2. **CRITICAL:** Update certification timeline in Compliance-Matrix.md
3. **HIGH:** Add vulnerability remediation SLAs to Support-Model.md
4. **HIGH:** Add all technical standards to Security-Privacy-Compliance.md
5. **MEDIUM:** Update platform name to CredEntry throughout Wiki
6. **MEDIUM:** Add user transaction dashboard to Technical-Specification.md
7. **LOW:** Add detailed multi-tenancy and API requirements

## Validation Checklist

- [ ] TS-11 OSI licensing requirement clearly documented with non-compliance disclosed
- [ ] All TS-1 through TS-13 technical standards added
- [ ] All PS-1 through PS-11 SDK standards documented
- [ ] All CR-1 through CR-8 compliance requirements added
- [ ] Vulnerability remediation SLAs added (Critical ≤5 days, etc.)
- [ ] User transaction dashboard (TS-9) documented
- [ ] All PM-1 through PM-4 multi-tenancy standards added
- [ ] All PA-1 through PA-8 API requirements documented
- [ ] All PCR-1 through PCR-7 credential requirements added
- [ ] Certification roadmap accurate (ISO 27001, SOC2, IRAP only certified)
- [ ] Platform name updated to CredEntry throughout
- [ ] .NET versions kept as-is (input: .NET 9, Wiki: .NET 10)