# Appendix I – Pricing & Assumptions

[← Back to Master PRD](./PRD_Master.md#pricing-options)

All prices in **AUD** inclusive of GST. For comprehensive service-by-service and role-by-role breakdowns, see [Appendix I - Detailed Cost Breakdown (Internal)](./Appendix_I_Detailed_Cost_Breakdown_Internal.md).

---

## Pilot Pricing (12 Months)

**Total Fixed Price: $1,487,880.00**

### Pilot Pricing Breakdown

| Item # | Description | Quantity | Unit Cost (inc GST) | Sub-Total (inc GST) |
| --- | --- | --- | --- | --- |
| 1 | **Platform Infrastructure (including dev tools)** | 12 | $8,990 | $107,880 |
| 2 | **Personnel Costs (6.5 FTE)** | 12 | $109,250 | $1,311,000 |
| 3 | **Security & Compliance** | 1 | $69,000 | $69,000 |
| | | | | |
| | **TOTAL PILOT COST** | | | **$1,487,880.00** |

**Note:** Price includes 15% contingency and 20% margin on all costs. Personnel costs include all setup & integration, infrastructure development, and training & documentation activities. Supports 10,000 users with Key Vault Managed HSM for 12-month pilot.

---

## Option 1 – Infrastructure Plus Overheads (Production)

### Fixed Annual Pricing Structure

| Item # | Description | Small Scale | Medium Scale | Large Scale |
| --- | --- | --- | --- | --- |
| 1 | **Platform Infrastructure (including dev tools)** | $202,060 | $334,484 | $951,108 |
| 2 | **Personnel Costs** | $731,400 | $753,348 | $951,876 |
| 3 | **Security & Compliance** | $138,000 | $207,000 | $276,000 |
| 4 | **Operations & Support** | $82,800 | $138,000 | $248,400 |
| 5 | **Biometric Capability (optional)** | $60,000 | $120,000 | $240,000 |
| | | | | |
| | **ANNUAL CONTRACT VALUE** | **$1,187,391.60** | **$1,424,555.18** | **$2,402,520.29** |

**Note:** Prices include 15% contingency and 20% margin. Annual CPI adjustments of 3% apply from Year 2 onwards.

### Deployment Specifications

| Description | Small Scale | Medium Scale | Large Scale |
| --- | --- | --- | --- |
| **Issued Credentials** | 10,000 | 100,000 | 1,000,000+ |
| **Credential Types** | 1 | 5 | 10+ |
| **Transactions/Year** | 50,000 | 500,000 | 5,000,000 |
| **Customer Partitions (PKI)** | 1 | 2 | 10 |
| **Active Users** | 100,000 | 500,000 | 2,000,000+ |

---

## Option 2 – Consumption Based Pricing (Production)

### Base Platform Fees

| Item # | Description | Unit Cost | Quantity | Small Scale | Medium Scale | Large Scale |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Setup, Implementation and Integration | Once | 1 | Included | Included | Included |
| 2 | Base Platform Fee | Monthly | 12 | $480,000 | $1,080,000 | $1,800,000 |
| 3 | Issued Credential Transaction Costs | Per credential | Variable | $50,000 | $500,000 | $5,000,000 |
| 4 | Customer Partitions (PKI) Managed Keys | Per partition | Variable | $2,000 | $10,000 | $100,000 |
| 5 | Licensing | Annual | 1 | $25,000 | $60,000 | $96,000 |
| 6 | Training and Support | Annual | 1 | $15,000 | $40,000 | $75,000 |
| 7 | Biometric Capability (optional) | Per credential | Variable | - | - | - |
| | **Subtotal** | | | **$572,000** | **$1,690,000** | **$7,071,000** |
| 8 | **Margin (20%)** | Annual | 1 | $114,400 | $338,000 | $1,414,200 |
| | **Estimated Annual Value** | | | **$686,400** | **$2,028,000** | **$8,485,200** |

### Consumption Pricing Details

| Component | Small | Medium | Large | Unit |
| --- | --- | --- | --- | --- |
| **Base Monthly Fee** | $40,000 | $90,000 | $150,000 | per month |
| **Credential Issuance** | $0.50 | $0.35 | $0.30 | per credential |
| **Credential Verification** | $0.08 | $0.06 | $0.04 | per transaction |
| **Active Wallet** | $0.80 | $0.60 | $0.40 | per wallet/month |
| **Revocation/Update** | $0.15 | $0.12 | $0.10 | per event |
| **PKI Management** | $2,000 | $1,500 | $1,000 | per tenant/month |

**Included in Base Fee:**
- First 50,000 verifications/month (Small)
- First 100,000 verifications/month (Medium)  
- First 200,000 verifications/month (Large)

---

## Key Assumptions

### Technical Assumptions
- Azure cloud infrastructure with Perth Extended Zone deployment (mid-2025)
- Multi-tenant architecture with complete tenant isolation
- AI-augmented development providing 40-50% productivity gains
- Auto-scaling capability to handle 3× normal load
- 99.5% - 99.95% availability SLA depending on tier

### Commercial Assumptions
- 3-year minimum contract term
- Annual CPI adjustments of 3%
- Volume discounts apply automatically at thresholds
- No surge pricing during security events or emergency revocations
- Enterprise Agreement discounts available (15-25% for WA Government)
- All prices include GST
- **Disaster Recovery (DR) and Business Continuity Planning (BCP)** are included in Operational Overheads:
  - DR testing conducted quarterly by DevOps team
  - BCP maintenance and updates by operations staff
  - Infrastructure backup costs included in Platform Infrastructure
  - Automated failover and HA included in Azure services configuration

### Service Level Agreements

| Metric | Small | Medium | Large |
| --- | --- | --- | --- |
| **Availability** | 99.5% | 99.9% | 99.95% |
| **Response Time** | <500ms | <200ms | <100ms |
| **Support Hours** | 24/7 Basic | 24/7 Standard | 24/7 Premium |
| **Response SLA** | 4 hours | 2 hours | 1 hour |
| **Resolution SLA** | 24 hours | 8 hours | 4 hours |
| **RPO/RTO** | 24hr/48hr | 4hr/8hr | 15min/1hr |

---

## Additional Professional Services

| Service | Fixed Price | Duration | Description |
| --- | --- | --- | --- |
| **New Tenant Onboarding** | $75,000 | 4 weeks | Complete PKI setup, training, go-live support |
| **New Credential Type** | $35,000 | 2 weeks | Schema design, implementation, testing |
| **Custom Integration** | $25,000 base | 2-4 weeks | API development, testing, documentation |
| **Security Assessment** | $20,000 | 1 week | Penetration testing and vulnerability assessment |
| **Performance Tuning** | $15,000 | 1 week | Optimization and scaling assessment |
| **Training Workshop** | $5,000/day | 1-3 days | Hands-on training with materials |
| **Architecture Review** | $10,000 | 3 days | Solution assessment and recommendations |

---

## Consumption Reporting

### Real-Time Dashboard
- Live usage metrics and trends
- Cost tracking with projections
- Department-level cost allocation
- Transaction volumes by type
- Active wallet statistics
- SLA performance metrics

### Monthly Reports
- Detailed billing breakdown
- Usage analytics and trends
- Performance against SLAs
- Security incident summary
- Capacity planning recommendations
- Cost optimization opportunities

### Billing Structure
```
MONTHLY INVOICE
================
Base Platform Fee:           $XX,XXX
Credential Issuance:         $XX,XXX
Verifications (over free):   $XX,XXX
Active Wallets:              $XX,XXX
Additional Services:         $XX,XXX
--------------------------------
Subtotal:                    $XXX,XXX
Volume Discount:             -$XX,XXX
GST (10%):                   Included
--------------------------------
TOTAL DUE:                   $XXX,XXX
```

---

[Back to Master PRD](./PRD_Master.md) | [Detailed Service Breakdown (Internal)](./Appendix_I_Detailed_Cost_Breakdown_Internal.md)