# Appendix: Pricing and Assumptions
## Digital Wallet and Verifiable Credentials Solution

**Document Version:** 1.0  
**Parent Document:** [Master PRD](./PRD_Master.md)  
**Last Updated:** December 2024

---

## 1. Pricing Models Overview

### 1.1 Pricing Strategy Framework

```mermaid
graph TB
    subgraph "Pricing Models"
        CONSUMPTION[Consumption-Based<br/>Pay per use]
        FIXED[Fixed-Fee<br/>Predictable cost]
        HYBRID[Hybrid Model<br/>Base + Usage]
        TIERED[Tiered Pricing<br/>Volume discounts]
    end
    
    subgraph "Cost Components"
        INFRA[Infrastructure]
        PERSONNEL[Personnel]
        LICENSES[Licenses]
        SUPPORT[Support]
        MARGIN[Margin]
    end
    
    subgraph "Pricing Factors"
        USERS[Active Users]
        TRANSACTIONS[Transactions]
        STORAGE[Storage]
        COMPUTE[Compute]
        BANDWIDTH[Bandwidth]
    end
    
    subgraph "Commercial Terms"
        SLA[SLA Levels]
        PAYMENT[Payment Terms]
        ESCALATION[Escalation Clauses]
        PENALTIES[Penalties]
    end
    
    CONSUMPTION --> USERS
    FIXED --> PERSONNEL
    HYBRID --> INFRA
    TIERED --> TRANSACTIONS
    
    USERS --> SLA
    TRANSACTIONS --> PAYMENT
    STORAGE --> ESCALATION
    COMPUTE --> PENALTIES
```

### 1.2 Pilot Pricing Comparison

```mermaid
graph LR
    subgraph "Option 1: Consumption"
        BASE_FEE[Base: $15K/month]
        USAGE_FEE[Usage: Variable]
        OVERAGE[Overage: Premium rates]
        TOTAL_C[Total: $25-35K/month]
    end
    
    subgraph "Option 2: Fixed"
        POA_FEE[POA: $150K]
        SETUP_FEE[Setup: $200K]
        PILOT_FEE[Pilot: $480K]
        SUPPORT_FEE[Support: $120K]
        TOTAL_F[Total: $1.1M fixed]
    end
    
    BASE_FEE --> USAGE_FEE
    USAGE_FEE --> OVERAGE
    OVERAGE --> TOTAL_C
    
    POA_FEE --> SETUP_FEE
    SETUP_FEE --> PILOT_FEE
    PILOT_FEE --> SUPPORT_FEE
    SUPPORT_FEE --> TOTAL_F
```

---

## 2. Option 1: Consumption-Based Pricing

### 2.1 Detailed Pricing Structure

| Component | Unit Price (AUD) | Included Volume | Overage Rate | Notes |
|-----------|-----------------|-----------------|--------------|-------|
| **Base Platform Fee** | $15,000/month | - | - | Includes basic infrastructure |
| **Active Wallets** | $0.50/wallet/month | 10,000 | $0.75 | Unique active users |
| **Credential Issuance** | $0.10/credential | 50,000/month | $0.15 | All credential types |
| **Verifications** | $0.01/verification | 500,000/month | $0.02 | Online and offline |
| **API Calls** | $1.00/million | 10M/month | $1.50/million | All API endpoints |
| **Storage** | $5/GB/month | 100 GB | $8/GB | Encrypted storage |
| **Bandwidth** | $0.10/GB | 1 TB/month | $0.15/GB | Data transfer |
| **Support** | $5,000/month | Business hours | 24x7: +$10K | L2 support included |

### 2.2 Consumption Pricing Calculator

```mermaid
graph TD
    subgraph "Monthly Usage Example"
        WALLETS_C[8,000 Wallets<br/>8,000 × $0.50 = $4,000]
        CREDS_C[40,000 Credentials<br/>40,000 × $0.10 = $4,000]
        VERIFY_C[400,000 Verifications<br/>400,000 × $0.01 = $4,000]
        API_C[8M API Calls<br/>8 × $1.00 = $8]
        STORAGE_C[80 GB Storage<br/>80 × $5 = $400]
    end
    
    subgraph "Monthly Total"
        BASE_C[Base Fee: $15,000]
        USAGE_C[Usage Fees: $12,408]
        SUPPORT_C[Support: $5,000]
        TOTAL_MONTH[Total: $32,408/month]
    end
    
    WALLETS_C --> USAGE_C
    CREDS_C --> USAGE_C
    VERIFY_C --> USAGE_C
    API_C --> USAGE_C
    STORAGE_C --> USAGE_C
    
    BASE_C --> TOTAL_MONTH
    USAGE_C --> TOTAL_MONTH
    SUPPORT_C --> TOTAL_MONTH
```

### 2.3 Tiered Volume Discounts

| Volume Tier | Wallets | Discount | Effective Rate |
|-------------|---------|----------|----------------|
| **Tier 1** | 0-10,000 | 0% | $0.50/wallet |
| **Tier 2** | 10,001-25,000 | 10% | $0.45/wallet |
| **Tier 3** | 25,001-50,000 | 20% | $0.40/wallet |
| **Tier 4** | 50,001-100,000 | 30% | $0.35/wallet |
| **Tier 5** | 100,001+ | 40% | $0.30/wallet |

---

## 3. Option 2: Fixed-Fee Pricing

### 3.1 Fixed Price Breakdown

```mermaid
pie title Fixed Price Distribution ($1.1M)
    "POA Development" : 150
    "Environment Setup" : 200
    "Pilot Operations" : 480
    "Training & Documentation" : 50
    "L2 Support" : 120
    "Change Requests" : 100
```

### 3.2 Detailed Fixed Pricing Components

| Phase | Deliverables | Cost (AUD) | Timeline | Payment |
|-------|--------------|------------|----------|---------|
| **POA Development** | Working demo, SDK integration | $150,000 | 3 weeks | On completion |
| **Environment Setup** | Production environment, security | $200,000 | 2 months | Monthly |
| **Pilot Operations** | 12-month pilot, maintenance | $480,000 | 12 months | Monthly |
| **Training** | User training, documentation | $50,000 | 2 months | On delivery |
| **L2 Support** | Business hours support | $120,000 | 12 months | Quarterly |
| **Change Budget** | Minor changes, enhancements | $100,000 | As needed | On approval |
| **Total** | Complete pilot solution | **$1,100,000** | 15 months | Staged |

### 3.3 Payment Schedule

```mermaid
gantt
    title Payment Schedule - Fixed Fee Option
    dateFormat YYYY-MM-DD
    
    section Initial
    Contract Signing (20%)     :milestone, 2025-01-15, 0d
    
    section POA
    POA Completion (20%)       :milestone, 2025-02-24, 0d
    
    section Monthly Payments
    Month 1 (5%)              :2025-03-01, 30d
    Month 2 (5%)              :2025-04-01, 30d
    Month 3 (5%)              :2025-05-01, 30d
    Month 4 (5%)              :2025-06-01, 30d
    Month 5 (5%)              :2025-07-01, 30d
    Month 6 (5%)              :2025-08-01, 30d
    Month 7 (5%)              :2025-09-01, 30d
    Month 8 (5%)              :2025-10-01, 30d
    Month 9 (5%)              :2025-11-01, 30d
    Month 10 (5%)             :2025-12-01, 30d
    Month 11 (5%)             :2026-01-01, 30d
    Month 12 (5%)             :2026-02-01, 30d
```

---

## 4. Production Pricing Model

### 4.1 Production Pricing Structure

```mermaid
graph TD
    subgraph "Base Components"
        PLATFORM[Platform License<br/>$50K/month]
        INFRASTRUCTURE[Infrastructure<br/>$30K/month]
        OPERATIONS[Operations<br/>$25K/month]
    end
    
    subgraph "Usage Components"
        USERS_P[Per User<br/>$2/user/year]
        CREDENTIALS_P[Per Credential<br/>$0.05/credential]
        VERIFICATIONS_P[Per Verification<br/>$0.005/verification]
    end
    
    subgraph "Optional Services"
        PREMIUM_SUPPORT[24x7 Support<br/>$20K/month]
        CUSTOM_DEV[Custom Development<br/>$2K/day]
        TRAINING_P[Training Services<br/>$5K/session]
    end
    
    subgraph "Enterprise Discounts"
        VOLUME_DISC[Volume: 10-30%]
        COMMITMENT[3-year: 20%]
        BUNDLE[Bundle: 15%]
    end
    
    PLATFORM --> BASE_TOTAL[Base: $105K/month]
    INFRASTRUCTURE --> BASE_TOTAL
    OPERATIONS --> BASE_TOTAL
    
    USERS_P --> USAGE_TOTAL[Usage: Variable]
    CREDENTIALS_P --> USAGE_TOTAL
    VERIFICATIONS_P --> USAGE_TOTAL
    
    BASE_TOTAL --> ENTERPRISE_PRICE[Enterprise Price]
    USAGE_TOTAL --> ENTERPRISE_PRICE
    VOLUME_DISC --> ENTERPRISE_PRICE
```

### 4.2 Production Scaling Scenarios

| Scale | Users | Monthly Cost | Annual Cost | Per User |
|-------|-------|--------------|-------------|----------|
| **Small** | 100,000 | $120,000 | $1,440,000 | $14.40 |
| **Medium** | 500,000 | $200,000 | $2,400,000 | $4.80 |
| **Large** | 1,000,000 | $350,000 | $4,200,000 | $4.20 |
| **Enterprise** | 2,000,000+ | $600,000 | $7,200,000 | $3.60 |

---

## 5. Cost Structure Analysis

### 5.1 Cost Breakdown

```mermaid
graph TB
    subgraph "Direct Costs (65%)"
        INFRA_COST[Infrastructure: 20%]
        PERSONNEL_COST[Personnel: 35%]
        LICENSES_COST[Licenses: 10%]
    end
    
    subgraph "Indirect Costs (25%)"
        OVERHEAD[Overhead: 10%]
        SUPPORT_COST[Support: 10%]
        COMPLIANCE_COST[Compliance: 5%]
    end
    
    subgraph "Margin (10%)"
        GROSS_MARGIN[Gross Margin: 30%]
        OPERATING_MARGIN[Operating Margin: 15%]
        NET_MARGIN[Net Margin: 10%]
    end
    
    INFRA_COST --> DIRECT_TOTAL[Direct: 65%]
    PERSONNEL_COST --> DIRECT_TOTAL
    LICENSES_COST --> DIRECT_TOTAL
    
    OVERHEAD --> INDIRECT_TOTAL[Indirect: 25%]
    SUPPORT_COST --> INDIRECT_TOTAL
    COMPLIANCE_COST --> INDIRECT_TOTAL
    
    DIRECT_TOTAL --> TOTAL_COST[Total Cost]
    INDIRECT_TOTAL --> TOTAL_COST
    TOTAL_COST --> NET_MARGIN
```

### 5.2 Infrastructure Costs

| Component | Monthly Cost | Annual Cost | Notes |
|-----------|--------------|-------------|-------|
| **Azure Kubernetes Service** | $8,000 | $96,000 | 3 clusters |
| **PostgreSQL Flexible Server** | $6,000 | $72,000 | HA configuration |
| **Storage (Blob + Disk)** | $3,000 | $36,000 | 10TB total |
| **Networking (VNet, LB, WAF)** | $2,500 | $30,000 | Multi-region |
| **Key Vault / HSM** | $4,000 | $48,000 | Dedicated HSM |
| **Monitoring & Logging** | $2,000 | $24,000 | Full observability |
| **Backup & DR** | $2,500 | $30,000 | Geo-redundant |
| **Total Infrastructure** | **$28,000** | **$336,000** | |

---

## 6. Key Assumptions

### 6.1 Technical Assumptions

```mermaid
graph LR
    subgraph "Architecture"
        MULTI_TENANT[Multi-tenant SaaS]
        CLOUD_NATIVE[Cloud-native design]
        MICROSERVICES[Microservices]
        API_FIRST[API-first approach]
    end
    
    subgraph "Technology"
        DOTNET[.NET 8 LTS]
        POSTGRES[PostgreSQL 15+]
        AZURE[Azure AU regions]
        KUBERNETES[Kubernetes orchestration]
    end
    
    subgraph "Standards"
        W3C[W3C VC/DID]
        OAUTH[OAuth 2.1]
        OIDC[OpenID Connect]
        ISO[ISO standards]
    end
    
    subgraph "Integration"
        SERVICEWA[ServiceWA SDK]
        TRUST_REG[Trust Registry]
        GOV_SYSTEMS[Gov systems]
        THIRD_PARTY[3rd party issuers]
    end
```

### 6.2 Business Assumptions

| Category | Assumption | Impact | Validation |
|----------|------------|--------|------------|
| **User Adoption** | 80% adoption within target group | High | Pilot metrics |
| **Transaction Volume** | 10 verifications per user/month | Medium | Usage analytics |
| **Support Load** | <5% users need support | Medium | Support tickets |
| **Performance** | Linear scaling with load | High | Load testing |
| **Availability** | 99.9% achievable | High | Monitoring |
| **Security** | No major breaches | Critical | Security audits |

### 6.3 Commercial Assumptions

- **Contract Duration:** Minimum 12 months for pilot
- **Payment Terms:** Net 30 days
- **Currency:** All prices in AUD
- **Taxes:** GST exclusive
- **Escalation:** Annual CPI adjustment
- **Termination:** 90 days notice
- **Liability:** Limited to contract value
- **IP Rights:** Client owns data, we retain platform IP

---

## 7. Risk-Adjusted Pricing

### 7.1 Risk Factors

```mermaid
graph TD
    subgraph "Technical Risks"
        SCALE_RISK[Scaling beyond 10K users: +10%]
        INTEGRATION_RISK[Complex integrations: +15%]
        SECURITY_RISK[Enhanced security: +10%]
    end
    
    subgraph "Commercial Risks"
        SCOPE_RISK[Scope creep: +20%]
        TIMELINE_RISK[Compressed timeline: +15%]
        SUPPORT_RISK[24x7 support: +25%]
    end
    
    subgraph "Operational Risks"
        COMPLIANCE_RISK[Compliance changes: +10%]
        VENDOR_RISK[Vendor dependencies: +5%]
        RESOURCE_RISK[Resource availability: +10%]
    end
    
    subgraph "Risk Premium"
        LOW_RISK[Low Risk: +0-10%]
        MEDIUM_RISK[Medium Risk: +10-25%]
        HIGH_RISK[High Risk: +25-50%]
    end
    
    SCALE_RISK --> MEDIUM_RISK
    INTEGRATION_RISK --> MEDIUM_RISK
    SECURITY_RISK --> LOW_RISK
    
    SCOPE_RISK --> HIGH_RISK
    TIMELINE_RISK --> MEDIUM_RISK
    SUPPORT_RISK --> HIGH_RISK
    
    COMPLIANCE_RISK --> LOW_RISK
    VENDOR_RISK --> LOW_RISK
    RESOURCE_RISK --> MEDIUM_RISK
```

### 7.2 Contingency Planning

| Risk Category | Probability | Impact | Contingency | Cost Impact |
|--------------|-------------|--------|-------------|-------------|
| **Technical Complexity** | Medium | High | Additional resources | +15% |
| **Integration Delays** | Medium | Medium | Buffer time | +10% |
| **Scope Changes** | High | Medium | Change control | +20% |
| **Performance Issues** | Low | High | Infrastructure scaling | +10% |
| **Security Requirements** | Low | High | Security experts | +10% |
| **Total Contingency** | - | - | - | **+15% weighted** |

---

## 8. Value Proposition

### 8.1 ROI Analysis

```mermaid
graph LR
    subgraph "Costs"
        PILOT_COST[Pilot: $1.1M]
        PROD_COST[Production: $2.4M/year]
        MIGRATION[Migration: $200K]
        TRAINING_COST[Training: $100K]
    end
    
    subgraph "Benefits"
        EFFICIENCY[Efficiency: $3M/year]
        FRAUD_REDUCTION[Fraud Reduction: $1M/year]
        COMPLIANCE_BENEFIT[Compliance: $500K/year]
        USER_SATISFACTION[User Satisfaction: $2M value]
    end
    
    subgraph "ROI Metrics"
        PAYBACK[Payback: 8 months]
        NPV[NPV: $8.5M over 3 years]
        IRR[IRR: 125%]
        TOTAL_ROI[ROI: 287% over 3 years]
    end
    
    PILOT_COST --> TOTAL_INVESTMENT[Total: $3.8M Year 1]
    PROD_COST --> TOTAL_INVESTMENT
    MIGRATION --> TOTAL_INVESTMENT
    TRAINING_COST --> TOTAL_INVESTMENT
    
    EFFICIENCY --> TOTAL_BENEFIT[Total: $6.5M/year]
    FRAUD_REDUCTION --> TOTAL_BENEFIT
    COMPLIANCE_BENEFIT --> TOTAL_BENEFIT
    USER_SATISFACTION --> TOTAL_BENEFIT
    
    TOTAL_INVESTMENT --> TOTAL_ROI
    TOTAL_BENEFIT --> TOTAL_ROI
```

### 8.2 Cost Comparison

| Solution | Year 1 | Year 2 | Year 3 | 3-Year Total | Per User (2M) |
|----------|--------|--------|--------|--------------|---------------|
| **Our Solution** | $3.8M | $2.4M | $2.4M | $8.6M | $4.30 |
| **Competitor A** | $5.0M | $3.0M | $3.0M | $11.0M | $5.50 |
| **Competitor B** | $4.5M | $3.5M | $3.5M | $11.5M | $5.75 |
| **In-House Build** | $8.0M | $2.0M | $2.0M | $12.0M | $6.00 |

---

## 9. Service Level Agreements

### 9.1 SLA Tiers

```mermaid
graph TD
    subgraph "Standard SLA"
        STD_AVAIL[Availability: 99.5%]
        STD_RESPONSE[Response: <2s]
        STD_SUPPORT[Support: Business hours]
        STD_CREDIT[Credits: 10% max]
    end
    
    subgraph "Premium SLA"
        PREM_AVAIL[Availability: 99.9%]
        PREM_RESPONSE[Response: <500ms]
        PREM_SUPPORT[Support: 24x7]
        PREM_CREDIT[Credits: 25% max]
    end
    
    subgraph "Enterprise SLA"
        ENT_AVAIL[Availability: 99.95%]
        ENT_RESPONSE[Response: <200ms]
        ENT_SUPPORT[Support: Dedicated team]
        ENT_CREDIT[Credits: 50% max]
    end
    
    STD_AVAIL --> STD_PRICE[+$0/month]
    PREM_AVAIL --> PREM_PRICE[+$10K/month]
    ENT_AVAIL --> ENT_PRICE[+$25K/month]
```

### 9.2 SLA Credits

| Availability | Credit | Calculation | Cap |
|--------------|--------|-------------|-----|
| **99.5% - 99.9%** | 10% | Monthly fee × 10% | 10% |
| **99.0% - 99.5%** | 15% | Monthly fee × 15% | 25% |
| **98.0% - 99.0%** | 25% | Monthly fee × 25% | 50% |
| **< 98.0%** | 50% | Monthly fee × 50% | 100% |

---

## 10. Consumption Reporting

### 10.1 Reporting Framework

```mermaid
graph TB
    subgraph "Data Collection"
        API_METRICS[API Metrics]
        USER_METRICS[User Metrics]
        TRANS_METRICS[Transaction Metrics]
        RESOURCE_METRICS[Resource Metrics]
    end
    
    subgraph "Processing"
        AGGREGATION[Aggregation]
        CALCULATION[Cost Calculation]
        TRENDING[Trend Analysis]
        FORECASTING[Forecasting]
    end
    
    subgraph "Reporting"
        DAILY_REPORT[Daily Dashboard]
        WEEKLY_REPORT[Weekly Summary]
        MONTHLY_INVOICE[Monthly Invoice]
        QUARTERLY_REVIEW[Quarterly Review]
    end
    
    subgraph "Delivery"
        PORTAL[Customer Portal]
        EMAIL_REPORT[Email Reports]
        API_EXPORT[API Export]
        DASHBOARD[Live Dashboard]
    end
    
    API_METRICS --> AGGREGATION
    USER_METRICS --> AGGREGATION
    TRANS_METRICS --> AGGREGATION
    RESOURCE_METRICS --> AGGREGATION
    
    AGGREGATION --> CALCULATION
    CALCULATION --> TRENDING
    TRENDING --> FORECASTING
    
    FORECASTING --> DAILY_REPORT
    FORECASTING --> WEEKLY_REPORT
    FORECASTING --> MONTHLY_INVOICE
    FORECASTING --> QUARTERLY_REVIEW
    
    DAILY_REPORT --> DASHBOARD
    WEEKLY_REPORT --> EMAIL_REPORT
    MONTHLY_INVOICE --> PORTAL
    QUARTERLY_REVIEW --> API_EXPORT
```

### 10.2 Consumption Metrics

| Metric | Frequency | Format | Distribution | Retention |
|--------|-----------|--------|--------------|-----------|
| **Real-time Usage** | Continuous | Dashboard | Portal | 30 days |
| **Daily Summary** | Daily | JSON/CSV | API/Email | 90 days |
| **Weekly Report** | Weekly | PDF | Email | 1 year |
| **Monthly Invoice** | Monthly | PDF | Portal/Email | 7 years |
| **Quarterly Analysis** | Quarterly | Excel | Email | 3 years |

---

## 11. Contract Terms

### 11.1 Key Commercial Terms

| Term | Pilot | Production | Notes |
|------|-------|------------|-------|
| **Minimum Term** | 12 months | 36 months | Auto-renewal |
| **Payment Terms** | Net 30 | Net 30 | Monthly invoicing |
| **Termination Notice** | 30 days | 90 days | Written notice |
| **Price Protection** | Fixed | CPI + 3% max | Annual adjustment |
| **Volume Commitment** | None | 80% of forecast | Quarterly true-up |
| **Liability Cap** | Contract value | Annual fees | Excludes data breach |
| **Warranty** | 90 days | 90 days | Defect remediation |
| **Indemnification** | Mutual | Mutual | IP and data |

### 11.2 Penalties and Incentives

```mermaid
graph TD
    subgraph "Penalties"
        LATE_DELIVERY[Late Delivery: 5%/week]
        SLA_BREACH[SLA Breach: Credits]
        SECURITY_INCIDENT[Security: 10% annual]
        DATA_BREACH[Data Breach: Uncapped]
    end
    
    subgraph "Incentives"
        EARLY_DELIVERY[Early: 5% bonus]
        EXCEED_SLA[Exceed SLA: 2% bonus]
        USER_SATISFACTION[High NPS: 3% bonus]
        INNOVATION[Innovation: 5% bonus]
    end
    
    subgraph "Adjustments"
        SCOPE_CHANGE[Scope: Renegotiate]
        VOLUME_CHANGE[Volume: Tier adjustment]
        EMERGENCY[Emergency: Time & materials]
        REGULATORY[Regulatory: Pass through]
    end
    
    LATE_DELIVERY --> CONTRACT_ADJUSTMENT
    SLA_BREACH --> CONTRACT_ADJUSTMENT
    EARLY_DELIVERY --> CONTRACT_ADJUSTMENT
    EXCEED_SLA --> CONTRACT_ADJUSTMENT
    SCOPE_CHANGE --> CONTRACT_ADJUSTMENT
    VOLUME_CHANGE --> CONTRACT_ADJUSTMENT
```

---

## 12. Financial Projections

### 12.1 3-Year Financial Model

```mermaid
graph LR
    subgraph "Year 1"
        Y1_REVENUE[Revenue: $1.1M]
        Y1_COST[Cost: $950K]
        Y1_MARGIN[Margin: $150K]
        Y1_PERCENT[Margin %: 13.6%]
    end
    
    subgraph "Year 2"
        Y2_REVENUE[Revenue: $2.4M]
        Y2_COST[Cost: $1.8M]
        Y2_MARGIN[Margin: $600K]
        Y2_PERCENT[Margin %: 25%]
    end
    
    subgraph "Year 3"
        Y3_REVENUE[Revenue: $4.2M]
        Y3_COST[Cost: $2.9M]
        Y3_MARGIN[Margin: $1.3M]
        Y3_PERCENT[Margin %: 31%]
    end
    
    Y1_REVENUE --> Y1_MARGIN
    Y1_COST --> Y1_MARGIN
    Y1_MARGIN --> Y1_PERCENT
    
    Y2_REVENUE --> Y2_MARGIN
    Y2_COST --> Y2_MARGIN
    Y2_MARGIN --> Y2_PERCENT
    
    Y3_REVENUE --> Y3_MARGIN
    Y3_COST --> Y3_MARGIN
    Y3_MARGIN --> Y3_PERCENT
```

### 12.2 Break-Even Analysis

| Metric | Value | Timeline | Assumptions |
|--------|-------|----------|-------------|
| **Break-even Volume** | 5,000 users | Month 6 | At current pricing |
| **Break-even Revenue** | $65K/month | Month 6 | Covers all costs |
| **Payback Period** | 8 months | From go-live | Including setup costs |
| **Cash Flow Positive** | Month 9 | From contract | With payment terms |
| **Target Margin** | 25% | Year 2 | Operational efficiency |

---

## Pricing Approval Matrix

### Decision Authority

| Amount | Discount | Approval Level | Documentation |
|--------|----------|----------------|---------------|
| **< $100K** | < 10% | Sales Manager | Quote |
| **$100K - $500K** | 10-20% | Sales Director | Business case |
| **$500K - $1M** | 20-30% | VP Sales | Executive review |
| **> $1M** | > 30% | CEO | Board approval |

### Price Exceptions

- **Strategic Accounts:** Up to 40% discount with CEO approval
- **Volume Commitments:** Additional 10% for 3-year contracts
- **Reference Customers:** Special pricing with marketing commitment
- **Government:** Standard government rates apply

---

**END OF PRICING AND ASSUMPTIONS APPENDIX**