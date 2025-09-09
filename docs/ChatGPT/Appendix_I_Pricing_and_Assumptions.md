# Appendix I – Pricing & Assumptions

[← Back to Master PRD](PRD_Master.md#pricing-options)

The tender requests Pilot pricing and provides two methodologies (Option 1 and Option 2) for the Full Production environment.  This appendix outlines our pricing structure, assumptions and consumption reporting model.  All prices are in **AUD** and inclusive of GST.  Detailed cost breakdowns will be provided in the commercial response.

## Table of Contents
- [Pilot Pricing](#pilot-pricing)
- [Team Composition & AI Augmentation](#team-composition--ai-augmentation)
- [Option 1 - Consumption-based Pricing](#option-1--consumption-based-pricing-full-production)
- [Option 2 - Fixed-fee Pricing](#option-2--fixed-fee-pricing-full-production)
- [Assumptions](#assumptions)
- [Consumption Reporting](#consumption-reporting)

## Pilot pricing

The Pilot Phase lasts **12 months** and includes one credential type and up to 50 users during the restricted stage, expanding during the preview stage.  We leverage **AI-augmented development** to deliver superior value with a lean, highly productive team.

### AI-Augmented Team Cost Analysis

| Team Member | FTE | Base Annual | With 15% Contingency | Monthly Cost | Role Description |
| --- | --- | --- | --- | --- | --- |
| Project Manager | 1.0 | $180,000 | $207,000 | $17,250 | Stakeholder management, reporting, coordination |
| Business Analyst | 1.0 | $100,000 | $115,000 | $9,583 | Requirements gathering, documentation, process mapping |
| Tester/QA | 1.0 | $100,000 | $115,000 | $9,583 | Test planning, UAT coordination, quality assurance |
| Architect/Developer | 1.0 | $180,000 | $207,000 | $17,250 | 50% architecture, 50% hands-on development, L3 support |
| Full-Stack Developer | 1.0 | $160,000 | $184,000 | $15,333 | Backend + mobile SDK development, highly productive with AI |
| DevOps/Security Engineer | 1.0 | $180,000 | $207,000 | $17,250 | Infrastructure, security, can assist with development |
| Technical Support | 0.5 | $50,000 | $57,500 | $4,792 | Part-time technical support, documentation |
| **Total Team Cost** | **6.5** | **$950,000** | **$1,092,500** | **$91,041** | **15% contingency covers super, leave, replacements** |

### Pilot Phase Pricing Breakdown

| Item | Quantity / Unit | Cost (inc GST) | Notes |
| --- | --- | --- | --- |
| Setup, implementation & integration | Once | $250,000 | Environment provisioning, SDK development, ServiceWA integration |
| Infrastructure development | Once | $150,000 | Custom platform components, integrations, initial setup |
| Team costs (inc. 15% contingency) | 12 months | $91,041 / month | 6.5 FTE AI-augmented team with super, leave coverage |
| Platform infrastructure (inc. AI tools) | 12 months | $18,000 / month | Azure + AI tools (Copilot, Claude/GPT-4 APIs) merged |
| Security & compliance | 12 months | $6,000 / month | Penetration test (1×), audits, vulnerability scanning |
| Training & documentation | Included | – | Materials, sessions for DGov; knowledge-base setup |
| Project contingency (10%) | 12 months | $10,750 / month | Risk mitigation and unforeseen requirements |
| **Sub-total (Cost)** | – | **$1,493,000** | Total costs for 12 months |
| **Margin (25%)** | – | **$373,250** | Reduced margin for competitive pilot pricing |
| **Total Pilot Price** | – | **$1,866,250** | Highly competitive with AI-augmented efficiency |

### Productivity Gains with AI

| Activity | Traditional Team | AI-Augmented Team | Efficiency Gain |
| --- | --- | --- | --- |
| API Development | 40 hours/endpoint | 22 hours/endpoint | 45% |
| Mobile SDK Features | 60 hours/feature | 36 hours/feature | 40% |
| Infrastructure Setup | 80 hours | 40 hours | 50% |
| Documentation | 20 hours/module | 8 hours/module | 60% |
| Code Reviews | 4 hours/PR | 2 hours/PR | 50% |
| Testing | 30% of dev time | 15% of dev time | 50% |

## Team Composition & AI Augmentation

Our team leverages cutting-edge AI tools to achieve exceptional productivity:

### AI-Augmented Development Approach
- **Integrated AI Tools**: GitHub Copilot, Claude/GPT-4 APIs included in infrastructure costs
- **Productivity Gain**: 40-50% across all development activities
- **Team Efficiency**: 5.5 FTE delivers output equivalent to 8-9 traditional FTE
- **Cross-functional Capability**: DevOps/Security engineer can assist with development
- **ROI**: Saves equivalent of 3-3.5 FTE (~$500,000/year)
- **Note**: AI costs merged into infrastructure for simplified billing

### Phase-Specific Team Evolution

| Phase | Team Size | Composition | Annual Cost | Support Model |
| --- | --- | --- | --- | --- |
| Pilot | 6.5 FTE | PM, BA, Tester, Arch/Dev, 1×Dev, DevOps/Sec, 0.5×Support | $1,092,500 | Business hours + on-call |
| Production Y1 | 3.6 FTE | Account Mgr, 0.3×BA/Test, 0.5×Arch, 0.5×Dev, 0.5×DevOps, 1×Support | $575,000 | 24/7 with AI chatbot |
| Production Y2 | 3.6 FTE | Same with 3% CPI increase | $592,250 | 24/7 premium support |
| Production Y3+ | 3.6-4.5 FTE | Scale support as needed, annual CPI increases | $610,000+ | Full NOC + AI automation |

## Option 1 – Infrastructure Plus Overheads (Full Production)

Option 1 provides predictable fixed annual costs with our AI-augmented team delivering exceptional value. This option includes all infrastructure, team, and operational costs with transparent pricing.

### Fixed Annual Pricing

| Deployment | Annual Price | Team Cost (Y1) | Infrastructure | Security | Margin | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| **Small** | $1,500,000 | $575,000 | $300,000 | $140,000 | 48% | Lean team, high efficiency |
| **Medium** | $2,100,000 | $592,250 | $600,000 | $200,000 | 50% | Y2 with CPI increase |
| **Large** | $2,800,000 | $610,000 | $1,200,000 | $280,000 | 35% | Y3+ scaled support |

### Infrastructure Cost Breakdown

#### Small Deployment ($300,000/year - $25,000/month)
| Component | Monthly Cost | Details |
| --- | --- | --- |
| AKS Cluster (2 nodes) | $5,000 | Single region, auto-scaling |
| PostgreSQL Database | $3,000 | Single instance with backup |
| HSM & Key Vault | $2,500 | Basic HSM for key management |
| Storage & Backups | $2,000 | 500GB storage, daily backups |
| Monitoring & Logging | $1,500 | Basic metrics and alerts |
| Network & CDN | $3,000 | Regional load balancing |
| AI Tools Integration | $3,000 | Copilot, Claude API access |
| DR & Business Continuity | $5,000 | Backup site, 24hr RPO |

#### Medium Deployment ($600,000/year - $50,000/month)
| Component | Monthly Cost | Details |
| --- | --- | --- |
| AKS Cluster (4 nodes) | $10,000 | Multi-AZ, auto-scaling |
| PostgreSQL Database | $8,000 | Primary + read replica |
| HSM & Key Vault | $5,000 | Standard HSM, geo-redundant |
| Storage & Backups | $4,000 | 2TB storage, hourly backups |
| Monitoring & Logging | $3,000 | Advanced observability |
| Network & CDN | $6,000 | Multi-region CDN |
| AI Tools Integration | $4,000 | Enhanced AI capabilities |
| DR & Business Continuity | $10,000 | Active standby, 4hr RPO |

#### Large Deployment ($1,200,000/year - $100,000/month)
| Component | Monthly Cost | Details |
| --- | --- | --- |
| AKS Cluster (8 nodes) | $20,000 | Multi-region, auto-scaling |
| PostgreSQL Database | $20,000 | Clustered, geo-replicated |
| HSM & Key Vault | $10,000 | Premium HSM, multiple regions |
| Storage & Backups | $8,000 | 10TB storage, continuous backup |
| Monitoring & Logging | $6,000 | Full observability suite |
| Network & CDN | $12,000 | Global CDN, DDoS protection |
| AI Tools Integration | $6,000 | Premium AI services |
| DR & Business Continuity | $18,000 | Active-active, 15min RPO |

### Phase-Specific Security Costs

| Phase | Penetration Tests | Vulnerability Scans | Compliance Audits | Support Model |
| --- | --- | --- | --- | --- |
| Pilot | 1× ($20K total) | Monthly ($6K/year) | Self-assessment | Business hours |
| Production Y1 | 2× ($40K/year) | Weekly ($12K/year) | ISO 27001 ($40K) | 24/7 with Account Mgr |
| Production Y2+ | 2× + Red Team ($80K) | Continuous ($24K/year) | ISO + SOC2 ($80K) | 24/7 premium |

## Option 2 – Consumption-Based Pricing (Full Production)

Option 2 aligns costs with actual usage, as recommended in the pricing schedule. A base fee covers platform maintenance and support; variable charges apply per credential event and per active wallet:

### Consumption Pricing Components

| Component | Unit | Small | Medium | Large | Notes |
| --- | --- | --- | --- | --- | --- |
| **Base platform fee** | Per month | $45,000 | $75,000 | $120,000 | Infrastructure and team |
| **Credential issuance** | Per credential | $0.40 | $0.35 | $0.30 | Volume discounts at scale |
| **Presentation & verification** | Per transaction | $0.08 | $0.06 | $0.04 | First 50,000/month included |
| **Credential revocation/update** | Per event | $0.15 | $0.12 | $0.10 | Lifecycle management |
| **Active wallets (MAU)** | Per wallet/month | $0.80 | $0.60 | $0.40 | Progressive discounts |
| **PKI certificate management** | Per tenant | $2,000 | $1,500 | $1,000 | IACA and signing certs |
| **24/7 Support upgrade** | Per month | +$8,000 | +$6,000 | +$5,000 | Enhanced SLAs |
| **AI Chatbot (Tier 0)** | Per month | $3,000 | $5,000 | $8,000 | Handles 60%+ of queries |
| **Optional biometrics** | Per credential | $0.50 | $0.40 | $0.30 | Face/fingerprint |

### Predicted Annual Costs Based on Tender Volumes

| Size | Credentials | Verifications | Active Wallets | Base Fee | Total Annual |
| --- | --- | --- | --- | --- | --- |
| **Small** | 100K × $0.40 = $40K | 500K × $0.08 = $40K | 50K × $0.80 × 12 = $480K | $540K | **$1,100,000** |
| **Medium** | 500K × $0.35 = $175K | 2M × $0.06 = $120K | 200K × $0.60 × 12 = $1,440K | $900K | **$2,635,000** |
| **Large** | 1M × $0.30 = $300K | 5M × $0.04 = $200K | 500K × $0.40 × 12 = $2,400K | $1,440K | **$4,340,000** |

### Consumption Thresholds & Burst Handling

* Charges calculated monthly with detailed reporting via dashboard
* Volume discount tiers automatically applied:
  - 0–100k transactions: Standard rate
  - 100k–500k: 15% discount
  - 500k–1M: 25% discount
  - 1M+: 35% discount
* Burst handling: Auto-scaling accommodates 3× normal load
* Surge pricing (125% rate) only for sustained >200% monthly forecast
* Security events (breaches, emergency revocations): No surge pricing

### Detailed Inclusions by Tier

| Feature | Small | Medium | Large |
| --- | --- | --- | --- |
| Credentials | 100,000 | 500,000 | 1,000,000+ |
| Transactions/year | 500,000 | 2,000,000 | 5,000,000+ |
| Tenant partitions | 1 | 5 | 10+ |
| Team size | 3.6 FTE | 3.6 FTE | 4.5 FTE |
| Infrastructure | Single region | Multi-AZ | Multi-region |
| Availability SLA | 99.5% | 99.9% | 99.95% |
| Support | 24/7 basic | 24/7 standard | 24/7 premium |
| Penetration tests | 2/year | 2/year | 2/year + red team |
| DR capability | 24hr RPO | 4hr RPO | 15min RPO |
| AI Chatbot | Basic | Advanced | Premium + voice |
| Account Manager | Shared | Dedicated | Dedicated + team |

### Additional Services & Rate Card

| Service | Rate | Description |
| --- | --- | --- |
| New tenant onboarding | $75,000 | PKI setup, training, integration (increased from $50K) |
| New credential type | $35,000 | Schema design, testing, deployment |
| Biometrics module | $0.40/credential/year | Face/fingerprint with liveness detection |
| Custom integration | $25,000 base + T&M | API development, testing, documentation |
| Priority support | $15,000/month | Dedicated engineer, 15-min SLA |

### Professional Services Rate Card

| Role | Hourly Rate | Daily Rate | Expertise |
| --- | --- | --- | --- |
| Solution Architect | $250 | $1,800 | Architecture, design, L3 support |
| Senior Developer | $200 | $1,500 | Backend, mobile, integrations |
| Business Analyst | $150 | $1,100 | Requirements, testing, documentation |
| DevOps/Security Engineer | $220 | $1,650 | Infrastructure, security, automation |
| Project Manager | $180 | $1,350 | Coordination, reporting, stakeholder management |
| Technical Support | $120 | $900 | L1/L2 support, troubleshooting |

## Assumptions

### Team & Productivity Assumptions

* **AI-augmented team**: 40-50% productivity gain through GitHub Copilot, Claude/GPT-4
* **Lean structure**: 5.5 FTE pilot, 3.6 FTE production through high efficiency
* **Cross-functional skills**: DevOps/Security engineer can assist with development
* **BA/Tester role**: Combined 0.5 BA + 0.5 Testing for comprehensive quality
* **Account Manager**: Replaces PM in production, maintains stakeholder relationships
* **15% salary contingency**: Covers superannuation, annual leave, and replacements
* **CPI increases**: 3% annual increases for production phase team

### Infrastructure & Operations Assumptions

* **Data sovereignty**: All data remains within Australian Azure regions
* **Hosting costs**: Azure public pricing + 20% contingency buffer
* **Transaction volumes**: Follow tender-defined small/medium/large scenarios
* **Credential complexity**: Standard mDL-like credentials (complex = +30% effort)
* **PKI management**: DGov manages root CA; we manage subordinate CAs
* **Integration scope**: Backend + SDK only (ServiceWA handles frontend)
* **Biometrics**: Optional module for production (not in pilot)
* **Support evolution**: Human-focused pilot → AI-assisted production

### Financial Assumptions

* **Margin targets**: 25% pilot phase, 45-50% production phase
* **AI tools integrated**: Included in infrastructure costs, not separately billed
* **Volume discounts**: Applied automatically at consumption tiers
* **Payment terms**: Monthly in arrears for consumption; quarterly for fixed
* **Currency**: All prices in AUD including GST

## Consumption reporting

### Real-time Dashboard Features

| Metric Category | Metrics Tracked | Update Frequency | Export Format |
| --- | --- | --- | --- |
| Credentials | Issued, revoked, updated, expired | Real-time | CSV, JSON, API |
| Transactions | Presentations, verifications, failures | 5-minute | CSV, JSON, API |
| Performance | API latency, error rates, availability | 1-minute | Grafana, CSV |
| Security | Failed auth, anomalies, threats blocked | Real-time | SIEM, JSON |
| Support | Tickets opened, resolved, SLA status | Hourly | CSV, PDF |
| Costs | Current spend, projections, tier status | Daily | PDF, Excel |

### Billing & Reconciliation

* **Monthly statements**: Detailed breakdown with audit trail
* **Cost allocation**: Per credential type, per agency
* **Predictive analytics**: Forecast next month's usage
* **Budget alerts**: Configurable thresholds with notifications
* **API integration**: Direct feed to DGov financial systems

### ROI Metrics & Value Demonstration

| Metric | Pilot Target | Production Y1 | Production Y2+ |
| --- | --- | --- | --- |
| Cost per credential | $15 | $8 | $4 |
| Cost per verification | $0.50 | $0.20 | $0.08 |
| Support ticket reduction | Baseline | 40% (AI chatbot) | 60% (advanced AI) |
| Deployment time | 3 months | 1 month | 2 weeks |
| System availability | 99.5% | 99.9% | 99.95% |

---

[← Back to Master PRD](PRD_Master.md#pricing-options) | [View Security Details →](Appendix_B_Security_Privacy_Compliance.md) | [View Team Structure →](Appendix_H_ProjectPlan_Team_Effort.md)
## Comprehensive Assumptions for Pricing & Team Structure

### Our Key Assumptions (Not Specified in Tender):

#### Security Testing Frequency
* **Penetration Testing:** 4× annually in production is OUR RECOMMENDATION for best practice, not a tender requirement
* **Rationale:** Quarterly testing aligns with industry standards for government systems handling sensitive data
* **Cost Impact:** $80K/year in production ($20K per test)

#### Team Structure Assumptions
* **15% Salary Contingency:** Covers superannuation (11%), annual leave coverage, and replacement risk
* **AI Productivity Gains:** 40-50% efficiency improvement validated through our experience
* **Cross-functional Capability:** DevOps/Security engineer can assist with development during peak periods
* **BA/Tester Split:** 0.5 FTE BA + 0.5 FTE Testing provides comprehensive quality coverage
* **Account Manager in Production:** Replaces PM role, maintains stakeholder relationships at lower cost

#### Infrastructure Development
* **One-time Cost:** $150,000 for custom platform components not covered in standard setup
* **AI Tools Integration:** $3,000/month merged into infrastructure costs (not separately billed)
* **Infrastructure Scaling:** Assumes Azure public pricing + 20% contingency buffer

#### Production Phase Assumptions
* **Team Reduction:** From 5.5 to 3.6 FTE based on automation and stabilization
* **CPI Increases:** 3% annual salary increases for production years
* **Support Evolution:** Transition from human-centric to AI-assisted support model
* **Margin Adjustment:** 25% pilot (competitive entry), 45-50% production (operational efficiency)

#### Compliance & Certification
* **ISO 27001:** Self-assessment during pilot, full certification in production Year 1
* **SOC2 Type 2:** Production Year 2 onwards
* **IRAP Assessment:** Annual from production Year 1

#### Performance Targets (Our Commitments)
* **API Response Time:** <100ms in production (exceeding tender requirements)
* **Availability:** 99.95% for large deployments (exceeding 99.9% requirement)
* **Support Resolution:** 4-hour SLA for production issues

#### Risk Mitigation
* **10% Project Contingency:** For unforeseen requirements during pilot
* **Replacement Buffer:** 15% salary contingency ensures continuity if team changes
* **AI Tool Fallback:** Can operate without AI tools if needed (at reduced efficiency)

### Clarifications Needed from DGov:
1. Confirm penetration testing frequency expectations
2. Validate infrastructure development scope
3. Confirm support model preferences (AI-first vs traditional)
4. Clarify any specific compliance timeline requirements
