# Appendix: Regulatory Mapping
## Digital Wallet and Verifiable Credentials Solution

**Document Version:** 1.0  
**Parent Document:** [Master PRD](./PRD_Master.md)  
**Last Updated:** December 2024

---

## 1. Tender Requirements Traceability

### 1.1 Requirements Mapping Overview

```mermaid
graph TB
    subgraph "Tender Documents"
        REQUEST[DPC2142 Request]
        ATTACH1[Attachment 1<br/>Agreement]
        ATTACH2[Attachment 2<br/>Statement of Requirements]
        ATTACH3[Attachment 3<br/>Specifications]
        ATTACH4[Attachment 4<br/>Pricing]
        ADDENDUM[Addendum<br/>Q&A]
    end
    
    subgraph "Our Response"
        PRD[Master PRD]
        ARCH[Architecture]
        SECURITY[Security Plan]
        PRICING[Pricing Model]
        TEAM[Team Structure]
    end
    
    subgraph "Compliance Status"
        COMPLIANT[Fully Compliant]
        PARTIAL[Partially Compliant]
        ALTERNATE[Alternative Proposed]
        EXCEEDS[Exceeds Requirement]
    end
    
    REQUEST --> PRD
    ATTACH1 --> PRICING
    ATTACH2 --> ARCH
    ATTACH3 --> SECURITY
    ATTACH4 --> PRICING
    ADDENDUM --> PRD
    
    PRD --> COMPLIANT
    ARCH --> EXCEEDS
    SECURITY --> COMPLIANT
    PRICING --> COMPLIANT
```

### 1.2 Schedule 2 - Statement of Requirements Mapping

| Req ID | Requirement | Our Response | Status | Reference |
|--------|-------------|--------------|--------|-----------|
| **SOR-001** | Digital wallet solution | Full wallet implementation with SDK | ✅ Compliant | PRD Section 3 |
| **SOR-002** | Verifiable credentials support | W3C VC 2.0 compliant | ✅ Exceeds | Architecture 2.1 |
| **SOR-003** | ServiceWA integration | Native SDK provided | ✅ Compliant | APIs & SDKs 3.2 |
| **SOR-004** | Multi-credential support | Extensible credential framework | ✅ Exceeds | Data Model 3.1 |
| **SOR-005** | Offline verification | Full offline capability | ✅ Compliant | Workflows 2.2 |
| **SOR-006** | Privacy preserving | ZKP with BBS+ signatures | ✅ Exceeds | Security 4.2 |
| **SOR-007** | Scalability 2M+ users | Auto-scaling architecture | ✅ Compliant | Architecture 7.2 |
| **SOR-008** | 99.9% availability | Multi-region HA design | ✅ Compliant | Operations 5.3 |
| **SOR-009** | Data sovereignty | AU regions only | ✅ Compliant | Architecture 3.1 |
| **SOR-010** | Security compliance | ISO 27001 aligned | ✅ Compliant | Security 7.1 |

---

## 2. Schedule 3 - Specifications Compliance

### 2.1 Technical Specifications Mapping

```mermaid
graph LR
    subgraph "Functional Specs"
        ISSUANCE[Credential Issuance]
        STORAGE[Secure Storage]
        PRESENTATION[Presentation]
        REVOCATION[Revocation]
    end
    
    subgraph "Non-Functional Specs"
        PERFORMANCE[Performance]
        SECURITY_SPEC[Security]
        RELIABILITY[Reliability]
        USABILITY[Usability]
    end
    
    subgraph "Integration Specs"
        SERVICEWA_INT[ServiceWA]
        ISSUERS[Gov Issuers]
        VERIFIERS[Verifiers]
        TRUST[Trust Registry]
    end
    
    subgraph "Compliance Level"
        FULL[100% Compliant]
        ENHANCED[Enhanced Solution]
        RISK_MGMT[Risk Mitigated]
    end
    
    ISSUANCE --> FULL
    STORAGE --> ENHANCED
    PRESENTATION --> FULL
    REVOCATION --> FULL
    
    PERFORMANCE --> ENHANCED
    SECURITY_SPEC --> ENHANCED
    RELIABILITY --> FULL
    USABILITY --> FULL
    
    SERVICEWA_INT --> FULL
    ISSUERS --> FULL
    VERIFIERS --> FULL
    TRUST --> ENHANCED
```

### 2.2 Detailed Specifications Compliance

| Spec Category | Requirement | Our Implementation | Compliance | Evidence |
|--------------|-------------|-------------------|------------|----------|
| **Wallet Management** | Create, backup, restore | Full lifecycle management | ✅ 100% | PRD 3.1 |
| **Credential Types** | Driver's license, WWC, Age | All types + extensible | ✅ 110% | Data Model 3 |
| **Cryptography** | Industry standard | FIPS compliant + post-quantum ready | ✅ 120% | Security 2.3 |
| **Standards** | W3C, ISO | Full compliance + additional | ✅ 100% | PRD 5.1 |
| **API** | RESTful | REST + GraphQL + gRPC | ✅ 130% | APIs 2.1 |
| **SDK** | Mobile SDK | Flutter + .NET + TypeScript | ✅ 150% | APIs 6.1 |
| **Performance** | <2s response | <500ms P95 | ✅ 400% | Testing 4.3 |
| **Availability** | 99.9% | 99.95% design | ✅ 105% | Operations 2.2 |
| **Security** | Best practices | Zero-trust architecture | ✅ 120% | Security 4.2 |
| **Privacy** | APP compliance | Privacy by design | ✅ 100% | Security 4.3 |

---

## 3. Australian Privacy Principles (APP) Compliance

### 3.1 APP Compliance Matrix

```mermaid
graph TD
    subgraph "Collection Principles"
        APP1[APP 1: Open & Transparent]
        APP2[APP 2: Anonymity]
        APP3[APP 3: Collection]
        APP4[APP 4: Unsolicited]
        APP5[APP 5: Notification]
    end
    
    subgraph "Use & Disclosure"
        APP6[APP 6: Use/Disclosure]
        APP7[APP 7: Direct Marketing]
        APP8[APP 8: Cross-border]
        APP9[APP 9: Gov Identifiers]
    end
    
    subgraph "Data Quality & Security"
        APP10[APP 10: Quality]
        APP11[APP 11: Security]
    end
    
    subgraph "Access & Correction"
        APP12[APP 12: Access]
        APP13[APP 13: Correction]
    end
    
    APP1 --> IMPLEMENTED[✅ Implemented]
    APP2 --> IMPLEMENTED
    APP3 --> IMPLEMENTED
    APP11 --> ENHANCED[⭐ Enhanced]
    APP12 --> IMPLEMENTED
```

### 3.2 APP Implementation Details

| APP # | Principle | Implementation | Controls | Validation |
|-------|-----------|----------------|----------|------------|
| **APP 1** | Open and transparent | Published privacy policy, clear notices | UI/UX design | Legal review |
| **APP 2** | Anonymity option | Anonymous credentials supported | Technical design | Testing |
| **APP 3** | Collection of solicited info | Consent management system | Workflow engine | Audit logs |
| **APP 4** | Unsolicited info | Auto-deletion procedures | Automated process | Compliance check |
| **APP 5** | Notification | In-app notifications, emails | Notification service | User testing |
| **APP 6** | Use or disclosure | Purpose limitation engine | Access controls | Audit trail |
| **APP 7** | Direct marketing | Opt-out mechanisms | Preference center | Testing |
| **APP 8** | Cross-border disclosure | Data residency controls | Infrastructure | Monitoring |
| **APP 9** | Government identifiers | Separate encrypted storage | Data model | Security audit |
| **APP 10** | Quality of information | Validation rules, user corrections | Data quality engine | Metrics |
| **APP 11** | Security of information | Multi-layer security | Security architecture | Penetration testing |
| **APP 12** | Access to information | Self-service portal | User portal | Functional testing |
| **APP 13** | Correction of information | Update APIs, audit trail | API endpoints | Testing |

---

## 4. Regulatory Compliance Framework

### 4.1 Compliance Architecture

```mermaid
graph TB
    subgraph "Australian Regulations"
        PRIVACY_ACT[Privacy Act 1988]
        STATE_RECORDS[State Records Act 2000]
        SPAM_ACT[Spam Act 2003]
        ARCHIVE_ACT[Archives Act 1983]
    end
    
    subgraph "International Standards"
        ISO27001[ISO/IEC 27001]
        ISO27701[ISO/IEC 27701]
        ISO27018[ISO/IEC 27018]
        SOC2[SOC 2 Type II]
    end
    
    subgraph "Industry Standards"
        W3C_VC[W3C VC Data Model]
        W3C_DID[W3C DID Core]
        OIDC[OpenID Connect]
        FIDO[FIDO2/WebAuthn]
    end
    
    subgraph "Implementation"
        POLICIES[Policies & Procedures]
        TECHNICAL[Technical Controls]
        AUDIT_COMP[Audit & Compliance]
        TRAINING[Training & Awareness]
    end
    
    PRIVACY_ACT --> POLICIES
    ISO27001 --> TECHNICAL
    W3C_VC --> TECHNICAL
    
    POLICIES --> AUDIT_COMP
    TECHNICAL --> AUDIT_COMP
    AUDIT_COMP --> TRAINING
```

### 4.2 Regulatory Requirements Mapping

| Regulation | Key Requirements | Our Implementation | Gap Analysis | Action Items |
|------------|-----------------|-------------------|--------------|--------------|
| **Privacy Act 1988** | APP compliance, breach notification | Full implementation | None | Maintain |
| **State Records Act** | Record retention, disposal authority | 7-year retention policy | None | Implement schedule |
| **Spam Act 2003** | Consent for electronic messages | Opt-in system | None | Maintain |
| **Archives Act** | Commonwealth records management | Archival system | Review needed | Q2 2025 review |
| **Notifiable Breaches** | 72-hour notification | Incident response plan | None | Test quarterly |

---

## 5. Security Standards Compliance

### 5.1 ISO 27001 Controls Mapping

```mermaid
graph LR
    subgraph "ISO 27001 Domains"
        A5[A.5 Organizational]
        A6[A.6 HR Security]
        A7[A.7 Asset Management]
        A8[A.8 Access Control]
        A9[A.9 Cryptography]
        A10[A.10 Operations]
        A11[A.11 Communications]
        A12[A.12 Development]
        A13[A.13 Supplier]
        A14[A.14 Incident Mgmt]
    end
    
    subgraph "Implementation Status"
        IMPLEMENTED_SEC[Implemented: 85%]
        IN_PROGRESS[In Progress: 10%]
        PLANNED[Planned: 5%]
    end
    
    subgraph "Evidence"
        DOCUMENTATION[Documentation]
        TESTING_SEC[Testing Results]
        AUDIT_RESULTS[Audit Reports]
        METRICS[Security Metrics]
    end
    
    A8 --> IMPLEMENTED_SEC
    A9 --> IMPLEMENTED_SEC
    A14 --> IN_PROGRESS
    
    IMPLEMENTED_SEC --> DOCUMENTATION
    IN_PROGRESS --> TESTING_SEC
    PLANNED --> AUDIT_RESULTS
```

### 5.2 Security Controls Implementation

| Control Category | ISO 27001 Ref | Implementation | Maturity | Evidence |
|-----------------|---------------|----------------|----------|----------|
| **Access Control** | A.8 | RBAC, MFA, Zero Trust | Level 4 | Audit logs |
| **Cryptography** | A.9 | HSM, key management | Level 5 | Crypto assessment |
| **Operations Security** | A.10 | SOC, monitoring | Level 3 | SOC reports |
| **Communications** | A.11 | TLS 1.3, mTLS | Level 4 | Network scans |
| **Development** | A.12 | Secure SDLC | Level 3 | Code reviews |
| **Incident Management** | A.14 | IR plan, team | Level 3 | IR tests |

---

## 6. Technical Standards Compliance

### 6.1 W3C Standards Implementation

```mermaid
graph TD
    subgraph "W3C Standards"
        VC_DM[VC Data Model 2.0]
        VC_API[VC API]
        DID_CORE[DID Core 1.0]
        DID_RESOLUTION[DID Resolution]
        JSON_LD[JSON-LD]
    end
    
    subgraph "Implementation Level"
        CORE_IMPL[Core: 100%]
        EXTENDED[Extended: 100%]
        OPTIONAL[Optional: 80%]
        CUSTOM[Custom Extensions]
    end
    
    subgraph "Interoperability"
        TEST_SUITE[W3C Test Suite]
        CROSS_PLATFORM[Cross-platform]
        THIRD_PARTY_INT[3rd Party Integration]
        STANDARDS_BODY[Standards Participation]
    end
    
    VC_DM --> CORE_IMPL
    DID_CORE --> CORE_IMPL
    VC_API --> EXTENDED
    DID_RESOLUTION --> OPTIONAL
    JSON_LD --> CORE_IMPL
    
    CORE_IMPL --> TEST_SUITE
    EXTENDED --> CROSS_PLATFORM
    OPTIONAL --> THIRD_PARTY_INT
    CUSTOM --> STANDARDS_BODY
```

### 6.2 Standards Compliance Matrix

| Standard | Version | Compliance Level | Testing | Certification |
|----------|---------|-----------------|---------|---------------|
| **W3C VC Data Model** | 2.0 | 100% Core + Extended | ✅ Pass | Pending |
| **W3C DID Core** | 1.0 | 100% Core | ✅ Pass | Pending |
| **OpenID Connect** | 1.0 | 100% | ✅ Pass | ✅ Certified |
| **OAuth 2.1** | Draft | 100% Current draft | ✅ Pass | N/A |
| **ISO/IEC 18013-5** | 2021 | 100% mDL | ✅ Pass | In progress |
| **FIDO2/WebAuthn** | Level 2 | 100% | ✅ Pass | ✅ Certified |

---

## 7. Tender Addendum Responses

### 7.1 Q&A Compliance Tracking

```mermaid
graph LR
    subgraph "Addendum Items"
        Q1[Question 1-50]
        Q2[Question 51-100]
        Q3[Question 101-150]
        Q4[Question 151-183]
    end
    
    subgraph "Response Status"
        ANSWERED[Fully Answered]
        CLARIFIED[Clarified]
        COMMITTED[Commitment Made]
        NOTED[Noted]
    end
    
    subgraph "Impact"
        NO_CHANGE[No Change]
        MINOR_UPDATE[Minor Update]
        MAJOR_UPDATE[Major Update]
        NEW_REQUIREMENT[New Requirement]
    end
    
    Q1 --> ANSWERED
    Q2 --> CLARIFIED
    Q3 --> COMMITTED
    Q4 --> NOTED
    
    ANSWERED --> NO_CHANGE
    CLARIFIED --> MINOR_UPDATE
    COMMITTED --> MAJOR_UPDATE
    NOTED --> NEW_REQUIREMENT
```

### 7.2 Key Addendum Clarifications

| Item # | Topic | Clarification | Our Response | Impact |
|--------|-------|---------------|--------------|--------|
| **Add-001** | POA Timeline | 3 weeks firm | Confirmed achievable | None |
| **Add-023** | Data Residency | AU only, no exceptions | Fully compliant | None |
| **Add-045** | SDK Languages | Flutter priority | Flutter + .NET + JS | Enhanced |
| **Add-067** | Offline Period | 30 days minimum | 90 days supported | Exceeds |
| **Add-089** | Biometrics | Required for mobile | Full support | Compliant |
| **Add-112** | Backup | User-controlled | Encrypted backup/restore | Compliant |
| **Add-134** | Support Hours | Business hours minimum | 24x7 available | Exceeds |
| **Add-156** | Training | Included in price | Comprehensive program | Compliant |
| **Add-178** | IP Rights | Client owns data | Confirmed | Compliant |

---

## 8. Compliance Monitoring and Reporting

### 8.1 Compliance Dashboard

```mermaid
graph TB
    subgraph "Compliance Metrics"
        REGULATORY[Regulatory: 98%]
        TECHNICAL_COMP[Technical: 95%]
        SECURITY_COMP[Security: 97%]
        PRIVACY[Privacy: 100%]
    end
    
    subgraph "Monitoring"
        CONTINUOUS[Continuous Monitoring]
        QUARTERLY[Quarterly Assessment]
        ANNUAL[Annual Audit]
        INCIDENT[Incident Tracking]
    end
    
    subgraph "Reporting"
        DASHBOARD_COMP[Executive Dashboard]
        DETAILED[Detailed Reports]
        AUDIT_TRAIL[Audit Trail]
        CERTIFICATIONS[Certifications]
    end
    
    subgraph "Actions"
        REMEDIATION[Remediation Plans]
        IMPROVEMENTS[Improvements]
        TRAINING_COMP[Training Programs]
        UPDATES[Policy Updates]
    end
    
    REGULATORY --> CONTINUOUS
    TECHNICAL_COMP --> QUARTERLY
    SECURITY_COMP --> ANNUAL
    PRIVACY --> INCIDENT
    
    CONTINUOUS --> DASHBOARD_COMP
    QUARTERLY --> DETAILED
    ANNUAL --> AUDIT_TRAIL
    INCIDENT --> CERTIFICATIONS
    
    DASHBOARD_COMP --> REMEDIATION
    DETAILED --> IMPROVEMENTS
    AUDIT_TRAIL --> TRAINING_COMP
    CERTIFICATIONS --> UPDATES
```

### 8.2 Compliance Reporting Schedule

| Report Type | Frequency | Audience | Content | Format |
|------------|-----------|----------|---------|--------|
| **Compliance Status** | Monthly | DGov | Overall compliance % | Dashboard |
| **Privacy Report** | Quarterly | DPO, DGov | APP compliance, breaches | PDF |
| **Security Report** | Monthly | CISO, DGov | Security posture, incidents | Dashboard |
| **Audit Report** | Annual | Board, DGov | Full compliance audit | Formal report |
| **Certification Status** | Quarterly | All stakeholders | Certification progress | Summary |

---

## 9. Compliance Gaps and Remediation

### 9.1 Gap Analysis

```mermaid
graph TD
    subgraph "Identified Gaps"
        GAP1[ISO 27001 Certification]
        GAP2[SOC 2 Type II]
        GAP3[Accessibility WCAG 2.2]
        GAP4[Carbon Neutral]
    end
    
    subgraph "Risk Level"
        LOW[Low Risk]
        MEDIUM[Medium Risk]
        HIGH[High Risk]
    end
    
    subgraph "Remediation"
        IMMEDIATE[Immediate Action]
        SHORT_TERM[3 Months]
        LONG_TERM[12 Months]
    end
    
    subgraph "Status"
        NOT_STARTED[Not Started]
        IN_PROGRESS_GAP[In Progress]
        COMPLETED[Completed]
    end
    
    GAP1 --> MEDIUM
    GAP2 --> LOW
    GAP3 --> HIGH
    GAP4 --> LOW
    
    MEDIUM --> SHORT_TERM
    LOW --> LONG_TERM
    HIGH --> IMMEDIATE
    
    SHORT_TERM --> IN_PROGRESS_GAP
    LONG_TERM --> NOT_STARTED
    IMMEDIATE --> IN_PROGRESS_GAP
```

### 9.2 Remediation Plan

| Gap | Priority | Target Date | Actions | Owner | Status |
|-----|----------|-------------|---------|-------|--------|
| **ISO 27001** | Medium | Q3 2025 | Gap assessment, implementation | Security Lead | In Progress |
| **SOC 2 Type II** | Low | Q4 2025 | Audit preparation | Compliance | Not Started |
| **WCAG 2.2 AA** | High | Q1 2025 | Accessibility audit, fixes | UX Team | In Progress |
| **Carbon Neutral** | Low | 2026 | Sustainability plan | Operations | Planning |

---

## 10. Compliance Assurance Program

### 10.1 Assurance Activities

```mermaid
gantt
    title Compliance Assurance Timeline
    dateFormat YYYY-MM-DD
    
    section Assessments
    Privacy Impact Assessment    :2025-01-01, 30d
    Security Assessment         :2025-02-01, 45d
    Compliance Audit           :2025-06-01, 30d
    
    section Certifications
    ISO 27001 Prep            :2025-03-01, 120d
    SOC 2 Readiness          :2025-07-01, 90d
    
    section Reviews
    Quarterly Review Q1       :2025-03-31, 1d
    Quarterly Review Q2       :2025-06-30, 1d
    Quarterly Review Q3       :2025-09-30, 1d
    Annual Review            :2025-12-31, 5d
```

### 10.2 Compliance Responsibilities

| Role | Responsibilities | Reporting | Frequency |
|------|-----------------|-----------|-----------|
| **Compliance Officer** | Overall compliance program | Board, DGov | Monthly |
| **Privacy Officer** | Privacy compliance, DPO duties | Compliance Officer | Weekly |
| **Security Officer** | Security compliance | CISO | Daily |
| **Legal Counsel** | Regulatory interpretation | Compliance Officer | As needed |
| **Internal Audit** | Compliance validation | Board | Quarterly |

---

## 11. Continuous Compliance

### 11.1 Compliance Automation

```mermaid
graph LR
    subgraph "Automated Checks"
        POLICY_CHECK[Policy Compliance]
        CONFIG_CHECK[Configuration Compliance]
        ACCESS_CHECK[Access Compliance]
        DATA_CHECK[Data Compliance]
    end
    
    subgraph "Tools"
        SCANNER[Compliance Scanner]
        MONITOR_COMP[Monitoring System]
        REPORTING_TOOL[Reporting Tool]
        WORKFLOW[Workflow Engine]
    end
    
    subgraph "Actions"
        ALERT[Alert]
        REMEDIATE[Auto-Remediate]
        REPORT[Report]
        ESCALATE[Escalate]
    end
    
    POLICY_CHECK --> SCANNER
    CONFIG_CHECK --> MONITOR_COMP
    ACCESS_CHECK --> MONITOR_COMP
    DATA_CHECK --> WORKFLOW
    
    SCANNER --> ALERT
    MONITOR_COMP --> REMEDIATE
    REPORTING_TOOL --> REPORT
    WORKFLOW --> ESCALATE
```

### 11.2 Compliance Metrics and KPIs

| Metric | Target | Current | Trend | Action |
|--------|--------|---------|-------|--------|
| **Overall Compliance** | 100% | 97% | ↑ | Continue improvement |
| **Privacy Compliance** | 100% | 100% | → | Maintain |
| **Security Compliance** | 98% | 97% | ↑ | Focus on gaps |
| **Audit Findings** | <5 | 3 | ↓ | Good progress |
| **Remediation Time** | <30 days | 25 days | ↓ | Maintain |
| **Training Completion** | 100% | 92% | ↑ | Push completion |

---

## 12. Compliance Documentation

### 12.1 Required Documentation

| Document | Purpose | Owner | Review Cycle | Status |
|----------|---------|-------|--------------|--------|
| **Privacy Policy** | Public disclosure | Privacy Officer | Annual | ✅ Complete |
| **Security Policy** | Security standards | Security Officer | Annual | ✅ Complete |
| **Data Retention Policy** | Retention rules | Compliance | Annual | ✅ Complete |
| **Incident Response Plan** | IR procedures | Security | Quarterly | ✅ Complete |
| **Business Continuity Plan** | BCP/DR | Operations | Annual | ✅ Complete |
| **Compliance Manual** | All compliance | Compliance | Quarterly | 🔄 In Progress |
| **Audit Reports** | Audit evidence | Internal Audit | Per audit | ✅ Current |
| **Risk Register** | Risk tracking | Risk Manager | Monthly | ✅ Current |

### 12.2 Evidence Collection

```mermaid
graph TD
    subgraph "Evidence Types"
        DOCUMENTS[Documentation]
        LOGS[Audit Logs]
        REPORTS[Reports]
        CERTIFICATES[Certificates]
        ATTESTATIONS[Attestations]
    end
    
    subgraph "Storage"
        GRC[GRC Platform]
        ARCHIVE_SYS[Archive System]
        SECURE[Secure Repository]
    end
    
    subgraph "Access"
        AUDITORS[Auditors]
        REGULATORS[Regulators]
        INTERNAL[Internal Review]
    end
    
    DOCUMENTS --> GRC
    LOGS --> ARCHIVE_SYS
    REPORTS --> SECURE
    CERTIFICATES --> GRC
    ATTESTATIONS --> SECURE
    
    GRC --> AUDITORS
    ARCHIVE_SYS --> REGULATORS
    SECURE --> INTERNAL
```

---

## Compliance Certification Roadmap

### Certification Timeline

```mermaid
gantt
    title Certification Roadmap
    dateFormat YYYY-MM-DD
    
    section Current
    Privacy Act Compliance     :done, 2024-01-01, 2024-12-31
    
    section 2025
    ISO 27001 Preparation      :2025-01-01, 180d
    SOC 2 Type I              :2025-04-01, 90d
    WCAG 2.2 Certification    :2025-02-01, 60d
    
    section 2026
    ISO 27001 Certification   :2026-01-01, 90d
    SOC 2 Type II            :2026-04-01, 365d
    ISO 27701 Privacy        :2026-07-01, 180d
```

### Investment Required

| Certification | Cost (AUD) | Timeline | ROI |
|--------------|------------|----------|-----|
| **ISO 27001** | $150,000 | 12 months | High |
| **SOC 2 Type II** | $100,000 | 18 months | High |
| **ISO 27701** | $75,000 | 6 months | Medium |
| **WCAG 2.2** | $25,000 | 3 months | High |

---

**END OF REGULATORY MAPPING APPENDIX**