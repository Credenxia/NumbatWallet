# Change Pack: qualitative-requirements

**Run**: 2025-09-12 11:26:00  
**Commit**: 7f92c3ac75a3029ed834d2dc331138aa17dbada0

## Wiki Edits

### 1. Security Incident Reporting Timeline
- **File**: ../NumbatWallet.wiki/Support-Model.md
- **Section Anchor**: "## Incident Management"
- **Operation**: Replace
- **Find (exact)**:
  ```
  | P1 - Critical | System-wide outage, data breach, security incident | 30 minutes | 2 hours | 4 hours | Executive escalation |
  ```
- **With (exact)**:
  ```
  | P1 - Critical | System-wide outage, data breach, security incident | Immediate | 2 hours | 4 hours | Executive escalation |
  ```
- **Rationale**: Newer input in temp/filestoassess/Qualitative Requirements (b).pdf requires immediate notification

### 2. Post-Incident Report Timeline
- **File**: ../NumbatWallet.wiki/Support-Model.md
- **Section Anchor**: "### Post-Incident Review"
- **Operation**: Replace
- **Find (exact)**:
  ```
  - Formal PIR delivered within 5 business days
  ```
- **With (exact)**:
  ```
  - Formal incident report delivered within 24 hours
  - Comprehensive PIR delivered within 5 business days
  ```
- **Rationale**: PDF specifies 24-hour formal report requirement, maintaining 5-day PIR for detailed analysis

### 3. Pilot Stage 2 User Count
- **File**: ../NumbatWallet.wiki/Testing-Strategy.md
- **Section Anchor**: "### Pilot Phases"
- **Operation**: Replace
- **Find (exact)**:
  ```
  2. **Restricted Pilot (3 months)**: Limited to government users
  ```
- **With (exact)**:
  ```
  2. **Restricted Pilot (3 months)**: Limited to ~50 government users
  ```
- **Rationale**: PDF specifies approximately 50 government testers for Stage 2

### 4. Pilot Stage 3 User Count
- **File**: ../NumbatWallet.wiki/Testing-Strategy.md
- **Section Anchor**: "### Pilot Phases"
- **Operation**: Replace
- **Find (exact)**:
  ```
  3. **Preview Pilot (Remaining 9 months)**: Controlled group of citizens
  ```
- **With (exact)**:
  ```
  3. **Preview Pilot (Remaining 9 months)**: Controlled group of 200+ citizens
  ```
- **Rationale**: PDF specifies 200+ testers for Stage 3 Preview Pilot

### 5. Monthly Reporting Timeline
- **File**: ../NumbatWallet.wiki/Pricing-Assumptions.md
- **Section Anchor**: "## Reporting & Transparency"
- **Operation**: Replace
- **Find (exact)**:
  ```
  ### Monthly Consumption Reports
  - Detailed usage breakdown by credential type
  ```
- **With (exact)**:
  ```
  ### Monthly Consumption Reports
  - Delivered within 5 business days of month-end
  - Detailed usage breakdown by credential type
  ```
- **Rationale**: PDF requires reports within 5 business days of month-end

### 6. Billing Accuracy Guarantee
- **File**: ../NumbatWallet.wiki/Support-Model.md
- **Section Anchor**: "## Service Level Agreements"
- **Operation**: Add after "### Availability SLAs"
- **Find (exact)**:
  ```
  ### Performance SLAs
  ```
- **With (exact)**:
  ```
  ### Billing Accuracy SLA
  - **Target**: 99.9% billing accuracy guaranteed
  - **Measurement**: Monthly reconciliation of consumption vs charges
  - **Credits**: 5% of monthly fee for each 0.1% below target
  - **Audit**: Customer retains right to independent verification
  
  ### Performance SLAs
  ```
- **Rationale**: PDF guarantees 99.9% billing accuracy with service credits

### 7. ISO 22301 Compliance
- **File**: ../NumbatWallet.wiki/Compliance-Matrix.md
- **Section Anchor**: "## ISO Standards"
- **Operation**: Add after ISO 27001 entry
- **Find (exact)**:
  ```
  | ISO/IEC 27002:2022 | Information Security Controls | Implemented | Security control selection |
  ```
- **With (exact)**:
  ```
  | ISO/IEC 27002:2022 | Information Security Controls | Implemented | Security control selection |
  | ISO/IEC 22301:2019 | Business Continuity Management | In Progress | Required for Pilot Phase |
  ```
- **Rationale**: PDF requires ISO 22301 certification

### 8. ISO 22301 in Security Documentation
- **File**: ../NumbatWallet.wiki/Security-Privacy-Compliance.md
- **Section Anchor**: "### Compliance Certifications"
- **Operation**: Replace
- **Find (exact)**:
  ```
  - ISO/IEC 27001:2022 (Information Security Management)
  - ISO/IEC 27017:2015 (Cloud Security)
  - ISO/IEC 27018:2019 (Cloud Privacy)
  ```
- **With (exact)**:
  ```
  - ISO/IEC 27001:2022 (Information Security Management)
  - ISO/IEC 22301:2019 (Business Continuity Management)
  - ISO/IEC 27017:2015 (Cloud Security)
  - ISO/IEC 27018:2019 (Cloud Privacy)
  ```
- **Rationale**: Add ISO 22301 to compliance certification list

### 9. Biometric Module Pricing
- **File**: ../NumbatWallet.wiki/Pricing-Assumptions.md
- **Section Anchor**: "## Optional Modules"
- **Operation**: Add new section after "### Additional Services"
- **Find (exact)**:
  ```
  ## Implementation & Transition
  ```
- **With (exact)**:
  ```
  ## Optional Modules
  
  ### Biometric Verification Module
  - **One-time Implementation**: $244,000 (includes architecture, compliance, testing)
  - **Per-Verification Pricing**:
    - Small Tier: $0.35 per verification
    - Medium Tier: $0.25 per verification
    - Large Tier: $0.15 per verification
  - **Includes**: Liveness detection, anti-spoofing, 1:1 matching
  - **Compliance**: ISO/IEC 19794, NIST SP 800-63B
  
  ## Implementation & Transition
  ```
- **Rationale**: PDF specifies biometric module pricing structure

### 10. Update POA Plan with User Counts
- **File**: ../NumbatWallet.wiki/POA-Plan.md
- **Section Anchor**: "## Pilot Phase Overview"
- **Operation**: Replace
- **Find (exact)**:
  ```
  - Stage 2: Restricted Pilot (government users)
  - Stage 3: Preview Pilot (controlled citizens)
  ```
- **With (exact)**:
  ```
  - Stage 2: Restricted Pilot (~50 government users)
  - Stage 3: Preview Pilot (200+ controlled citizens)
  ```
- **Rationale**: Align POA Plan with specific user counts from PDF

## Cross-Propagation

### Update Master PRD (Home.md)
- **Also update**: ../NumbatWallet.wiki/Home.md
- **Sections**: Service Levels, Pilot Structure, Compliance Requirements
- **Changes**: Reflect 24-hour incident reporting, user counts, ISO 22301

### Update Risk Matrix
- **Also update**: ../NumbatWallet.wiki/Risk-Matrix.md
- **Section**: Compliance Risks
- **Changes**: Add ISO 22301 certification timeline risk (Medium priority)

### Update Delivery Assumptions
- **Also update**: ../NumbatWallet.wiki/Delivery-Assumptions-Narrative.md
- **Section**: Pilot Assumptions
- **Changes**: Document specific user counts as planning assumptions

### Update Training Plan
- **Also update**: ../NumbatWallet.wiki/Training-Plan.md
- **Section**: Audience Sizing
- **Changes**: Reference 50 government users for initial training cohort

## Non-Wiki Source Edits

### Not Applicable
- Document is from tender submission (PDF)
- No source documents to update in this assessment
- All changes apply to Wiki documentation only

## Validation Checklist

Before applying changes:
- [ ] Backup current Wiki state
- [ ] Verify all file paths exist
- [ ] Test find strings match exactly (including whitespace)
- [ ] Review with stakeholders for blocking issues (24-hour report, ISO 22301)
- [ ] Confirm biometric pricing aligns with commercial model
- [ ] Validate user counts with infrastructure sizing

## Implementation Notes

1. **Priority 1 (Blocking)**: Security incident reporting timeline - contractual obligation
2. **Priority 2 (High)**: ISO 22301 requirement - may affect contract award
3. **Priority 3 (Medium)**: User counts, billing accuracy, monthly report timing
4. **Priority 4 (Low)**: Biometric pricing - optional module

## Quality Gates

- All changes preserve existing formatting and structure
- No destructive edits to unrelated content
- Cross-references updated consistently
- Version history maintained via Git commits

---
Generated by Cross-Validation Agent v1.0
Source Document: Qualitative Requirements (b) - Suitability of Proposed Solution and Services.pdf
Target: NumbatWallet.wiki repository