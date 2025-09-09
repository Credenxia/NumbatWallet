# Appendix I – Detailed Cost Breakdown

[← Back to Pricing Overview](./Appendix_I_Pricing_Assumptions.md) | [← Back to Master PRD](./PRD_Master.md)

**Document Version:** 2.0 FINAL  
**Last Updated:** December 2024

All costs in **AUD** inclusive of GST. This document provides detailed breakdowns for each deployment scenario.

---

## 1. PILOT PHASE (12 Months)

### 1.1 Personnel Costs

| Role | FTE | Base Monthly | Contingency (15%) | Margin (20%) | Total Monthly | 12 Months | Responsibilities |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Project Manager | 1.0 | $15,000 | $2,250 | $3,450 | $20,700 | $248,400 | Stakeholder management, reporting |
| Business Analyst | 1.0 | $8,333 | $1,250 | $1,917 | $11,500 | $138,000 | Requirements, documentation |
| Tester/QA | 1.0 | $8,333 | $1,250 | $1,917 | $11,500 | $138,000 | Testing, quality assurance |
| Solution Architect | 1.0 | $15,000 | $2,250 | $3,450 | $20,700 | $248,400 | Architecture, L3 support |
| Full-Stack Developer | 1.0 | $13,333 | $2,000 | $3,067 | $18,400 | $220,800 | Core development |
| DevOps/Security | 1.0 | $15,000 | $2,250 | $3,450 | $20,700 | $248,400 | Infrastructure, security |
| Technical Support | 0.5 | $4,167 | $625 | $958 | $5,750 | $69,000 | L1/L2 support |
| **TOTAL** | **6.5** | **$79,166** | **$11,875** | **$18,209** | **$109,250** | **$1,311,000** | |

### 1.2 Infrastructure Costs (Monthly)

| Service | Configuration | Base Monthly | Contingency (15%) | Margin (20%) | Total Monthly | 12 Months |
| --- | --- | --- | --- | --- | --- | --- |
| Azure cloud services | Includes AKS, PostgreSQL, Key Vault, etc. | $5,875 | $881 | $1,351 | $8,107 | $97,284 |
| Development Tools - GitHub Copilot | 5 seats @ $32/month | $160 | $24 | $37 | $221 | $2,652 |
| Development Tools - Claude Code | 3 seats @ $160/month | $480 | $72 | $110 | $662 | $7,944 |
| **TOTAL** | | **$6,515** | **$977** | **$1,498** | **$8,990** | **$107,880** |

### 1.3 Other Costs

| Category | Qty | Unit Cost | Base Amount | Contingency (15%) | Margin (20%) | Total Amount | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| **Security & Compliance** | | | | | | | |
| Penetration testing | 1 | $20,000 | $20,000 | $3,000 | $4,600 | $27,600 | Annual assessment |
| Vulnerability scanning | 6 | $1,000 | $6,000 | $900 | $1,380 | $8,280 | Monthly scans (6 months pilot) |
| Compliance audits | 1 | $15,000 | $15,000 | $2,250 | $3,450 | $20,700 | Initial audit |
| DR testing & BCP | 3 | $3,000 | $9,000 | $1,350 | $2,070 | $12,420 | Quarterly tests |
| **TOTAL** | | | **$50,000** | **$7,500** | **$11,500** | **$69,000** | |

**Note:** Setup & Integration, Infrastructure Development, and Training & Documentation are covered by personnel costs (included in the work performed by the project team).

### 1.4 Pilot Total Summary

| Component | Base Cost | Total with Contingency & Margin |
| --- | --- | --- |
| Personnel (12 months) | $950,000 | $1,311,000 |
| Infrastructure (12 months) | $78,180 | $107,880 |
| Security & Compliance | $50,000 | $69,000 |
| **PILOT TOTAL** | **$1,078,180** | **$1,487,880** |

---

## 2. SMALL PRODUCTION (Annual)

### 2.1 Personnel Costs

| Role | FTE | Base Monthly | Contingency (15%) | Margin (20%) | Total Monthly | Annual |
| --- | --- | --- | --- | --- | --- | --- |
| Account Manager | 1.0 | $13,333 | $2,000 | $3,067 | $18,400 | $220,800 |
| Business Analyst | 0.3 | $2,500 | $375 | $575 | $3,450 | $41,400 |
| Solution Architect | 0.5 | $7,500 | $1,125 | $1,725 | $10,350 | $124,200 |
| Full-Stack Developer | 0.5 | $6,667 | $1,000 | $1,533 | $9,200 | $110,400 |
| DevOps/Security | 0.5 | $7,500 | $1,125 | $1,725 | $10,350 | $124,200 |
| Technical Support | 0.8 | $6,667 | $1,000 | $1,533 | $9,200 | $110,400 |
| **TOTAL** | **3.6** | **$44,167** | **$6,625** | **$10,158** | **$60,950** | **$731,400** |

### 2.2 Infrastructure Costs (Monthly)

| Service | Configuration | Base Monthly | Contingency (15%) | Margin (20%) | Total Monthly | Annual |
| --- | --- | --- | --- | --- | --- | --- |
| Azure cloud services | All Azure services | $11,562.44 | $1,734 | $2,659 | $15,955 | $191,464 |
| Development Tools - GitHub Copilot | 5 seats @ $32/month | $160 | $24 | $37 | $221 | $2,652 |
| Development Tools - Claude Code | 3 seats @ $160/month | $480 | $72 | $110 | $662 | $7,944 |
| **TOTAL** | | **$12,202.44** | **$1,830** | **$2,806** | **$16,838** | **$202,060** |

### 2.3 Other Costs

| Category | Qty | Unit Cost | Base Amount | Contingency (15%) | Margin (20%) | Total Amount |
| --- | --- | --- | --- | --- | --- | --- |
| **Security & Compliance** | | | | | | |
| Penetration testing | 2 | $20,000 | $40,000 | $6,000 | $9,200 | $55,200 |
| Vulnerability scanning | 12 | $1,000 | $12,000 | $1,800 | $2,760 | $16,560 |
| Compliance audits | 2 | $15,000 | $30,000 | $4,500 | $6,900 | $41,400 |
| DR testing & BCP | 4 | $3,000 | $12,000 | $1,800 | $2,760 | $16,560 |
| **Subtotal Security** | | | **$94,000** | **$14,100** | **$21,620** | **$129,720** |
| **Operations & Support** | | | | | | |
| 24/7 Support services | 12 | $5,000 | $60,000 | $9,000 | $13,800 | $82,800 |
| Monitoring & alerting | 12 | $2,500 | $30,000 | $4,500 | $6,900 | $41,400 |
| **Subtotal Operations** | | | **$90,000** | **$13,500** | **$20,700** | **$124,200** |
| **TOTAL** | | | **$184,000** | **$27,600** | **$42,320** | **$253,920** |

### 2.4 Small Production Total Summary

| Component | Base Cost | Total with Contingency & Margin |
| --- | --- | --- |
| Personnel (Annual) | $530,000.00 | $731,400.00 |
| Infrastructure (Annual) | $146,429.28 | $202,071.60 |
| Security & Compliance | $94,000.00 | $129,720.00 |
| Operations & Support | $90,000.00 | $124,200.00 |
| **SMALL PRODUCTION TOTAL** | **$860,429.28** | **$1,187,391.60** |

---

## 3. MEDIUM PRODUCTION (Annual)

### 3.1 Personnel Costs

**Note:** Includes 3% CPI annual increase from Year 2 onwards.

| Role | FTE | Base Monthly | Contingency (15%) | Margin (20%) | Total Monthly | Annual |
| --- | --- | --- | --- | --- | --- | --- |
| Account Manager | 1.0 | $13,733 | $2,060 | $3,159 | $18,952 | $227,424 |
| Business Analyst | 0.3 | $2,575 | $386 | $592 | $3,553 | $42,636 |
| Solution Architect | 0.5 | $7,725 | $1,159 | $1,777 | $10,661 | $127,932 |
| Full-Stack Developer | 0.5 | $6,867 | $1,030 | $1,579 | $9,476 | $113,712 |
| DevOps/Security | 0.5 | $7,725 | $1,159 | $1,777 | $10,661 | $127,932 |
| Technical Support | 0.8 | $6,867 | $1,030 | $1,579 | $9,476 | $113,712 |
| **TOTAL** | **3.6** | **$45,492** | **$6,824** | **$10,463** | **$62,779** | **$753,348** |

### 3.2 Infrastructure Costs (Monthly)

**Note:** Includes 3% CPI annual increase from Year 2 onwards.

| Service | Configuration | Base Monthly | Contingency (15%) | Margin (20%) | Total Monthly | Annual |
| --- | --- | --- | --- | --- | --- | --- |
| Azure cloud services | All Azure services | $19,558.53 | $2,934 | $4,498 | $26,991 | $323,888 |
| Development Tools - GitHub Copilot | 5 seats @ $32/month | $160 | $24 | $37 | $221 | $2,652 |
| Development Tools - Claude Code | 3 seats @ $160/month | $480 | $72 | $110 | $662 | $7,944 |
| **TOTAL** | | **$20,198.53** | **$3,030** | **$4,645** | **$27,874** | **$334,484** |

### 3.3 Other Costs  

| Category | Qty | Unit Cost | Base Amount | Contingency (15%) | Margin (20%) | Total Amount |
| --- | --- | --- | --- | --- | --- | --- |
| **Security & Compliance** | | | | | | |
| Penetration testing | 2 | $20,000 | $40,000 | $6,000 | $9,200 | $55,200 |
| Vulnerability scanning | 12 | $1,500 | $18,000 | $2,700 | $4,140 | $24,840 |
| Compliance audits | 2 | $15,000 | $30,000 | $4,500 | $6,900 | $41,400 |
| DR testing & BCP | 12 | $3,000 | $36,000 | $5,400 | $8,280 | $49,680 |
| **Subtotal Security** | | | **$124,000** | **$18,600** | **$28,520** | **$171,120** |
| **Operations & Support** | | | | | | |
| 24/7 Premium support | 12 | $7,000 | $84,000 | $12,600 | $19,320 | $115,920 |
| NOC monitoring | 12 | $3,000 | $36,000 | $5,400 | $8,280 | $49,680 |
| **Subtotal Operations** | | | **$120,000** | **$18,000** | **$27,600** | **$165,600** |
| **TOTAL** | | | **$244,000** | **$36,600** | **$56,120** | **$336,720** |

### 3.4 Medium Production Total Summary

| Component | Base Cost | Total with Contingency & Margin |
| --- | --- | --- |
| Personnel (Annual) | $545,904.00 | $753,347.52 |
| Infrastructure (Annual) | $242,382.36 | $334,487.66 |
| Security & Compliance | $124,000.00 | $171,120.00 |
| Operations & Support | $120,000.00 | $165,600.00 |
| **MEDIUM PRODUCTION TOTAL** | **$1,032,286.36** | **$1,424,555.18** |

---

## 4. LARGE PRODUCTION (Annual)

### 4.1 Personnel Costs

**Note:** Includes 3% CPI annual increase from Year 3 onwards.

| Role | FTE | Base Monthly | Contingency (15%) | Margin (20%) | Total Monthly | Annual |
| --- | --- | --- | --- | --- | --- | --- |
| Account Manager | 1.0 | $14,145 | $2,122 | $3,253 | $19,520 | $234,240 |
| Business Analyst | 0.5 | $4,425 | $664 | $1,018 | $6,107 | $73,284 |
| Solution Architect | 0.5 | $7,957 | $1,194 | $1,830 | $10,981 | $131,772 |
| Senior Developer | 1.0 | $14,145 | $2,122 | $3,253 | $19,520 | $234,240 |
| DevOps/Security | 0.5 | $7,957 | $1,194 | $1,830 | $10,981 | $131,772 |
| Technical Support | 1.0 | $8,850 | $1,328 | $2,036 | $12,214 | $146,568 |
| **TOTAL** | **4.5** | **$57,479** | **$8,624** | **$13,220** | **$79,323** | **$951,876** |

### 4.2 Infrastructure Costs (Monthly)

**Note:** Includes 3% CPI annual increase from Year 3 onwards.

| Service | Configuration | Base Monthly | Contingency (15%) | Margin (20%) | Total Monthly | Annual |
| --- | --- | --- | --- | --- | --- | --- |
| Azure cloud services | All Azure services | $56,794.09 | $8,519 | $13,063 | $78,376 | $940,512 |
| Development Tools - GitHub Copilot | 5 seats @ $32/month | $160 | $24 | $37 | $221 | $2,652 |
| Development Tools - Claude Code | 3 seats @ $160/month | $480 | $72 | $110 | $662 | $7,944 |
| **TOTAL** | | **$57,434.09** | **$8,615** | **$13,210** | **$79,259** | **$951,108** |

### 4.3 Other Costs

| Category | Qty | Unit Cost | Base Amount | Contingency (15%) | Margin (20%) | Total Amount |
| --- | --- | --- | --- | --- | --- | --- |
| **Security & Compliance** | | | | | | |
| Penetration testing | 2 | $20,000 | $40,000 | $6,000 | $9,200 | $55,200 |
| Vulnerability scanning (continuous) | 12 | $2,000 | $24,000 | $3,600 | $5,520 | $33,120 |
| Compliance audits | 2 | $15,000 | $30,000 | $4,500 | $6,900 | $41,400 |
| DR testing & BCP (weekly) | 52 | $1,000 | $52,000 | $7,800 | $11,960 | $71,760 |
| **Subtotal Security** | | | **$146,000** | **$21,900** | **$33,580** | **$201,480** |
| **Operations (NOC/SOC)** | | | | | | |
| 24/7 SOC services | 12 | $12,000 | $144,000 | $21,600 | $33,120 | $198,720 |
| NOC operations | 12 | $6,000 | $72,000 | $10,800 | $16,560 | $99,360 |
| **Subtotal Operations** | | | **$216,000** | **$32,400** | **$49,680** | **$298,080** |
| **TOTAL** | | | **$362,000** | **$54,300** | **$83,260** | **$499,560** |

### 4.4 Large Production Total Summary

| Component | Base Cost | Total with Contingency & Margin |
| --- | --- | --- |
| Personnel (Annual) | $689,748.00 | $951,851.76 |
| Infrastructure (Annual) | $689,209.08 | $951,108.53 |
| Security & Compliance | $146,000.00 | $201,480.00 |
| Operations & Support | $216,000.00 | $298,080.00 |
| **LARGE PRODUCTION TOTAL** | **$1,740,957.08** | **$2,402,520.29** |

---

[Back to Pricing Overview](./Appendix_I_Pricing_Assumptions.md) | [Back to Master PRD](./PRD_Master.md)