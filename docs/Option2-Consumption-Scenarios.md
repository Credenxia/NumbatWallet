# Option 2 - Consumption-Based Pricing Scenarios

**Document Version:** 1.0  
**Date:** December 2024  
**Purpose:** Analysis of consumption-based pricing model for digital wallet solution

---

## Pricing Structure

### Base Platform Fee
- **All Scales:** $1,250,476/year (excluding GST)
- **With 10% GST:** $1,375,524/year

### Credential Pricing (Cumulative - Never Resets)
| Scale | Range | Price per Credential |
|-------|-------|---------------------|
| **Small** | 0 - 100,000 | Included in base |
| **Medium** | 100,001 - 1,000,000 | $5.00 |
| **Large** | 1,000,001+ | First 100K free, 100K-1M @ $5.00, Above 1M @ $2.00 |

### Transaction Pricing (Resets Annually)
| Volume/Year | Small Overage | Medium | Large |
|-------------|---------------|---------|-------|
| 0 - 500,000 | Included | $0.30 | $0.10 |
| 500,001 - 5,000,000 | $0.80 | $0.30 | $0.10 |
| Above 5,000,000 | $0.80 | $0.30 | $0.10 |

### Additional Services
- **PKI Partitions:** $50,000 one-time (beyond first included)
- **Credential Types:** No additional charge

---

## Standard Scenarios

### Scenario 1: Small Scale - Within Limits
**50,000 credentials, 250,000 transactions/year**

```
Base Platform: $1,250,476
Credentials: $0 (within 100K included)
Transactions: $0 (within 500K included)
Subtotal: $1,250,476
GST (10%): $125,048
Total: $1,375,524
```
**vs Option 1 Small ($1,375,524):** SAME PRICE ✓

### Scenario 2: Small Scale - High Transactions
**90,000 credentials, 1,000,000 transactions/year**

```
Base Platform: $1,250,476
Credentials: $0 (within 100K included)
Transactions: 500,000 × $0.80 = $400,000
Subtotal: $1,650,476
GST (10%): $165,048
Total: $1,815,524
```
**vs Option 1 Small ($1,375,524):** Client pays $440,000 more

### Scenario 3: Just Entered Medium
**150,000 credentials, 750,000 transactions/year**

```
Base Platform: $1,250,476
Credentials: 50,000 × $5.00 = $250,000
Transactions: 250,000 × $0.30 = $75,000
Subtotal: $1,575,476
GST (10%): $157,548
Total: $1,733,024
```
**vs Option 1 Medium ($1,773,126):** Client saves $40,102 ✓

### Scenario 4: Mid-Medium Scale
**500,000 credentials, 2,500,000 transactions/year**

```
Base Platform: $1,250,476
Credentials: 400,000 × $5.00 = $2,000,000
Transactions: 2,000,000 × $0.30 = $600,000
Subtotal: $3,850,476
GST (10%): $385,048
Total: $4,235,524
```
**vs Option 1 Medium ($1,773,126):** Client pays $2,462,398 more

### Scenario 5: Large Scale
**1,500,000 credentials, 7,500,000 transactions/year**

```
Base Platform: $1,250,476
Credentials:
  - First 100K: $0
  - 100K-1M: 900,000 × $5.00 = $4,500,000
  - Above 1M: 500,000 × $2.00 = $1,000,000
Transactions: 7,500,000 × $0.10 = $750,000
Subtotal: $7,500,476
GST (10%): $750,048
Total: $8,250,524
```
**vs Option 1 Large ($3,115,152):** Client pays $5,135,372 more

---

## Breakeven Analysis Scenarios

### Scenario 6: Medium Scale Breakeven
**Target: Match Option 1 Medium ($1,773,126)**

To reach breakeven at $1,773,126:
- Base: $1,250,476
- Available for consumption: $522,650 (before GST)
- With GST: $475,136

**Calculation:**
- If 200,000 credentials: 100,000 × $5 = $500,000
- Transactions budget: -$24,864 (impossible)

**Actual Breakeven: ~195,000 credentials with minimal transactions**
```
Base Platform: $1,250,476
Credentials: 95,000 × $5.00 = $475,000
Transactions: 0
Subtotal: $1,725,476
GST (10%): $172,548
Total: $1,898,024
```

### Scenario 7: Large Scale Breakeven
**Target: Match Option 1 Large ($3,115,152)**

To reach breakeven at $3,115,152:
- Base: $1,250,476
- Available for consumption: $1,864,676 (before GST)
- With GST: $1,695,160

**Calculation:**
```
Base Platform: $1,250,476
Credentials: 
  - 100K-1M: 900,000 × $5.00 = $4,500,000
  Would exceed budget at 439,000 credentials
```

**Actual: Cannot breakeven - Medium tier pricing too high**

---

## Additional Random Scenarios

### Scenario 8: Government Mandate - Rapid Adoption
**750,000 credentials, 3,750,000 transactions/year**

```
Base Platform: $1,250,476
Credentials: 650,000 × $5.00 = $3,250,000
Transactions: 3,250,000 × $0.30 = $975,000
Subtotal: $5,475,476
GST (10%): $547,548
Total: $6,023,024
```
**vs Option 1 Large ($3,115,152):** Client pays $2,907,872 more

### Scenario 9: Pilot Extension - Slow Growth
**35,000 credentials, 175,000 transactions/year**

```
Base Platform: $1,250,476
Credentials: $0 (within 100K included)
Transactions: $0 (within 500K included)
Subtotal: $1,250,476
GST (10%): $125,048
Total: $1,375,524
```
**vs Option 1 Small ($1,375,524):** SAME PRICE ✓

### Scenario 10: Multi-Agency Adoption
**2,500,000 credentials, 12,500,000 transactions/year**

```
Base Platform: $1,250,476
Credentials:
  - First 100K: $0
  - 100K-1M: 900,000 × $5.00 = $4,500,000
  - Above 1M: 1,500,000 × $2.00 = $3,000,000
Transactions: 12,500,000 × $0.10 = $1,250,000
Subtotal: $10,000,476
GST (10%): $1,000,048
Total: $11,000,524
```
**vs Option 1 Large ($3,115,152):** Client pays $7,885,372 more

---

## Key Findings

### Advantages of Option 2:
1. **Predictable base costs** - Same as Option 1 Small
2. **Pay for growth** - Only charged when exceeding thresholds
3. **Volume discounts** - Lower per-unit costs at Large scale

### Disadvantages:
1. **Expensive at scale** - Significantly more than Option 1 at Medium/Large
2. **No true breakeven** - Cannot match Option 1 Medium/Large prices
3. **Transaction costs add up** - Even small per-transaction fees accumulate

### Recommended Adjustments:
1. **Reduce Medium credential price** to $2-3 range
2. **Reduce transaction prices** by 50-70%
3. **Consider volume bands** within each scale tier
4. **Add consumption caps** to limit maximum charges

---

## Assumptions
- Each credential generates approximately 5 transactions per year
- Transaction volumes reset annually
- Credential counts are cumulative (never reset)
- All prices exclude GST unless specified
- PKI partitions are one-time charges, not recurring

---

[Back to Home](../README.md) | [View Pricing Overview](../NumbatWallet.wiki/Pricing-Assumptions.md)