# NumbatWallet Infrastructure Gaps Analysis

## Executive Summary
Senior-level analysis reveals critical infrastructure gaps and external dependencies that block completion of security components. GitHub issues POA-128 and POA-130 are incorrectly marked as CLOSED when they are incomplete.

## 1. Infrastructure Prerequisites Not Met

### 1.1 Azure Resources Required But Not Provisioned

#### Azure Dedicated HSM (Critical)
- **Current State**: Using Azure Key Vault (software-protected keys)
- **Required**: Azure Dedicated HSM for FIPS 140-2 Level 2+ compliance
- **Gap**: No Dedicated HSM provisioned
- **Action Required**:
  ```bash
  # Request quota increase for Dedicated HSM
  az support tickets create \
    --ticket-name "Dedicated HSM Quota Request" \
    --title "Request Azure Dedicated HSM quota for NumbatWallet" \
    --severity "C" \
    --problem-classification "/providers/Microsoft.Support/services/quota_service_guid/problemClassifications/dedicated_hsm_guid"

  # After approval, deploy HSM
  az dedicated-hsm create \
    --resource-group rg-numbatwallet-prod \
    --name hsm-numbatwallet-prod \
    --location australiaeast \
    --sku SafeNet Luna Network HSM A790
  ```

#### Azure Certificate Authority
- **Current State**: No CA infrastructure
- **Required**: Managed CA for issuing Document Signing Certificates
- **Gap**: No Azure CA configured
- **Blocker**: Required for POA-126

#### Azure Functions
- **Current State**: No serverless compute for automation
- **Required**: For key rotation scheduling (POA-131)
- **Gap**: No Function App deployed

#### Azure Service Bus
- **Current State**: No messaging infrastructure
- **Required**: For event-driven notifications
- **Gap**: No Service Bus namespace

### 1.2 Bicep Templates Status

Current Bicep module requirements (from POA-009 series):
- ✅ POA-009a: Main orchestrator template
- ✅ POA-009b: Networking module
- ✅ POA-009c: Database module
- ❌ POA-009d: **Dedicated HSM module** (MISSING)
- ❌ POA-009e: **Certificate Authority module** (MISSING)
- ❌ POA-009f: **Function App module** (MISSING)

## 2. External Dependencies Blocking Progress

### 2.1 ICAO PKD Access (POA-125)
- **Dependency**: International Civil Aviation Organization PKD access
- **Current State**: No access credentials
- **Required From**: ServiceWA / Australian Government
- **Timeline**: 4-6 weeks typical approval
- **Impact**: Blocks mobile driving license verification

### 2.2 Government Trust List (POA-127)
- **Dependency**: Approved issuer/verifier list
- **Current State**: Not provided
- **Required From**: WA State Government
- **Format**: Unknown (need specification)
- **Impact**: Cannot validate trusted entities

### 2.3 CSCA Root Certificates (POA-125)
- **Dependency**: Country Signing CA certificates
- **Current State**: Not available
- **Required From**: Australian Passport Office
- **Impact**: Cannot validate identity documents

### 2.4 Blockchain Infrastructure Decision (POA-127)
- **Dependency**: Architecture decision on DLT
- **Options**: Ethereum, Hyperledger Fabric, Hedera
- **Current State**: No decision made
- **Impact**: Trust list anchoring design blocked

## 3. Implementation Status Reality Check

### POA-128: HSM Integration
**GitHub Status**: CLOSED ❌ (Incorrect)
**Actual Status**: 40% Complete

**Completed**:
- ✅ IHsmService interface
- ✅ Basic HsmService with Key Vault
- ✅ Unit tests

**Missing Critical Components**:
- ❌ Azure Dedicated HSM integration
- ❌ FIPS 140-2 Level 2+ certification
- ❌ M of N authorization
- ❌ Key ceremony procedures
- ❌ High availability configuration
- ❌ Hardware backup procedures

### POA-130: Revocation Registry
**GitHub Status**: CLOSED ❌ (Incorrect)
**Actual Status**: 60% Complete

**Completed**:
- ✅ IRevocationRegistryService interface
- ✅ Basic CRL generation
- ✅ OCSP data structures
- ✅ Unit tests

**Missing Critical Components**:
- ❌ OCSP responder endpoint
- ❌ Certificate Authority integration
- ❌ Distributed synchronization
- ❌ Production CRL distribution
- ❌ Real-time revocation checking

### POA-125: IACA Root Certificates
**GitHub Status**: OPEN ✅
**Actual Status**: 0% - Blocked by external dependencies

**Blockers**:
- No ICAO PKD access
- No CSCA certificates
- No Master List access

### POA-126: Document Signing Certificates
**GitHub Status**: OPEN ✅
**Actual Status**: 0% - Blocked by POA-125 and infrastructure

**Blockers**:
- POA-125 must complete first (trust chain)
- No Azure CA infrastructure
- No HSM for key protection

### POA-127: Trust List Management
**GitHub Status**: OPEN ✅
**Actual Status**: 0% - Blocked by external decisions

**Blockers**:
- No government trust list
- No blockchain decision
- No distribution infrastructure

### POA-131: Key Rotation Policies
**GitHub Status**: OPEN ✅
**Actual Status**: 20% Complete

**Completed**:
- ✅ Basic RotateKeyAsync in HsmService

**Missing**:
- ❌ KeyRotationService
- ❌ Policy configuration
- ❌ Azure Functions scheduling
- ❌ Grace period management
- ❌ Automated rotation

## 4. Resource Requirements

### Azure Resources Needed
```json
{
  "dedicatedHsm": {
    "sku": "SafeNet Luna Network HSM A790",
    "location": "australiaeast",
    "estimatedCost": "$4,000/month"
  },
  "certificateAuthority": {
    "type": "Azure Managed CA",
    "tier": "Premium",
    "estimatedCost": "$300/month"
  },
  "functionApp": {
    "plan": "Premium P1V3",
    "runtime": "dotnet-isolated",
    "estimatedCost": "$200/month"
  },
  "serviceBus": {
    "tier": "Standard",
    "estimatedCost": "$10/month"
  }
}
```

### Personnel Requirements
- HSM Administrator (for key ceremonies)
- PKI Specialist (for certificate infrastructure)
- Government Liaison (for external dependencies)

## 5. Recommended Action Plan

### Immediate Actions (Week 1)
1. **Escalate to Management**:
   - Infrastructure budget approval (~$4,500/month)
   - Dedicated HSM quota request
   - Government liaison assignment

2. **Update GitHub Issues**:
   - Reopen POA-128 with remaining tasks
   - Reopen POA-130 with missing components
   - Add dependency labels to blocked issues

3. **Create Infrastructure Tickets**:
   - Deploy Dedicated HSM
   - Setup Azure CA
   - Configure Function Apps
   - Create Service Bus

### Short Term (Weeks 2-4)
1. **External Dependencies**:
   - Schedule ServiceWA meeting for ICAO PKD
   - Request government trust list specification
   - Initiate CSCA certificate request

2. **Infrastructure Deployment**:
   - Deploy Bicep templates for missing resources
   - Configure networking for HSM
   - Setup monitoring and alerts

### Medium Term (Weeks 5-8)
1. **Complete POA-128** (HSM):
   - Migrate to Dedicated HSM
   - Implement M of N authorization
   - Conduct key ceremony

2. **Complete POA-130** (Revocation):
   - Deploy OCSP responder
   - Integrate with CA
   - Setup distribution

3. **Start POA-131** (Key Rotation):
   - Deploy automation functions
   - Configure policies
   - Test rotation procedures

### Long Term (Weeks 9-12)
1. **POA-125**: IACA certificates (pending external)
2. **POA-126**: Document signing (after POA-125)
3. **POA-127**: Trust lists (pending blockchain decision)

## 6. Risk Mitigation

### High Risk Items
1. **Dedicated HSM Quota**: May take 2-3 weeks for approval
   - *Mitigation*: Start approval process immediately

2. **ICAO PKD Access**: Government process, 4-6 weeks
   - *Mitigation*: Use mock data for development

3. **Blockchain Decision**: Architecture impact
   - *Mitigation*: Build abstraction layer

### Contingency Plans
- Use Key Vault HSM-protected keys if Dedicated HSM delayed
- Implement mock trust list for testing
- Design pluggable blockchain interface

## 7. Success Criteria

### Infrastructure Success
- [ ] All Azure resources provisioned
- [ ] Dedicated HSM operational
- [ ] Certificate Authority configured
- [ ] Automation functions deployed

### Implementation Success
- [ ] POA-128 truly complete with Dedicated HSM
- [ ] POA-130 with working OCSP responder
- [ ] POA-131 with automated rotation
- [ ] All tests passing with real infrastructure

### Compliance Success
- [ ] FIPS 140-2 Level 2+ certified
- [ ] TDIF compliance verified
- [ ] Security audit passed
- [ ] Performance benchmarks met

## Conclusion

This analysis reveals significant gaps between the reported status and reality. The infrastructure prerequisites and external dependencies must be resolved before the PKI components can be properly implemented. The current "closed" status of POA-128 and POA-130 is misleading and should be corrected to reflect the actual implementation gaps.

**Recommendation**: Treat this as a P0 infrastructure initiative requiring executive sponsorship and dedicated resources.

---
*Analysis Date: September 22, 2025*
*Analyst: Senior Backend Architect*
*Classification: OFFICIAL - Infrastructure Planning*