# Option 3 - Consumption-Based Pricing Scenarios (10K Tier Structure)

**Document Version:** 1.0  
**Date:** December 2024  
**Purpose:** Analysis of consumption-based pricing model with 10K tier structure for digital wallet solution

---

## Pricing Structure

### Scale Tiers (Based on Credentials Issued)
- **Small:** 0 - 10,000 credentials
- **Medium:** 10,001 - 100,000 credentials  
- **Large:** 100,001+ credentials

### Base Platform Fee
- **All Scales:** $1,375,524/year (GST-inclusive)

### Credential Pricing (Cumulative - Never Resets)
| Tier | Range | Price per Credential |
|------|-------|---------------------|
| **First 10K** | 0 - 10,000 | FREE (included) |
| **Medium Tier** | 10,001 - 100,000 | $10.00 |
| **Large Tier** | 100,001+ | $2.00 |

**Pricing Logic:**
- Small scale: All 10,000 credentials free
- Medium scale: First 10K free, then charge $10 per credential for 10,001+
- Large scale: First 10K free, next 90K at $10, then $2 per credential for 100,001+

### Transaction Pricing (Resets Annually)
| Scale | Included Transactions | Overage Price |
|-------|----------------------|---------------|
| **Small** | 50,000 | $0.25 per transaction |
| **Medium** | 500,000 | $0.15 per transaction |
| **Large** | 1,000,000 | $0.10 per transaction |

### Additional Services
- **PKI Partitions:** $50,000 one-time (beyond first included)
- **Credential Types:** No additional charge

### Option 1 Comparison Prices (GST-inclusive)
- **Small Scale:** $1,375,524
- **Medium Scale:** $1,773,126  
- **Large Scale:** $3,115,152

**Note:** Option 1 Large scale pricing requires review if credentials exceed 1M or transactions exceed 5M annually.

---

## Standard Scenarios

### Scenario 1: Small Scale - Edge of Small (10,000 credentials)
**10,000 credentials, 50,000 transactions/year**

```
Option 3 Calculation:
Base Platform: $1,375,524
Credentials: $0 (all 10K free)
Transactions: $0 (within 50K included)
Total (inc GST): $1,375,524

Option 1 Comparison: Small Scale
Option 1 Total: $1,375,524
```
**Result:** SAME PRICE (Breakeven) ✓

### Scenario 2: Just Entered Medium (15,000 credentials)
**15,000 credentials, 75,000 transactions/year**

```
Option 3 Calculation:
Base Platform: $1,375,524
Credentials: 5,000 × $10 = $50,000
Transactions: $0 (within 500K included for Medium)
Total (inc GST): $1,425,524

Option 1 Comparison: Medium Scale (over 10K threshold)
Option 1 Total: $1,773,126
```
**Result:** Option 3 is $347,602 CHEAPER (-19.6%)

### Scenario 3: Mid-Medium Scale (50,000 credentials)
**50,000 credentials, 250,000 transactions/year**

```
Option 3 Calculation:
Base Platform: $1,375,524
Credentials: 40,000 × $10 = $400,000
Transactions: $0 (within 500K included for Medium)
Total (inc GST): $1,775,524

Option 1 Comparison: Medium Scale (over 10K threshold)
Option 1 Total: $1,773,126
```
**Result:** Option 3 is $2,398 MORE expensive (+0.1%)

### Scenario 4: Upper Medium (90,000 credentials)
**90,000 credentials, 450,000 transactions/year**

```
Option 3 Calculation:
Base Platform: $1,375,524
Credentials: 80,000 × $10 = $800,000
Transactions: $0 (within 500K included)
Total (inc GST): $2,175,524

Option 1 Comparison: Medium Scale (over 10K threshold)
Option 1 Total: $1,773,126
```
**Result:** Option 3 is $402,398 MORE expensive (+22.7%)

### Scenario 5: Just Crossed to Large (101,000 credentials)
**101,000 credentials, 505,000 transactions/year**

```
Option 3 Calculation:
Base Platform: $1,375,524
Credentials: 10K free + 90K × $10 + 1K × $2 = $900,000 + $2,000 = $902,000
Transactions: $0 (within 1M included for Large)
Total (inc GST): $2,277,524

Option 1 Comparison: Large Scale (over 100K threshold)
Option 1 Total: $3,115,152
```
**Result:** Option 3 is $837,628 CHEAPER (-26.9%)

### Scenario 6: Mid-Large Scale (500,000 credentials)
**500,000 credentials, 2,500,000 transactions/year**

```
Option 3 Calculation:
Base Platform: $1,375,524
Credentials: 10K free + 90K × $10 + 400K × $2 = $900,000 + $800,000 = $1,700,000
Transactions: 1,500,000 × $0.10 = $150,000
Total (inc GST): $3,225,524

Option 1 Comparison: Large Scale (over 100K threshold)
Option 1 Total: $3,115,152
```
**Result:** Option 3 is $110,372 MORE expensive (+3.5%)

### Scenario 7: Large Scale (1,000,000 credentials)
**1,000,000 credentials, 5,000,000 transactions/year**

```
Option 3 Calculation:
Base Platform: $1,375,524
Credentials: 10K free + 90K × $10 + 900K × $2 = $900,000 + $1,800,000 = $2,700,000
Transactions: 4,000,000 × $0.10 = $400,000
Total (inc GST): $4,475,524

Option 1 Comparison: Large Scale (over 100K threshold)
Option 1 Total: $3,115,152
```
**Result:** Option 3 is $1,360,372 MORE expensive (+43.7%)

### Scenario 8: Very Large Scale (2,000,000 credentials)
**2,000,000 credentials, 10,000,000 transactions/year**

```
Option 3 Calculation:
Base Platform: $1,375,524
Credentials: 10K free + 90K × $10 + 1,900K × $2 = $900,000 + $3,800,000 = $4,700,000
Transactions: 9,000,000 × $0.10 = $900,000
Total (inc GST): $6,975,524

Option 1 Comparison: Large Scale (over 100K threshold)
Option 1 Total: $3,115,152
```
**Result:** Option 3 is $3,860,372 MORE expensive (+123.9%)
**Note:** At 2M credentials, Option 1 Large pricing may need review

---

## Breakeven Analysis

### Finding the Sweet Spots

#### Small Scale Breakeven
**Maximum credentials to match Option 1 Small ($1,375,524):**
- With 10,000 credentials and 50,000 transactions = EXACT MATCH ✓
- Any credentials above 10,000 make Option 3 more expensive

#### Medium Scale Breakeven Attempt
**Target: Match Option 1 Medium ($1,773,126)**
```
To reach $1,773,126:
Base: $1,375,524
Available for credentials/transactions: $397,602

With Medium tier pricing ($10/credential):
Maximum additional credentials: 39,760
Total credentials: 49,760 (10K free + 39,760 paid)

But 49,760 < 100,000, so Option 1 would still be Small scale ($1,375,524)
```
**Result:** Cannot achieve true breakeven at Medium scale

#### Large Scale Breakeven Attempt
**Target: Match Option 1 Large ($3,115,152)**
```
To reach $3,115,152:
Base: $1,375,524
Available for credentials/transactions: $1,739,628

With cumulative pricing:
- First 10K free: $0
- Next 90K at $10: $900,000
- Remaining budget: $839,628
- Additional Large tier credentials at $2: 419,814
- Total credentials: 519,814

But 519,814 is Medium scale in Option 1 ($1,773,126), not Large
```
**Result:** Cannot achieve breakeven - pricing structure too expensive

---

## Special Scenarios

### Scenario 9: Government Mandate - Rapid Adoption
**750,000 credentials, 3,750,000 transactions/year**

```
Option 3 Calculation:
Base Platform: $1,375,524
Credentials: 10K free + 90K × $10 + 650K × $2 = $900,000 + $1,300,000 = $2,200,000
Transactions: 2,750,000 × $0.10 = $275,000
Total (inc GST): $3,850,524

Option 1 Comparison: Large Scale (over 100K threshold)
Option 1 Total: $3,115,152
```
**Result:** Option 3 is $735,372 MORE expensive (+23.6%)

### Scenario 10: Pilot Extension - Minimal Growth
**5,000 credentials, 25,000 transactions/year**

```
Option 3 Calculation:
Base Platform: $1,375,524
Credentials: $0 (within 10K free)
Transactions: $0 (within 50K included)
Total (inc GST): $1,375,524

Option 1 Comparison: Small Scale
Option 1 Total: $1,375,524
```
**Result:** SAME PRICE ✓

### Scenario 11: Multi-Agency Consortium
**2,500,000 credentials, 12,500,000 transactions/year**

```
Option 3 Calculation:
Base Platform: $1,375,524
Credentials: 10K free + 90K × $10 + 2,400K × $2 = $900,000 + $4,800,000 = $5,700,000
Transactions: 11,500,000 × $0.10 = $1,150,000
Total (inc GST): $8,225,524

Option 1 Comparison: Large Scale (over 100K threshold)
Option 1 Total: $3,115,152
```
**Result:** Option 3 is $5,110,372 MORE expensive (+164.1%)
**Note:** At 2.5M credentials and 12.5M transactions, Option 1 Large pricing requires review

---

## Key Findings

### Advantages of Option 3:
1. **Clear tier structure** - Simple 10K/100K boundaries matching Option 1
2. **Predictable base cost** - Same starting point as Option 1 Small
3. **Free entry tier** - First 10,000 credentials always free
4. **Competitive at certain sweet spots:**
   - At 15,000 credentials: 19.6% CHEAPER than Option 1
   - At 101,000 credentials: 26.9% CHEAPER than Option 1
   - At 50,000 credentials: Almost same price (0.1% difference)

### Disadvantages:
1. **High Medium tier price** - $10 per credential (10,001-100,000) creates pricing pressure
2. **Expensive at very large scale** - Above 1M credentials becomes significantly more expensive
3. **Cumulative pricing impact** - Large scale still pays the expensive Medium tier rates

### Critical Observations:
1. **Option 3 has competitive zones:**
   - **Small deployments (≤10K):** Matches Option 1 exactly
   - **Early Medium (15K):** Actually cheaper than Option 1 Medium
   - **Medium scale (50K):** Nearly identical pricing to Option 1
   - **Early Large (101K):** Significantly cheaper than Option 1 Large
2. **Crossover points:**
   - Below 50K credentials: Option 3 is competitive or cheaper
   - Above 100K but below 500K: Still reasonable (+3.5% at 500K)
   - Above 1M: Becomes increasingly expensive
3. **Strategic positioning:** Option 3 works best for organizations expecting 10K-500K credentials

### Recommended Use Cases:
1. **Ideal for:** Organizations with 10K-100K credentials
2. **Reasonable for:** Organizations up to 500K credentials
3. **Not recommended for:** Very large deployments (>1M credentials)
4. **Review needed:** When credentials exceed 1M or transactions exceed 5M (Option 1 pricing review threshold)

---

## Assumptions
- All prices include 10% GST
- Each credential generates approximately 5 transactions per year
- Transaction volumes reset annually
- Credential counts are cumulative (never reset)
- PKI partitions are one-time charges
- Option 1 Large scale requires review if exceeding 1M credentials or 5M transactions

---

## Comparison Summary Table

| Credentials | Option 3 Total | Option 1 Scale | Option 1 Total | Difference | % Difference |
|-------------|---------------|----------------|----------------|------------|--------------|
| 5,000 | $1,375,524 | Small | $1,375,524 | $0 | 0.0% |
| 10,000 | $1,375,524 | Small | $1,375,524 | $0 | 0.0% |
| 15,000 | $1,425,524 | Medium | $1,773,126 | -$347,602 | -19.6% |
| 50,000 | $1,775,524 | Medium | $1,773,126 | +$2,398 | +0.1% |
| 90,000 | $2,175,524 | Medium | $1,773,126 | +$402,398 | +22.7% |
| 101,000 | $2,277,524 | Large | $3,115,152 | -$837,628 | -26.9% |
| 500,000 | $3,225,524 | Large | $3,115,152 | +$110,372 | +3.5% |
| 1,000,000 | $4,475,524 | Large | $3,115,152 | +$1,360,372 | +43.7% |
| 2,000,000 | $6,975,524 | Large* | $3,115,152 | +$3,860,372 | +123.9% |

*Note: Above 1M credentials or 5M transactions, Option 1 Large pricing requires review

---

[Back to Home](../README.md) | [View Option 1 Pricing](../NumbatWallet.wiki/Pricing-Assumptions.md)