# Complete Appendix E Documentation Suite
## Digital Wallet and Verifiable Credentials Solution

---

# Appendix E - Support & Maintenance Framework

## Table of Contents

1. [Purpose and Scope](#1-purpose-and-scope)
2. [Support Services](#2-support-services)
3. [Service Levels and Availability](#3-service-levels-and-availability)
4. [Training Plan](#4-training-plan)
5. [Onboarding & Implementation Support](#5-onboarding-implementation-support)
6. [Maintenance and Continuous Improvement](#6-maintenance-and-continuous-improvement)
   - [6A. Service Lifecycle Coverage](#6a-service-lifecycle-coverage)
   - [6B. Stakeholder Engagement & Service Lifecycle](#6b-stakeholder-engagement-service-lifecycle)
7. [Incident Management & Escalation](#7-incident-management-escalation)
8. [Business Continuity & Disaster Recovery Alignment](#8-business-continuity-disaster-recovery-alignment)
9. [Governance, Reporting, and Reviews](#9-governance-reporting-and-reviews)
10. [Appendices](#10-appendices)

## 1. Purpose and Scope

This section outlines the intent of the Support & Maintenance Framework, ensuring all obligations under Schedules 2, 3, 5 and 6 are addressed. It defines how CredEntry will deliver comprehensive support, training, onboarding, incident management, reporting, and lifecycle coverage across the Digital Wallet Solution.

*(See also Appendix I – Standards Compliance Mapping, Appendix A – SLA)*

Our Support and Maintenance Framework provides a complete service model for the ongoing operation of the Digital Wallet and Verifiable Credentials Solution. It demonstrates how we will ensure service reliability, protect citizen trust, and meet all contractual obligations under Schedule 5 – Ongoing Services and Schedule 6 – Performance Assessment Regime.

The framework covers support delivery, incident and vulnerability management, system performance, governance and reporting, and resilience planning, and is underpinned by our Service Level Agreement (SLA) and Business Continuity & Disaster Recovery Plan (BCP/DRP).

## 2. Support Services

This section describes the day-to-day support services available to citizens, agencies, and integration partners. It covers the service desk model, hours of operation, escalation channels, and ongoing updates.

*(See also Appendix A – SLA, Appendix E – Communications Templates & Escalation Contacts)*

- **Citizens:** Access to wallet support through in-app help, FAQs, and knowledge base.
- **Agencies and Administrators:** WA-based service desk (8:00am–5:00pm AWST) with direct access to support specialists for credential, issuance, or revocation queries.
- **Emergency Escalation:** 24/7 coverage for Severity 1 incidents ensures continuity of critical services such as verification and revocation.
- **Developers and Integration Partners:** Access to SDK documentation, technical support, and integration assistance via structured DevOps workflows.

## 3. Service Levels and Availability

This section establishes the service performance targets for availability, recovery, and resilience, backed by monitoring and reporting mechanisms. It demonstrates alignment with WA Government's uptime and continuity requirements.

*(See also Appendix A – SLA, Appendix F – Testing Results & Improvement Log)*

- **Availability Commitments:** 99.9% uptime for credential verification and revocation; 99.8% for issuance and APIs; 99.5% for admin dashboard.
- **Resilience Design:** Active-active Azure deployment across sovereign regions, with automated failover and RTO/RPO targets of 4–8 hours / 1–12 hours.
- **Monitoring and Fraud Detection:** Continuous monitoring via Azure Sentinel, Event Hubs, and Redis caching. Real-time anomaly alerts protect against fraudulent credential activity.
- **Performance Visibility:** Administrators access a secure dashboard with live performance, audit logs, and compliance reporting.

## 4. Training Plan

This section describes the structured training program to ensure WA Government staff and stakeholders are fully equipped to operate and administer the Digital Wallet. It outlines training content, delivery, and evaluation methods.

*(See also Appendix B – Training Plan, Appendix C – Release & Onboarding Process Flow)*

CredEntry will deliver a structured training methodology covering:

- Training Plan delivered at least four weeks prior to Pilot commencement, with actual training sessions aligned to implementation phases.
- Role-based training for administrators, developers, and support staff.
- Integration into a Knowledge Management Database to maintain policies and procedures.
- Post-training evaluation to measure effectiveness and adoption.

## 5. Onboarding & Implementation Support

This section covers the onboarding of administrators, issuers, and citizens during Proof-of-Operation, Pilot, and Production phases. It ensures a smooth transition with detailed documentation, guides, and direct support.

*(See also Appendix C – Release & Onboarding Process Flow, Appendix E – Communications Templates)*

- End-to-end support for Proof-of-Operation, Pilot Phase, and Production transition.
- Administrator onboarding and credential provider enablement.
- Documentation including admin guides, SDK integration materials, and sample credential workflows.
- Supplier and stakeholder engagement as required by DGov.

## 6. Maintenance and Continuous Improvement

This section sets out how planned, emergency, and ongoing maintenance activities are conducted, with a focus on service continuity, product enhancement, and warranty coverage. It demonstrates the continuous improvement culture underpinning the solution.

*(See also Appendix F – Testing Results & Improvement Log, Appendix J – Warranty Inclusions & Exclusions)*

- **Planned Maintenance:** Scheduled outside business hours, ≤4 hrs/month, ≥7 days' notice.
- **Emergency Maintenance:** Executed as required with immediate Department notification.
- **Change & Release Management:** ITIL v4 processes to introduce patches, features, and updates without disruption.
- **Product Roadmap:** Future-proofed enhancements (e.g. biometrics, interoperability modules) delivered through structured release cycles and agreed roadmaps.

### 6A. Service Lifecycle Coverage

This section demonstrates CredEntry's ability to provide support across the full service lifecycle — from Proof-of-Operation and Pilot to Production and Transition-Out — ensuring continuity at every stage.

*(See also Appendix G – BCP & DRP, Appendix H – Recovery Runbooks & Checklists)*

- **Proof-of-Operation** – Dedicated support during integration and demonstration activities, including technical walkthroughs, SDK guidance, and incident capture aligned to evaluation requirements.
- **Pilot Phase** – Tiered support for restricted and preview pilots, structured reporting of issues, and targeted training cycles. Dedicated resources ensure continuity as the wallet scales to live users.
- **Production Operation** – Full SLA-backed support services, incident management, vulnerability scanning, performance monitoring, and ongoing training.
- **Transition-Out** – In line with the Agreement (clause 58), CredEntry will provide structured transition-out support, including data handover, runbooks, documentation, and knowledge transfer to ensure continuity of government services.

### 6B. Stakeholder Engagement & Service Lifecycle

This section outlines the key stakeholders in the Digital Wallet ecosystem, their roles and responsibilities, and the engagement models CredEntry maintains to ensure effective collaboration and support.

*(See also Appendix D – Reporting Matrix, Appendix E – Communications Templates)*

| Stakeholder | Role & Responsibilities | Engagement Model |
|-------------|------------------------|------------------|
| **Office of Digital Government (DGov)** | Contract owner; defines requirements; governs service; manages DTP and IdX | Weekly steering committee |
| **ServiceWA & Adapptor** | Owns ServiceWA app; integrates SDK; manages UX | Daily stand-ups during integration |
| **Digital Wallet Provider (CredEntry)** | Develops and operates platform; provides SDKs; ensures compliance and service continuity | Dedicated Account Manager |
| **Credential Issuers (Agencies)** | Provide credentials via DTP; maintain attribute sources | Monthly issuer forums |
| **Citizens** | Hold credentials; consent to usage; recover wallets via in-app support | ServiceWA integrated support |
| **Verifiers & Relying Parties** | Accept credentials; validate proofs | Developer portal access and technical documentation |
| **WA Identity Exchange (IdX)** | Provides federated authentication for wallets and relying parties | Technical integration team collaboration |

## 7. Incident Management & Escalation

This section defines how incidents are categorised, escalated, and resolved, including severity levels, PIR requirements, and escalation pathways. It ensures alignment with WA Government security and continuity requirements.

*(See also Appendix A – SLA, Appendix E – Escalation Contacts, Appendix F – Improvement Log)*

- **Structured Severity Model:** Critical to Low incidents managed with clear response and resolution timelines.
- **Post-Incident Communication:** PIRs within 5 business days for High and Critical events, including root cause, remediation, and preventative measures.
- **Transparent Case Management:** All incidents logged in Freshdesk, integrated with Azure DevOps, visible to authorised Department stakeholders.
- **Continuous Learning:** Quarterly trend analysis to detect recurring issues, feeding into system and training improvements.

## 8. Business Continuity & Disaster Recovery Alignment

This section sets out CredEntry's approach to resilience, recovery, and continuity planning, ensuring compliance with Schedule 5 and WA Cyber Security Policy. It describes recovery objectives and planned annual testing with government stakeholders.

*(See also Appendix G – BCP & DRP, Appendix H – Recovery Runbooks)*

- **Resilience Strategy:** Multi-region Azure deployment, sovereign key management, and zero-trust access model.
- **Recovery Objectives:** Critical services restored within 4–8 hrs (RTO); data integrity protected to within 1–12 hours (RPO).
- **Testing & Assurance:** Monthly backup restores, quarterly failover simulations, and annual scenario-based continuity exercises.
- **BCP Coverage:** Verification, revocation, issuance, SDK/API endpoints, and dashboard prioritised as critical services in the BCP impact matrix.

## 9. Governance, Reporting, and Reviews

This section defines governance processes, reporting obligations, and performance review mechanisms. It integrates SLA monitoring, compliance obligations (CR-1 to CR-5), and continuous improvement activities into one structured framework.

*(See also Appendix D – Reporting Matrix, Appendix F – Improvement Log, Appendix I – Standards Mapping)*

- **Governance Meetings:** Monthly contract management meetings to review KPIs, incidents, service levels, and planned improvements.
- **Formal Escalations:** Corrective Action Plans (CAPs) and Performance Remediation Plans (PRPs) delivered in line with Schedule 6 obligations.
- **Regular Reporting:** Monthly SLA compliance reports, quarterly trend analysis, annual performance review and roadmap discussion.

**Enhanced Reporting Alignment (CR-1 to CR-5):**

- Standards conformance results (ISO/IEC 18013, 23220, eIDAS 2.0).
- 24-hour cybersecurity incident reporting.
- Annual certification evidence (ISO/IEC 27001, SOC2, IRAP).
- Entity information security assurance.

## 10. Appendices

This section lists the supporting evidence pack that underpins the Support & Maintenance Framework.

- **Appendix E.1** – Service Level Agreement
- **Appendix E.2** – Training Plan
- **Appendix E.3** – Release & Onboarding Process Flow
- **Appendix E.4** – Reporting Matrix
- **Appendix E.5** – Communications Templates & Emergency Escalation Contacts
- **Appendix E.6** – Testing Results & Improvement Log
- **Appendix E.7** – Business Continuity Plan & Disaster Recovery Plan
- **Appendix E.8** – Recovery Runbooks & Checklists
- **Appendix E.9** – Standards Compliance Mapping

---

# Appendix E.1 - Service Level Agreement

## Table of Contents

1. [Purpose and Scope](#e1-1-purpose-and-scope)
2. [Support Services](#e1-2-support-services)
3. [Platform Availability and Recovery Objectives](#e1-3-platform-availability-and-recovery-objectives)
4. [Incident Response and Resolution](#e1-4-incident-response-and-resolution)
5. [Vulnerability Management](#e1-5-vulnerability-management)
6. [Maintenance and Continuous Improvement](#e1-6-maintenance-and-continuous-improvement)
7. [Performance Assessment and Service Credits](#e1-7-performance-assessment-and-service-credits)
8. [Alignment with BCP/DRP](#e1-8-alignment-with-bcpdrp)

## E1-1. Purpose and Scope

This Service Level Agreement (SLA) defines the minimum service performance targets, incident response times, and remedies applicable to the Digital Wallet and Verifiable Credentials Solution.

The SLA forms part of the Agreement and aligns with:

- **Schedule 5 – Ongoing Services**
- **Schedule 6 – Performance Assessment Regime**
- **Business Continuity & Disaster Recovery Plan (Appendix G)**

The SLA is also aligned with ISO/IEC 20000-1 (Service Management), ITIL v4 service practices, and ACSC Essential Eight requirements.

## E1-2. Support Services

CredEntry provides comprehensive support to all stakeholders:

- **Multi-channel support:**
  - Phone & email support: Monday–Friday, 8:00am–5:00pm AWST.
  - 24/7 emergency escalation for Severity 1 incidents.

- **Knowledge base & self-help guides:** 24/7 access within the platform.

- **Bug fixes & code corrections:** Timely remediation of defects.

- **Enhancements & updates:** Security patches and functional improvements at no additional charge, delivered under change management processes.

## E1-3. Platform Availability and Recovery Objectives

| Service Component | Availability Target | RTO | RPO | Measurement |
|-------------------|-------------------|-----|-----|-------------|
| Credential Verification | 99.9% | 4 hrs | 1 hr | Monthly |
| Revocation Services | 99.9% | 4 hrs | 1 hr | Monthly |
| Credential Issuance | 99.8% | 8 hrs | 2 hrs | Monthly |
| SDK/API Endpoints | 99.8% | 8 hrs | 4 hrs | Monthly |
| Admin Dashboard | 99.5% | 24 hrs | 12 hrs | Monthly |

**Notes:**

- Availability is measured at the API/SDK layer.
- Exclusions: planned maintenance (≤4 hrs/month, ≥7 days' notice), force majeure, uncontrollable third-party outages, or client-side issues.

## E1-4. Incident Response and Resolution

| Severity | Definition | Response Time | Resolution Target | Reporting |
|----------|------------|---------------|-------------------|-----------|
| **1 – Critical** | Complete outage, credential verification failure, or confirmed security breach. | 30 minutes (business hours, 1 hr after-hours) | ≤8 hrs workaround; ≤2 days resolution | PIR within 5 business days |
| **2 – High** | Major functionality impaired; workaround unavailable. | 1 hour (business hours, 4 hrs after-hours) | ≤2 days workaround; ≤7 days resolution | PIR within 10 business days |
| **3 – Medium** | Moderate issue; workaround available. | 4 hrs (business hours) | ≤10 business days | Monthly report |
| **4 – Low** | Cosmetic/minor issue, enhancement request. | 2 business days | Next release cycle | Release notes |

**Escalation Path:**  
Incidents follow the escalation matrix in **Appendix E – Emergency Escalation Contacts**: WA-based L1 → Project Delivery Lead → Performance Manager → Customer Contract Manager.

## E1-5. Vulnerability Management

- **Scanning:** Automated vulnerability scans at least quarterly and after major releases.

- **Remediation Targets:**
  - Critical: ≤5 business days (with compensating controls within 24 hrs).
  - High: ≤10 business days.
  - Medium: ≤30 business days.
  - Low: Next scheduled release cycle.

- **Tracking:** All remediation actions logged and reviewed as part of monthly SLA reporting.

## E1-6. Maintenance and Continuous Improvement

- **Planned Maintenance:** Scheduled outside business hours wherever possible; ≥7 days' notice provided.

- **Emergency Maintenance:** Carried out as required; DPC notified immediately.

- **Continuous Improvement:** Enhancements and roadmap features delivered via ITIL-aligned change processes, ensuring no disruption to citizen services.

## E1-7. Performance Assessment and Service Credits

- SLA performance reviewed **monthly** with DPC.

- Failure to achieve minimum service levels results in service credits, applied as a % of monthly fee:

| Availability % (monthly) | Service Credit |
|-------------------------|----------------|
| 99.95–99.50% | 5% of monthly fee |
| 99.50–99.00% | 10% of monthly fee |
| <99.00% | 25% of monthly fee + CAP |

**Credit Calculation:** Cumulative with 25% cap per month

- **Persistent underperformance:**
  - 2 consecutive breaches → Corrective Action Plan (CAP).
  - 3 breaches in 6 months → Performance Remediation Plan (PRP), with escalation under Schedule 6.

## E1-8. Alignment with BCP/DRP

- SLA metrics reflect recovery objectives defined in **Appendix E.7 – BCP & DRP**.
- Incident classifications and escalation pathways mirror those in **Appendix E.8 – Recovery Runbooks**.
- SLA performance feeds into quarterly reviews and the annual improvement cycle.

---

# Appendix E.2 - Training Plan

## Table of Contents

1. [Purpose and Scope](#e2-1-purpose-and-scope)
2. [Training Objectives](#e2-2-training-objectives)
3. [Training Content](#e2-3-training-content)
   - [a) Administrators and Department Staff](#e2-a-administrators-and-department-staff)
   - [b) Technical Teams (Developers & Integrators)](#e2-b-technical-teams-developers--integrators)
   - [c) Support Teams](#e2-c-support-teams)
   - [d) Relying Parties & Verifiers](#e2-d-relying-parties--verifiers)
   - [e) Citizens (via ServiceWA)](#e2-e-citizens-via-servicewa)
4. [Delivery Approach](#e2-4-delivery-approach)
5. [Schedule and Timing](#e2-5-schedule-and-timing)
6. [Roles and Responsibilities](#e2-6-roles-and-responsibilities)
7. [Evaluation and Success Measures](#e2-7-evaluation-and-success-measures)
8. [Knowledge Management Database](#e2-8-knowledge-management-database)

## E2-1. Purpose and Scope

The Training Plan provides a structured methodology to ensure that all stakeholders — Department staff, administrators, issuers, and relying parties — can effectively use, manage, and support the Digital Wallet and Verifiable Credentials Solution.

It aligns with:

- **Schedule 2, Section 1.8 – Training** (Statement of Requirements)
- **Implementation Plan (Appendix F)** for Pilot readiness
- **Knowledge Management Database obligations**

Training Plan will be delivered at least four weeks prior to the Pilot commencement, with actual training sessions conducted throughout the implementation phases and updated throughout the contract lifecycle.

## E2-2. Training Objectives

- Ensure all relevant staff are confident in using and managing the Digital Wallet solution.
- Provide administrators and technical staff with the skills to configure, monitor, and support the platform.
- Establish ongoing knowledge transfer to maintain operational resilience.
- Embed content into a Knowledge Management Database for long-term accessibility.

## E2-3. Training Content

Training modules will be tailored for each stakeholder group:

### E2-a) Administrators and Department Staff

- Credential lifecycle management (issuance, revocation, suspension, update).
- Access control and reporting dashboards.
- Incident management and escalation processes.

### E2-b) Technical Teams (Developers & Integrators)

- SDK integration procedures.
- API usage, configuration, and troubleshooting.
- Security and privacy considerations (OIDC4VCI/VP, ISO/IEC 18013-5 compliance).

### E2-c) Support Teams

- Helpdesk workflows and case management.
- Standard operating procedures (SOPs).
- Escalation triggers and use of emergency contact lists.

### E2-d) Relying Parties & Verifiers

- Credential validation and proof workflows.
- Developer portal onboarding.
- Handling expired or revoked credentials.

### E2-e) Citizens (via ServiceWA)

- In-app tutorials, FAQs, and support resources.
- Guidance on credential recovery and consent management.

## E2-4. Delivery Approach

Training will be delivered using a blended model:

- **Instructor-led workshops** (virtual and in-person).
- **E-learning modules** embedded in the Knowledge Management Database.
- **Role-based quick reference guides** and checklists.
- **Train-the-Trainer model** for agency leads to cascade knowledge.
- **Scenario-based simulations** during Pilot testing (restricted and preview phases).

## E2-5. Schedule and Timing

- Training Plan finalised and delivered: **≥4 weeks prior to Pilot commencement**.
- Initial training sessions: aligned with **Stage 1 and Stage 2 of the Pilot**.
- Ongoing refreshers: annual or as required by solution updates.
- Additional ad-hoc training delivered in response to changes in standards, integration needs, or Department request.

## E2-6. Roles and Responsibilities

- **CredEntry (Customer Success & Implementation Lead):** Development and delivery of all training content; integration into Knowledge Management Database.
- **DGov / ServiceWA:** Validation of training content and UX alignment.
- **Agencies (Credential Issuers):** Participation in issuer forums; support for attribute-specific training.
- **Department Contract Manager:** Oversight of training schedule and approval of materials.

## E2-7. Evaluation and Success Measures

Training success will be measured by:

- Attendance and participation logs.
- Pre- and post-training assessments.
- Feedback surveys from participants.
- Operational metrics during Pilot (e.g., reduced support tickets, improved first-call resolution).
- Quarterly review of training effectiveness as part of Governance Forum.

## E2-8. Knowledge Management Database

- All training materials, SOPs, and policies will be integrated into a **central Knowledge Management Database**, ensuring version control and continuous accessibility.
- Updates will be aligned to software releases and roadmap updates (Appendix D – Product Development Roadmap).

---

# Appendix E.3 - Release & Onboarding Process Flow

## Table of Contents

1. [Purpose and Scope](#e3-1-purpose-and-scope)
2. [Objectives](#e3-2-objectives)
3. [Onboarding Stages](#e3-3-onboarding-stages)
   - [a) Administrator Onboarding](#e3-a-administrator-onboarding)
   - [b) Credential Issuer Onboarding](#e3-b-credential-issuer-onboarding)
   - [c) Relying Parties / Verifiers](#e3-c-relying-parties--verifiers)
   - [d) Citizens](#e3-d-citizens)
4. [Release Management](#e3-4-release-management)
5. [Engagement Model](#e3-5-engagement-model)
6. [Roles and Responsibilities](#e3-6-roles-and-responsibilities)
7. [Success Measures](#e3-7-success-measures)
8. [Evidence and Artefacts](#e3-8-evidence-and-artefacts)

## E3-1. Purpose and Scope

This appendix details the process by which agencies, credential issuers, administrators, and relying parties are onboarded into the Digital Wallet and Verifiable Credentials Solution.

It also defines the structured release process used to deploy updates, new features, and security patches, ensuring alignment with WA Government service continuity expectations and the **Implementation Plan (Appendix F)**.

## E3-2. Objectives

- Provide a repeatable, transparent onboarding process for all stakeholders.
- Ensure controlled releases that minimise risk to production environments.
- Enable efficient credential issuer onboarding via DGov's DTP and IdX.
- Support rapid scale-up from Proof-of-Operation → Pilot → Production.
- Ensure all onboarding activities are documented and version-controlled.

## E3-3. Onboarding Stages

### E3-a) Administrator Onboarding

- Provision of admin accounts with role-based access control.
- Training delivery (cross-reference **Appendix E.2 – Training Plan**).
- Access to Admin Dashboard for monitoring credential lifecycle.

### E3-b) Credential Issuer Onboarding

- Monthly issuer forums for agency engagement.
- Schema mapping workshops to align attributes.
- Testing of credential issuance workflows in pre-production environment.

### E3-c) Relying Parties / Verifiers

- Access to developer portal and test APIs.
- Documentation on OIDC4VP verification and selective disclosure.
- Live test credentials issued for verification pilots.

### E3-d) Citizens

- Automatic onboarding via ServiceWA app.
- Wallet creation flows validated in Proof-of-Operation.
- Recovery and consent management tested in Pilot.

## E3-4. Release Management

**Controlled Release Workflow:**

1. **Development:** Features and fixes built in controlled DevOps environment.
2. **Internal QA:** Functional and regression testing by CredEntry QA team.
3. **UAT / Pilot Testing:** With DGov testers in restricted pilot groups.
4. **Staged Deployment:** Release into pre-production environment.
5. **Production Release:** Deployment to live environment with change approvals.
6. **Post-Release Monitoring:** Metrics captured, PIRs raised if required.

**Release Cadence:**

- **Major releases:** Quarterly, aligned with roadmap (see Appendix D – Product Development Roadmap).
- **Minor releases / patches:** Monthly or as required for security.
- **Emergency patches:** Within 24 hours of confirmed vulnerability.

## E3-5. Engagement Model

- **Daily stand-ups:** During SDK integration with ServiceWA (Adapptor, DGov).
- **Weekly steering committee:** DGov + CredEntry account manager for release status.
- **Monthly issuer forums:** For agency onboarding.
- **Quarterly governance meetings:** To review release and onboarding outcomes.

## E3-6. Roles and Responsibilities

- **CredEntry Project Delivery Lead:** Oversees onboarding and release coordination.
- **CredEntry Implementation Specialist:** Manages training, documentation, and onboarding support.
- **CredEntry QA Lead:** Ensures quality and regression testing.
- **DGov:** Governs requirements, approves release gates, manages DTP/IdX integration.
- **ServiceWA (Adapptor):** Ensures UI/UX consistency and SDK integration.
- **Agencies:** Provide attribute data and maintain credential source systems.

## E3-7. Success Measures

- 95% of onboarding activities completed within scheduled timeframe.
- Zero unplanned downtime during release windows.
- 100% of credential issuers onboarded with schema mapping verified.
- All releases accompanied by deployment notes, rollback procedures, and monitoring dashboards.
- Positive participant feedback during Pilot onboarding (measured via surveys).

## E3-8. Evidence and Artefacts

Supporting artefacts include:

- Onboarding process flow diagrams
- Deployment workbooks and configuration guides.
- Issuer onboarding checklists – to be released during pilot (phase 3)
- Release notes and change logs.

---

# Appendix E.4 - Reporting Matrix

## Table of Contents

1. [Purpose and Scope](#e4-1-purpose-and-scope)
2. [Reporting Matrix](#e4-2-reporting-matrix)
3. [Governance Integration](#e4-3-governance-integration)

## E4-1. Purpose and Scope

This appendix defines the reporting obligations that underpin governance of the Digital Wallet and Verifiable Credentials Solution.

It ensures that all operational, compliance, and assurance reporting is delivered **on time, to the correct recipients**, and aligned with:

- **Schedule 3 – Specifications (Compliance & Reporting CR-1 to CR-5)**
- **Schedule 5 – Ongoing Services**
- **Schedule 6 – Performance Assessment Regime**

The matrix below outlines report type, timing, responsible CredEntry role, recipient, and delivery method.

## E4-2. Reporting Matrix

| Report Type | Description | Frequency | Responsible Party (CredEntry) | Recipient | Delivery Method |
|-------------|-------------|-----------|-------------------------------|-----------|-----------------|
| **SLA Compliance Report** | Service levels achieved (availability, incident response, vulnerabilities, service credits applied). | Monthly | Justin Hancock – Project Delivery Lead | Customer Contract Manager & Performance Manager | PDF report + governance meeting pack |
| **Incident Notification** | Initial notification of Severity 1–2 incidents, including impact and workaround. | Within 24 hrs of detection | Zachariah Adams – Technical Support Lead | Customer Contract Manager + DGov Security Contact | Email + phone (per Escalation Contacts, Appendix E.5) |
| **Post-Incident Report (PIR)** | Root cause, remediation, and preventative actions for Severity 1–2 incidents. | Within 5 business days of incident closure | Justin Hancock – Project Delivery Lead | Customer Contract Manager & Performance Manager | PDF report |
| **Vulnerability Scan Report** | Results of quarterly scans (OWASP, Nessus, Defender for Cloud) and remediation progress. | Quarterly (and post-release) | Flavia C – Security & Compliance Officer | Customer Contract Manager + DGov Security Contact | PDF report |
| **Continuous Improvement & Trend Analysis** | Analysis of recurring issues, service improvements, and training adjustments. | Quarterly | Shelby Long – Implementation Specialist | Quarterly Governance Forum | Governance meeting presentation |
| **Training Delivery Report** | Confirmation of training delivered, audience covered, attendance logs, and feedback outcomes. | ≥4 weeks before Pilot go-live; annually thereafter | Shelby Long – Implementation Specialist | Customer Contract Manager + Project Sponsor | PDF report + LMS export |
| **Testing Results & Improvement Log** | Results from acceptance testing, pilot evaluation, BCP/DRP tests, and corrective actions. | Per test cycle; annually for continuity exercises | Marisa Cardoso – Quality Assurance | Customer Contract Manager + DGov Test/Integration Leads | PDF report |
| **Annual Performance & Roadmap Review** | Review of year's performance, roadmap for improvements, ISO certification updates. | Annually | Marcus Abreu – Senior Solution Architect (with Justin Hancock) | DPC Senior Stakeholders + Governance Forum | Governance meeting presentation |
| **Standards Conformance Reports** (CR-1 & CR-2) | Regular ISO/IEC 18013 & 23220 conformance tests; eIDAS 2.0 interoperability results. | Semi-annual | Marcus Abreu – Senior Solution Architect | Customer Contract Manager + Performance Manager | PDF report |
| **Certification Maintenance Evidence (CR-4)** | Evidence of ongoing ISO/IEC 27001, SOC 2 Type 2, IRAP. | Annually | Flavia C – Security & Compliance Officer | Customer Contract Manager | PDF report |
| **Cybersecurity Incident Report (CR-3)** | Notification of any cyber incident within 24 hours, aligned to WA Cyber Security Policy. | As required | Zachariah Adams – Technical Support Lead | DGov Security Contact & Contract Manager | Email + PIR follow-up |
| **Ad Hoc / On-Demand Reports** | Any reports requested by the Department (compliance evidence, audit support). | As requested | Relevant Lead (coordinated by Project Delivery Lead) | Customer Contract Manager | PDF/email |

## E4-3. Governance Integration

- Reports are reviewed in **monthly governance meetings** and **quarterly service reviews**.
- Escalations are documented in **Appendix E.5 – Escalation Contacts**.
- Testing and remediation evidence is maintained in **Appendix E.6 – Testing Results & Improvement Log**.
- Performance trends feed into the **Annual Performance & Roadmap Review** (Appendix D – Roadmap).

---

# Appendix E.5 - Communications Templates & Emergency Escalation

## Table of Contents

1. [Purpose and Scope](#e5-1-purpose-and-scope)
2. [Communication Templates](#e5-2-communication-templates)
3. [Emergency Escalation Contacts](#e5-3-emergency-escalation-contacts)
4. [Escalation Timelines](#e5-4-escalation-timelines)
5. [Document Control](#e5-5-document-control)

## E5-1. Purpose and Scope

This appendix provides:

1. The standard communication templates used for incident management, change control, compliance reporting, and escalation.

2. The emergency escalation contact list for both CredEntry and WA Government stakeholders, ensuring alignment with **Schedule 5 – Ongoing Services** and **Schedule 6 – Performance Assessment Regime**.

## E5-2. Communication Templates

CredEntry will use structured communication templates to ensure timely, consistent, and compliant notifications.

Templates include (full content in attached document):

- **Initial Incident Notification** (Critical / High / Medium / Low).
- **Security Incident Report** (aligned with WA Cyber Security Policy).
- **Incident Resolution Report (RCA/PIR)**.
- **Change / Release Notification**.
- **Quarterly Compliance & Performance Report**.
- **Force Majeure Notification**.
- **Performance Remediation Plan (PRP)**.
- **Media Holding Statement** (Customer-approved only).

(See supporting templates in Communication Templates file, Appendix E.5-A).

## E5-3. Emergency Escalation Contacts

**Escalation Flow:**  
Level 1 → Level 2 → Project Delivery Lead → CredEntry Performance Manager → Customer Contract Manager → DGov Security Contact.

### A. CredEntry Contacts (Project Team)

| Role | Name / Function | Escalation Level | Contact Method |
|------|-----------------|------------------|----------------|
| Technical Support Lead | **Zachariah Adams** | Level 1 (initial incident triage) | Phone / Email |
| Security & Compliance Officer | **Flavia C** | Level 2 (security incidents, vulnerabilities) | Phone / Email |
| Project Delivery Lead | **Justin Hancock** | Level 3 (major incidents, outages) | Phone / Email |
| Implementation Specialist | **Shelby Long** | Level 3 (training/onboarding issues) | Phone / Email |
| Senior Solution Architect | **Marcus Abreu** | Level 3 (system architecture issues) | Phone / Email |
| Head of IT / FullStack DevOps | **Rodrigo Miranda** | Level 3 (infrastructure/deployment issues) | Phone / Email |
| Performance Manager / Escalation Owner | **Fiona Ngo (General Manager)** | Level 4 (executive escalation, CAP/PRP) | Phone / Email |
| Directors | **Andre Garnaut / Gres Vukman** | Level 5 (executive governance, final escalation) | Phone / Email |

### B. WA Government Contacts

| Role | Name / Function | Escalation Responsibility | Contact Method |
|------|-----------------|--------------------------|----------------|
| Customer Contract Manager | DPC-appointed officer | Primary recipient of all SLA and incident communications | Phone / Email |
| Performance Manager | DPC-appointed officer | Oversight of SLA compliance, CAP/PRP approvals | Phone / Email |
| DGov Security Contact | Office of Digital Government | 24-hour cyber incident notifications (CR-3 obligation) | Phone / Email |
| DGov Technical / Test Leads | DGov | Coordination of PoO, Pilot, and integration testing | Email / Meeting |
| Senior Stakeholders | DPC Executive Steering Committee | Annual performance review, roadmap approvals | Governance forum |

## E5-4. Escalation Timelines

- **Critical (Severity 1):** Notify Contract Manager within **15 minutes** (phone + email).
- **High (Severity 2):** Notify within **30 minutes** (business hours) or **2 hours after-hours**.
- **Medium (Severity 3):** Notify within **2 business hours**.
- **Low (Severity 4):** Notify within **2 business days**.
- **Security & Privacy Incidents:** Notify within **24 hours**, OAIC notification within **72 hours** if Privacy Act breach.

## E5-5. Document Control

- **Owner:** Project Delivery Lead (CredEntry).
- **Review Cycle:** Quarterly review, annual full review, and post-incident update within 10 business days.
- **Storage:** SharePoint with full audit logging.

---

# Appendix E.6 - Testing Methodology, Results & Improvement Log

## Table of Contents

1. [Purpose and Scope](#e6-1-purpose-and-scope)
2. [Testing Methodology](#e6-2-testing-methodology)
   - [2.1 Objectives](#e6-21-objectives)
   - [2.2 Testing Types & Frequency](#e6-22-testing-types--frequency)
   - [2.3 Testing Environments](#e6-23-testing-environments)
   - [2.4 Standards Alignment](#e6-24-standards-alignment)
3. [Results, Evidence & Reporting](#e6-3-results-evidence--reporting)
   - [3.1 Acceptance Criteria](#e6-31-acceptance-criteria)
   - [3.2 Artefacts & Evidence Pack](#e6-32-artefacts--evidence-pack)
   - [3.3 Reporting](#e6-33-reporting)
   - [3.4 Stakeholder Communication](#e6-34-stakeholder-communication)
4. [Continuous Improvement](#e6-4-continuous-improvement)
   - [4.1 Issue Tracking & Root Cause Analysis](#e6-41-issue-tracking--root-cause-analysis)
   - [4.2 Corrective & Remedial Actions](#e6-42-corrective--remedial-actions)
   - [4.3 Updates to Plans & Documents](#e6-43-updates-to-plans--documents)
   - [4.4 Governance & Metrics](#e6-44-governance--metrics)
5. [Dependencies & Readiness Gates](#e6-5-dependencies--readiness-gates)

## E6-1. Purpose and Scope

This appendix defines CredEntry's structured approach to testing the resilience, security, performance, and operability of the Digital Wallet and Verifiable Credentials Solution.

It aligns with and is supported by:

- **Appendix E.1 – Service Level Agreement (SLA)** (availability, RTO/RPO, incident response, vulnerability remediation).
- **Appendix E.2 – Training Plan** (UAT readiness, role-based training linked to test outcomes).
- **Appendix E.3 – Release & Onboarding Process Flow** (release gates, pilot/production readiness testing).
- **Appendix E.4 – Reporting Matrix** (frequency, owners, recipients of test reports).
- **Appendix E.5 – Communications Templates & Emergency Escalation Contacts** (incident and test notification templates and escalation contacts).
- **Appendix E.7 – Business Continuity Plan & Disaster Recovery Plan** (resilience architecture, recovery objectives, continuity testing).
- **Appendix E.8 – Recovery Runbooks & Checklists** (step-by-step operational recovery procedures used during tests).
- **Appendix E.9 – Standards Compliance Mapping** (ISO/IEC 22301, 27001, 18013-5/-7, 23220-1, ACSC Essential Eight, TDIF, eIDAS 2.0).

## E6-2. Testing Methodology

### E6-2.1 Objectives

- Validate compliance with **Business Continuity and Disaster Recovery requirements** (per Appendix E.7).
- Ensure the solution meets functional, security, and integration expectations.
- Confirm **SLA targets** (availability, response/resolution, RTO/RPO) defined in Appendix E.1 are consistently met.
- Support continuous improvement and incident prevention.
- Demonstrate readiness for major events, onboarding milestones, and environmental changes (Appendix E.3).

### E6-2.2 Testing Types & Frequency

| Frequency | Activities | Linked Evidence | Reporting Obligation (Appendix E.4) |
|-----------|------------|-----------------|--------------------------------------|
| **Monthly** | Backup restoration; partial failover simulation; vulnerability scanning | Recovery steps (Appendix E.8), SLA metrics (Appendix E.1) | SLA Compliance Report; Incident Notification (if triggered) |
| **Quarterly** | Regional failover drills; tenant isolation checks; ServiceWA/IdX integration tests | BCP/DRP alignment (Appendix E.7), runbook validation (Appendix E.8) | Vulnerability Scan Report; Continuous Improvement & Trend Analysis |
| **Annual** | Full DR simulation; third-party penetration test; system-wide UAT | BCP/DRP scenarios (Appendix E.7); Release gates (Appendix E.3) | Testing Results & Improvement Log; Annual Performance & Roadmap Review |
| **Ad hoc** | Cyber incident simulation; emergency failover; onboarding release verification | Crisis comms templates (Appendix E.5); Runbooks (Appendix E.8) | Cybersecurity Incident Report (CR-3); PIR (≤5 business days) |

### E6-2.3 Testing Environments

- **Pre-Production Staging**: Mirrors production; used for all major tests and UAT.
- **Pilot/Test Infrastructure**: Used for SDK integration and onboarding validation (Appendix E.3).
- **Production**: Limited to monitoring and controlled failover testing.

### E6-2.4 Standards Alignment

Testing is aligned with standards documented in **Appendix E.9 – Standards Compliance Mapping**:

- ISO/IEC 27001:2022 – Information Security Management Systems
- ISO/IEC 22301:2019 – Business Continuity Management
- ISO/IEC 18013-5 & 18013-7 – Mobile Driving Licence standards
- ISO/IEC 23220-1:2023 – Mobile eID architecture
- ACSC Essential Eight; OWASP ASVS; TDIF 4.8; eIDAS 2.0; IRAP PROTECTED alignment

## E6-3. Results, Evidence & Reporting

### E6-3.1 Acceptance Criteria

A test cycle is successful if:

- Functional and technical specifications are met.
- SLA service levels (Appendix E.1) are achieved.
- RTO/RPO recovery objectives (Appendix E.7) are met.
- UAT and onboarding validation (Appendix E.3) is signed off.
- Runbooks (Appendix E.8) execute without deviation.

### E6-3.2 Artefacts & Evidence Pack

Evidence is maintained in SharePoint (per governance model in Appendix E.4) and includes:

- Test execution reports, defect logs, UAT sign-offs.
- Vulnerability scan and pen test results (linked to Appendix E.1 remediation timelines).
- DR drill timings vs RTO/RPO (Appendix E.7).
- Runbook execution checklists (Appendix E.8).
- PIRs, CAPs, PRPs (Appendix E.1 & E.5).

### E6-3.3 Reporting

- **Testing Results & Improvement Log** – after each major cycle, annually consolidated. Owner: QA Lead.
- **SLA Compliance Report** – monthly; includes test data where relevant. Owner: Project Delivery Lead.
- **Vulnerability Scan Report** – quarterly and post-release. Owner: Security & Compliance Officer.
- **Continuous Improvement & Trend Analysis** – quarterly. Owner: Implementation Specialist.
- **Post-Incident Report (PIRs)** – ≤5 business days for Severity 1–2. Owner: Project Delivery Lead.
- **Cybersecurity Incident Reports** – ≤24 hours of detection. Owner: Technical Support Lead.

### E6-3.4 Stakeholder Communication

- Severity 1: notify Contract Manager within 15 minutes.
- Severity 2: notify within 30 minutes (business hours) or 2 hours after-hours.
- Security/Privacy incidents: ≤24h to DGov Security; ≤72h OAIC if Privacy Act breach.

## E6-4. Continuous Improvement

### E6-4.1 Issue Tracking & Root Cause Analysis

- All failures/incidents logged and triaged. RCA within 5 business days.
- Unresolved risks entered into the Risk Register (per Appendix E.7).

### E6-4.2 Corrective & Remedial Actions

- **Corrective Action Plans (CAPs)** for critical deviations (Appendix E.1).
- **Performance Remediation Plans (PRPs)** for repeat issues (Appendix E.1).

### E6-4.3 Updates to Plans & Documents

- BCP/DRP (Appendix E.7), Runbooks (Appendix E.8), and Training materials (Appendix E.2) updated within 10 days of a critical incident/test failure.
- Test plans reviewed annually or after major releases (Appendix E.3).

### E6-4.4 Governance & Metrics

Metrics (per Appendix E.4 governance forums):

- MTTD/MTTR, % tests passed first attempt, vulnerability ageing, DR drill timings.
- Persistent breaches → CAP/PRP under Schedule 6 and Appendix E.1 SLA.

## E6-5. Dependencies & Readiness Gates

Release and onboarding readiness gates (per Appendix E.3):

- QA sign-off, security clearance (Appendix E.1), continuity rehearsal (Appendix E.7/E.8), UAT approval, and training updates (Appendix E.2).

External dependencies: IdX, DTP, OEM wallets, and CAs, managed under Appendix E.8.

---

# Appendix E.7 - Business Continuity Plan & Disaster Recovery Plan

**Digital Wallet and Verifiable Credentials Solution (DPC2142)**  
**Document Version:** 2.0  
**Prepared for:** Department of the Premier and Cabinet (DGov)  
**Contract Reference:** DPC2142  
**Classification:** OFFICIAL Sensitive  
**Date:** September 2025

## Table of Contents

1. [Executive Summary and Governance Context](#e7-1-executive-summary-and-governance-context)
2. [Standards and Regulatory Compliance](#e7-2-standards-and-regulatory-compliance)
3. [Scope and Coverage](#e7-3-scope-and-coverage)
4. [Risk Assessment & Business Impact Analysis](#e7-4-risk-assessment--business-impact-analysis)
5. [Business Continuity Strategy](#e7-5-business-continuity-strategy)
6. [Service Levels & Recovery Objectives](#e7-6-service-levels--recovery-objectives)
7. [Incident Response & Recovery](#e7-7-incident-response--recovery)
8. [Roles & Responsibilities (Recovery Operations)](#e7-8-roles--responsibilities-recovery-operations)
9. [Testing & Continuous Improvement](#e7-9-testing--continuous-improvement)
10. [Organisational Capability](#e7-10-organisational-capability)
11. [Compliance & Assurance](#e7-11-compliance--assurance)
12. [Commercial Considerations](#e7-12-commercial-considerations)
13. [Document Control](#e7-13-document-control)

## E7-1. Executive Summary and Governance Context

This BCP/DRP establishes CredEntry's approach to ensuring uninterrupted delivery of the WA Government Digital Wallet and Verifiable Credentials Solution.

- Supports **Digital WA Strategy** by ensuring seamless digital service delivery.
- Meets **WA Cyber Security Policy** requirements through defence-in-depth resilience.
- Enables **ServiceWA Integration** by ensuring continuous wallet functionality.
- Maintains **citizen trust** by protecting credential availability, security, and privacy during service disruptions.

Governed under clauses **47 (Business Continuity)** and **48 (Disaster Recovery)** of the Agreement.

## E7-2. Standards and Regulatory Compliance

Aligned with:

- ISO/IEC 22301:2019 (Business Continuity)
- ISO/IEC 27001:2022 (Information Security)
- ISO/IEC 18013-5:2021 / 18013-7:2024 (mDL standards)
- ISO/IEC 23220-1:2023 (mobile eID)
- ISO/IEC 12207:2017 (software lifecycle)
- ISO/IEC 29100:2024 (privacy)
- ISO/IEC 19790:2025 (crypto modules)
- ISO 9001:2015 / 90003:2018 (quality management)
- ACSC Essential Eight, IRAP PROTECTED, OWASP ASVS, W3C Verifiable Credentials, TDIF 4.8, eIDAS 2.0

**Certification roadmap:** ISO/IEC 27001 re-certification (Pilot Phase), IRAP PROTECTED alignment.

## E7-3. Scope and Coverage

**Within Scope**: SaaS wallet platform, SDK integration, credential lifecycle services, PKI, APIs (IdX, DTP), admin dashboard, monitoring, WA-based support, backup & recovery.  
**Out of Scope**: ServiceWA app infrastructure (Adapptor), source systems, end-user devices, GovNext-IP.

## E7-4. Risk Assessment & Business Impact Analysis

### Critical Functions Priority Matrix

| Function | Criticality | Citizen Impact | Agency Impact | Priority |
|----------|-------------|----------------|---------------|----------|
| Credential Verification | Critical | High | High | 1 |
| Real-time Revocation | Critical | High | High | 1 |
| Credential Issuance | High | Medium | High | 2 |
| SDK/API Endpoints | High | High | Medium | 2 |
| Admin Dashboard | Medium | Low | Medium | 3 |
| Analytics & Reporting | Low | Low | Low | 4 |

**Threat Landscape**: APTs, ransomware, DDoS, supply chain risks (Azure SDKs), social engineering, Azure outages, database corruption, config errors, key staff unavailability.

**Impact Scenarios**:

- Complete Service Outage: RTO 4 hrs, RPO 1 hr.
- Regional Failover: RTO 4 hrs, zero data loss.

## E7-5. Business Continuity Strategy

### Resilience Architecture

- Multi-region active-active Azure deployment.
- Tenant isolation, per-agency cryptographic keys.
- Zero-trust model (MFA, RBAC, TLS 1.3, AES-256).
- Continuous monitoring (Microsoft Sentinel).

### Data Sovereignty

- All data stored in Australian regions.
- Keys in Azure HSM (Australia only).
- Retention: 7 yrs logs, 35-day PITR.
- Testing: monthly restores, quarterly failovers.

## E7-6. Service Levels & Recovery Objectives

| Service Component | Availability | RTO | RPO | Measurement |
|-------------------|--------------|-----|-----|-------------|
| Credential Verification | 99.9% | 4 hrs | 1 hr | Monthly |
| Revocation Services | 99.9% | 4 hrs | 1 hr | Monthly |
| Credential Issuance | 99.8% | 8 hrs | 2 hrs | Monthly |
| SDK/API Endpoints | 99.8% | 8 hrs | 4 hrs | Monthly |
| Admin Dashboard | 99.5% | 24 hrs | 12 hrs | Monthly |

## E7-7. Incident Response & Recovery

- **Priority 1:** Response 15 min, resolution 2–4 hrs.
- **Escalation path:** L1 Perth Support → L2 Tech Lead → Incident Manager → Customer Contract Manager.
- **Customer notification:** DPC Contract Manager informed within 15 min.
- **Failover:** Automatic Azure Front Door redirection; manual E2E verification.
- **Security response:** Immediate containment, forensic evidence, OAIC within 72 hrs if privacy breach.
- **Records:** Stored in SharePoint, retained per ISO/IEC 27001.

## E7-8. Roles & Responsibilities (Recovery Operations)

| Role | Responsibilities | Primary Contact | Alternate Contact |
|------|------------------|-----------------|-------------------|
| **Incident Manager** | Overall coordination, DPC liaison | **Justin Hancock – Project Delivery Lead** | **Shelby Long – Implementation Specialist** |
| **Infrastructure Recovery Lead** | Azure infrastructure, regional failover | **Rodrigo Miranda – FullStack DevOps** | **Marcus Abreu – Senior Solution Architect** |
| **Application Recovery Lead** | Application fixes, deployments | **Marcus Abreu – Senior Solution Architect** | **Marisa Cardoso – Quality Assurance** |
| **Security & IR Lead** | Containment, forensic analysis, OAIC notifications | **Flavia C – Security & Compliance Officer** | **Shelby Long – Implementation Specialist** |
| **Data Recovery Specialist** | Backup restore, DB validation | **Zachariah Adams – Technical Support Lead** | **Credential Management / DB Admin (internal team)** |
| **Communications Lead** | Stakeholder and DPC comms | **Shelby Long – Implementation Specialist** | **WA Contract Manager (DPC)** |
| **Customer Approval** | Formal government liaison | **DPC Contract Manager** | **DPC Performance Manager** |

## E7-9. Testing & Continuous Improvement

- **Monthly:** Backup restores, service failover, vulnerability scans.
- **Quarterly:** Regional failover drills, tenant isolation checks, IdX/ServiceWA integration tests.
- **Annual:** DR simulation, external audit, penetration testing.
- **Post-incident:** RCA within 5 business days; BCP/DRP updated within 10 days.

## E7-10. Organisational Capability

- 24/7 WA-based support centre (Perth).
- Project Delivery Lead: WA-based, 15-min escalation.
- Certified Security IR team (CISSP/CISM).
- Development team 2-hr integration response.
- No subcontractors: full accountability.

## E7-11. Compliance & Assurance

- Privacy Act 1988 (APPs), Australian Digital ID Act 2024.
- Quarterly compliance reports (ISO, Essential Eight, pen test results).
- Regular assurance to DPC via governance forums.

## E7-12. Commercial Considerations

- Service credits for SLA breaches: 5–25% of monthly fee.
- Insurance: Cyber liability & professional indemnity up to $50M.

## E7-13. Document Control

- Classification: OFFICIAL Sensitive
- Review Cycle: Quarterly; annual full review.
- Next Review: December 2025
- Owner: WA Project Delivery Lead
- Customer Approval: DPC Contract Manager

---

# Appendix E.8 - Recovery Runbooks & Checklists

## 1. Purpose and Scope

This appendix defines the **operational recovery procedures and checklists** that underpin the Business Continuity and Disaster Recovery Plan (Appendix E.7).

These runbooks ensure that incident response, crisis communications, and disaster recovery are executed consistently and within the Recovery Time Objectives (RTOs) and Recovery Point Objectives (RPOs) defined in:

- **Service Level Agreement (Appendix E.1)**
- **BCP/DRP (Appendix E.7)**
- **Escalation & Communications Templates (Appendix E.5)**

## 2. General Incident Response & Management

### 2.1 Detection & Initial Triage Checklist

- Continuous monitoring (Azure Monitor, Sentinel SIEM/SOAR, Event Hubs).
- Real-time alert review.
- 24/7 L1 Perth Support centre intake.
- Severity classification:
  - **Priority 1 (Critical):** Outage, verified breach, credential verification failure.
  - **Priority 2 (High):** Significant but contained degradation.
  - **Priority 3 (Moderate/Low):** Limited impact.

### 2.2 Escalation Checklist

- Notify **DPC Contract Manager within 15 minutes** for Priority 1 incidents.
- Escalation: L1 Perth Support → L2 Tech Lead → Incident Manager → Customer Contract Manager.
- Specialist teams engaged (Security IR, DevOps, Project Manager).
- Notify external dependencies (IdX, DTP, OEM wallets, CAs) as required.

### 2.3 Crisis Communication Checklist

- Notify internal leadership, legal, comms.
- Notify **DPC Contract Manager within 15 minutes (P1)**.
- OAIC notification within 72 hours if Privacy Act breach.
- Use **pre-approved templates (Appendix E.5)**.
- Seek DPC approval before public disclosure.

## 3. Recovery Runbooks for Critical Functions

### 3.1 Complete Service Outage (Priority 1)

- Trigger: Core outage (verification, revocation).
- Restore via Azure Front Door failover (≤2h RTO, ≤15m RPO).
- Validate PostgreSQL, Storage, PKI synchronisation.
- Notify stakeholders with ETA.
- RCA post-restoration.

### 3.2 Regional Failover

- Trigger: Outage in one sovereign region.
- Redirect via Azure Front Door.
- Confirm replication of storage/DB, HSM integrity.
- Validate Redis, Service Bus, Functions.
- Restore service ≤4h RTO, zero data loss.

### 3.3 Data Corruption/Loss

- Trigger: Corruption, deletion, degradation.
- Isolate systems; restore PostgreSQL PITR / backups.
- Validate via hash checks.
- Notify stakeholders, conduct RCA.

### 3.4 Security Breach Scenarios

(a) **Ransomware/Malware** – Isolate, preserve logs, restore clean backups, run post-remediation scans.  
(b) **DDoS Attack** – Engage Azure DDoS Protection Standard, apply rate limits, monitor abnormal traffic.  
(c) **Insider Threat** – Immediately revoke access via JIT/RBAC, conduct forensic analysis, notify DPC.

For all: Notify OAIC if PII compromised; RCA + PIR within 5 business days.

### 3.5 PKI / Key Compromise

- Detect via Key Vault monitoring.
- Revoke compromised certs; generate new HSM keys.
- Publish updated trust lists.
- Re-issue affected credentials.
- Notify stakeholders and issuers.

## 4. Regular Testing & Maintenance Checklists

- **Monthly:** Backup restoration tests; service failover drills; vulnerability scans.
- **Quarterly:** Regional failover drills; IdX/ServiceWA integration tests.
- **Annual:** Full DR simulation; penetration test; third-party compliance audit.
- **Semi-Annual:** Tabletop exercises (decision-making & comms).
- **Secure Development:** Continuous SAST/DAST scans, code reviews, OWASP ASVS compliance.

## 5. Roles & Responsibilities (Recovery Operations)

| Role | Responsibilities | Primary Contact | Alternate Contact |
|------|------------------|-----------------|-------------------|
| **Incident Manager** | Overall coordination, DPC liaison | **Justin Hancock – Project Delivery Lead** | **Shelby Long – Implementation Specialist** |
| **Infrastructure Recovery Lead** | Azure infrastructure, regional failover | **Rodrigo Miranda – FullStack DevOps** | **Marcus Abreu – Senior Solution Architect** |
| **Application Recovery Lead** | Application fixes, deployments | **Marcus Abreu – Senior Solution Architect** | **Marisa Cardoso – Quality Assurance** |
| **Security & IR Lead** | Containment, forensic analysis, OAIC notifications | **Flavia C – Security & Compliance Officer** | **Shelby Long – Implementation Specialist** |
| **Data Recovery Specialist** | Backup restore, DB validation | **Zachariah Adams – Technical Support Lead** | **Credential Management / DB Admin (internal team)** |
| **Communications Lead** | Stakeholder and DPC comms | **Shelby Long – Implementation Specialist** | **WA Contract Manager (DPC)** |
| **Customer Approval** | Formal government liaison | **DPC Contract Manager** | **DPC Performance Manager** |

## 6. External Dependencies & SLAs

| Dependency | Function | SLA / RTO | Contact Protocol |
|------------|----------|-----------|------------------|
| IdX/DTP | Identity verification | 2h SLA | Notify via DGov channel |
| OEM Wallets | Wallet integration | 4h SLA | Vendor support portal |
| Certificate Authorities | PKI trust | 24h SLA | CA emergency hotline |

## 7. Document Control

- **Version:** 2.0 (or latest)
- **Last Updated:** September 2025
- **Next Review:** December 2025 (quarterly); full annual review 2026
- **Owner:** WA Project Delivery Lead
- **Storage:** SharePoint ISMS (restricted access)
- **Approval:** DPC Contract Manager

---

# Appendix E.9 - Standards Compliance Mapping

## Table of Contents

1. [Purpose and Scope](#e9-1-purpose-and-scope)
2. [Business Continuity & Disaster Recovery Standards](#e9-2-business-continuity--disaster-recovery-standards)
3. [Information Security & Cyber Resilience Standards](#e9-3-information-security--cyber-resilience-standards)
4. [Digital Identity & Interoperability Standards](#e9-4-digital-identity--interoperability-standards)
5. [Privacy & Data Protection Standards](#e9-5-privacy--data-protection-standards)
6. [Quality & Software Engineering Standards](#e9-6-quality--software-engineering-standards)
7. [Technical Implementation Standards](#e9-7-technical-implementation-standards)
8. [Compliance Reporting & Assurance](#e9-8-compliance-reporting--assurance)
9. [Standards Compliance Mapping (Matrix View)](#e9-9-standards-compliance-mapping-matrix-view)

## E9-1. Purpose and Scope

This appendix outlines CredEntry's alignment with **international standards, Australian frameworks, and WA Government requirements** relevant to the Digital Wallet and Verifiable Credentials Solution.

It demonstrates how CredEntry's Business Continuity Plan (BCP), Disaster Recovery Plan (DRP), and platform design meet — or are progressing towards — critical compliance obligations.

## E9-2. Business Continuity & Disaster Recovery Standards

- **ISO/IEC 22301:2019 – Business Continuity Management Systems**  
  CredEntry's BCP/DRP is explicitly structured around ISO 22301, with regular testing (monthly backup and failover, quarterly regional drills, annual full DR simulation). Post-incident reviews and continuous updates ensure ongoing alignment.

- **Digital Wallet and Verifiable Credentials Agreement (Clauses 47 & 48)**  
  CredEntry fulfils contractual obligations for Business Continuity and Disaster Recovery Services, with immediate notification, mitigation, and restoration. All costs for recovery actions arising from CredEntry systems are borne by CredEntry.

## E9-3. Information Security & Cyber Resilience Standards

- **ISO/IEC 27001:2022 – Information Security Management Systems**  
  Re-certification in progress (completion during Pilot Phase). ISMS practices include encrypted credential storage, RBAC, secure deletion, audit logging, and controlled SharePoint repositories.

- **WA Cyber Security Policy (2024)**  
  Defence-in-depth model with 24-hour incident reporting, interoperable open data formats, and configuration exportability.

- **ACSC Essential Eight**  
  MFA, patching, application control, and restricted admin privileges implemented to Maturity Level One (or equivalent), tracked quarterly.

- **IRAP PROTECTED**  
  Alignment in progress with ASD-accredited assessors engaged during Pilot Phase.

- **ISO/IEC 19790:2025 – Cryptographic Modules**  
  AES-256 and TLS 1.3 encryption, key rotation, HSM integration via Azure Key Vault.

- **OWASP ASVS & OWASP API Security Top 10**  
  APIs and SDKs tested for vulnerabilities, remediation targets defined in SLA.

- **Zero-Trust Architecture**  
  MFA, just-in-time RBAC, encryption, and continuous monitoring.

- **Secure by Design (ACSC Foundations)**  
  Secure release management and vulnerability remediation integrated into SLA.

- **Security Incident Response**  
  Immediate containment, forensic preservation, OAIC notification ≤72h, RCA and PIR reporting.

- **Vulnerability & Penetration Testing**  
  Monthly automated scans, annual penetration tests, automated SAST/DAST in CI/CD.

- **Key Management**  
  Sovereign Azure HSM key lifecycle (generation, rotation, destruction). SaaS IACA PKI managed by CredEntry; Root CA oversight retained by DPC.

## E9-4. Digital Identity & Interoperability Standards

- **ISO/IEC 18013-5:2021 / 18013-7:2024 (Mobile Driving Licence)** – mDL-ready with offline/online presentation, OIDC4VP support.

- **ISO/IEC 23220-1:2023 (Mobile eID Architectures)** – Modular, supports revocation, polling, event-driven issuance.

- **W3C Verifiable Credentials & DID** – Rapid updates (<5 min), selective disclosure, DID resolution.

- **Trusted Digital Identity Framework (TDIF 4.8)** – Data minimisation, governance reporting.

- **eIDAS 2.0** – PKI trust, issuer revocation, schema templates, remote verification.

- **OID4VCI & OIDC4VP** – APIs/SDKs conform to issuance and presentation protocols.

- **Australian Digital ID Act 2024** – Designed for compliance with interoperability, audit trails, and roadmap for Auth Level 2 biometrics.

## E9-5. Privacy & Data Protection Standards

- **Privacy Act 1988 (Cth) & APPs** – Privacy-by-design, selective disclosure, OAIC notification ≤72h.

- **ISO/IEC 29100:2024 – Privacy Framework** – Data minimisation, purpose limitation, attribute-level selective disclosure.

- **GDPR** – Data minimisation, user dashboards, delegated credential rights.

- **Data Sovereignty** – All data and cryptographic material stored in Australian sovereign regions.

- **Data Minimisation & Consent** – Consent receipts, obfuscation of PII.

## E9-6. Quality & Software Engineering Standards

- **ISO 9001:2015 / 90003:2018** – Quality management aligned to onboarding and production readiness. Certification targeted by end of Pilot Phase.

- **ISO/IEC 12207:2017** – Software lifecycle processes aligned to development and maintenance. Certification targeted by end of Pilot.

- **ITIL v4** – Incident response, SLA management, remediation aligned to ITIL service practices.

## E9-7. Technical Implementation Standards

- **OpenAPI 3** – API documentation for all endpoints.

- **WCAG 2.2+** – Interfaces tested to WCAG 2.2 AA accessibility standards.

- **Webhooks** – Event-driven credential lifecycle support.

- **SAML 2.0** – Configurable alongside OIDC IdPs.

## E9-8. Compliance Reporting & Assurance

- **Quarterly Compliance Reports** – Covering ISO/IEC 27001 alignment, Essential Eight maturity, pen test results, privacy compliance.

- **Annual Security Certification** – Independent audit reports provided annually.

- **Proof-of-Operation & Pilot Evaluation** – Evaluated against technical, integration, security, privacy, and performance criteria.

- **Audit Trail Transparency** – Administrative and credential logs made available to DPC upon request.

## E9-9. Standards Compliance Mapping (Matrix View)

| Standard / Framework | Requirement | CredEntry Evidence | Status |
|---------------------|-------------|-------------------|---------|
| ISO/IEC 22301:2019 (Business Continuity) | Maintain BCP/DRP with testing & continuous improvement | Monthly backup/ failover tests, quarterly regional drills, annual DR simulation, RCA & updates | Aligned |
| WA Digital Wallet Agreement (Clauses 47 & 48) | Immediate notification, mitigation, restoration at no cost if CredEntry-related | Escalation process, DPC notified ≤ 15 min for P1 incidents | Aligned |
| ISO/IEC 27001:2022 (ISMS) | Secure ISMS with controls for access, logging, encryption | Encrypted credential storage, RBAC, audit logs, SharePoint record keeping | In progress: re-certification by end Pilot Phase |
| WA Cyber Security Policy (2024) | Defence-in-depth, 24h incident reporting, interoperability | Sentinel, MFA, interoperable formats, 24h reporting commitment | Aligned |
| ACSC Essential Eight | MFA, patching, restricted admin, app control | Implemented to Maturity Level One or equivalent; tracked quarterly | Aligned |
| IRAP PROTECTED | Alignment to PROTECTED controls | Independent ASD-accredited assessors engaged during Pilot Phase | In progress |
| ISO/IEC 19790:2025 (Crypto modules) | Secure cryptographic modules | AES-256, TLS 1.3, key rotation, HSM via Azure Key Vault | Aligned |
| OWASP ASVS / API Top 10 | Secure coding & API hardening | SAST/DAST in CI/CD, API testing, SLA for remediation | Aligned |
| Zero-Trust Architecture | Strong identity & network segmentation | MFA, JIT RBAC, Sentinel monitoring | Aligned |
| ISO/IEC 18013-5 & -7 (mDL) | Mobile Driving Licence compliance (offline/online) | SDKs support OIDC4VP, offline presentation, mutable fields | Aligned |
| ISO/IEC 23220-1:2023 (Mobile eID) | Modular, interoperable identity architecture | Event-driven issuance, revocation, polling, PII minimisation | Aligned |
| W3C VCDM & DID | Verifiable Credentials model, DID resolution | VC issuance/revocation < 5 min, selective disclosure, DID support | Aligned |
| TDIF 4.8 | Digital ID interoperability & governance | Admin dashboards, reporting, data minimisation | Aligned |
| eIDAS 2.0 | Trust, revocation, mutable fields, remote verification | PKI trust mgmt, schema templates, eIDAS conformance testing | Aligned |
| OID4VCI / OIDC4VP | Credential issuance & presentation protocols | APIs/SDKs align to OID4VCI & OIDC4VP workflows | Aligned |
| Australian Digital ID Act 2024 | Interoperability, audit trails, optional biometrics | Wallet auditability, Auth Level 2 roadmap | Aligned |
| Privacy Act 1988 (APPs) | Privacy-by-design, OAIC notification ≤ 72h | Selective disclosure, consent logs, OAIC compliance | Aligned |
| ISO/IEC 29100:2024 (Privacy Framework) | Data minimisation, purpose limitation | Attribute-level selective disclosure, PII obfuscation | Aligned |
| GDPR | Data protection, purpose limitation, audit logs | Transaction dashboards, consent management | Aligned |
| Data Sovereignty (WA Govt) | Local data hosting | All data in Australian sovereign Azure regions | Aligned |
| ISO 9001:2015 / 90003:2018 | QMS for software & onboarding | Onboarding aligned; certification targeted end Pilot Phase | In progress |
| ISO/IEC 12207:2017 | Software lifecycle alignment | Development, testing, maintenance practices aligned | In progress |
| ITIL v4 | Incident mgmt & remediation SLAs | Draft SLA framework with explicit remediation timelines | Aligned |
| OpenAPI 3 | Standard API documentation | All platform APIs documented via OpenAPI spec | Aligned |
| WCAG 2.2+ | Accessibility compliance | Web interface tested to WCAG 2.2 AA | Aligned |
| Webhooks | Event-driven issuance & storage | Webhooks for credential lifecycle | Aligned |
| SAML 2.0 | Standards-based IdP integration | Configurable alongside OIDC IdPs | Aligned |
| Quarterly Compliance Reports | Provide updates to DPC | Reports on ISO, Essential Eight, penetration tests, privacy | Committed |
| Annual Security Certification | Independent certification provided to DPC | Annual ISO/Essential Eight/IRAP audit | Committed |
| Proof-of-Operation / Pilot Evaluation | Demonstrate operational readiness | Conformance testing against technical & security criteria | In progress |

---

**END OF COMBINED DOCUMENTATION**
