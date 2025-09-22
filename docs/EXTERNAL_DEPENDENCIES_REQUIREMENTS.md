# External Dependencies Requirements for NumbatWallet PKI

## Executive Summary
This document outlines critical external dependencies required for completing the PKI and security infrastructure. These dependencies are **blocking** the completion of POA-125, POA-126, and POA-127.

## 1. ICAO PKD Access (POA-125)

### What We Need
- **ICAO PKD (Public Key Directory) Access Credentials**
- **Purpose**: To download and validate Country Signing CA (CSCA) certificates for mobile driving licenses
- **Standard**: ISO 18013-5 (mDL) compliance

### Who Can Provide
- **Primary Contact**: ServiceWA Digital Identity Team
- **Secondary Contact**: Australian Passport Office
- **Government Liaison Required**: Yes

### Request Template
```
Subject: Request for ICAO PKD Access - NumbatWallet Digital Identity Platform

Dear [ServiceWA Contact],

We are implementing the PKI infrastructure for the NumbatWallet digital identity platform
as part of the Western Australia Digital Identity tender. To comply with ISO 18013-5
standards for mobile driving licenses, we require access to:

1. ICAO PKD download credentials
2. Master List signing certificates
3. CSCA certificate bundle for Australia
4. Technical documentation for integration

Required Timeline: [Date]
Project Reference: NumbatWallet POA-125

Please advise on the approval process and any compliance requirements.

Regards,
[Your Name]
Platform Security Team
```

### Expected Timeline
- Request Submission: 1 day
- Government Approval: 2-3 weeks
- Technical Access Setup: 1 week
- **Total: 3-4 weeks**

### Contingency Plan
- Use ICAO test certificates for development
- Mock CSCA validation until production access

## 2. Government Trust List (POA-127)

### What We Need
- **Official list of trusted issuers and verifiers**
- **Format specification for trust list**
- **Update frequency and distribution mechanism**
- **Governance model for additions/removals**

### Who Can Provide
- **Primary**: WA State Government Digital Office
- **Secondary**: Australian Digital Identity Authority

### Required Information
```json
{
  "trustList": {
    "version": "1.0.0",
    "issuers": [
      {
        "id": "issuer-uuid",
        "name": "Department of Transport WA",
        "did": "did:web:transport.wa.gov.au",
        "certificateThumbprint": "sha256:...",
        "credentialTypes": ["DriversLicense", "ProofOfAge"],
        "status": "active",
        "validFrom": "2025-01-01",
        "validTo": "2030-01-01"
      }
    ],
    "verifiers": [
      {
        "id": "verifier-uuid",
        "name": "WA Police",
        "did": "did:web:police.wa.gov.au",
        "allowedCredentialTypes": ["*"],
        "trustLevel": "high"
      }
    ]
  }
}
```

### Action Items
1. Schedule stakeholder meeting with government
2. Define trust list schema
3. Agree on distribution mechanism (API, blockchain, etc.)
4. Establish update procedures

## 3. CSCA Root Certificates (POA-125)

### What We Need
- **Australian Country Signing CA certificates**
- **Document Signer Certificate chain**
- **CRL distribution points**
- **OCSP responder endpoints**

### Sources
1. **Australian Passport Office**
   - Contact: passport.pki@dfat.gov.au
   - Required: Government agency authorization

2. **Department of Home Affairs**
   - Immigration document certificates
   - Border control PKI infrastructure

### Certificate Requirements
```
Certificate Type: X.509v3
Key Usage: Digital Signature, Certificate Sign, CRL Sign
Extended Key Usage: Document Signing
Subject: C=AU, O=Commonwealth of Australia, OU=Australian Passport Office
Validity: 10-15 years typical
```

### Integration Points
- Certificate chain validation
- CRL checking every 24 hours
- OCSP real-time validation
- Certificate pinning for critical roots

## 4. Blockchain Infrastructure Decision (POA-127)

### Decision Required
**Which blockchain/DLT platform for trust list anchoring?**

### Options Analysis

#### Option A: Ethereum (Public or Private)
- **Pros**: Mature, wide tooling support, smart contracts
- **Cons**: Gas fees (if public), scalability concerns
- **Cost**: ~$50-500/month depending on usage

#### Option B: Hyperledger Fabric
- **Pros**: Enterprise-grade, permissioned, Australian government precedent
- **Cons**: Complex setup, requires nodes
- **Cost**: Infrastructure only (~$2000/month for nodes)

#### Option C: Hedera Hashgraph
- **Pros**: Fast finality, predictable fees, governance model
- **Cons**: Less adoption, newer technology
- **Cost**: ~$0.0001 per transaction

#### Option D: Australian Government Blockchain (if available)
- **Pros**: Government operated, compliance built-in
- **Cons**: May not exist or be accessible
- **Investigation Required**: Contact Digital Transformation Agency

### Recommendation
Start with abstraction layer to allow switching:

```csharp
public interface IDistributedLedger
{
    Task<string> AnchorDataAsync(byte[] data);
    Task<bool> VerifyAnchorAsync(string anchorId, byte[] data);
    Task<BlockchainMetadata> GetMetadataAsync(string anchorId);
}
```

## 5. Certificate Authority Access (POA-126)

### What We Need
- **Subordinate CA certificate from Australian Government CA**
- **Certificate issuance API access**
- **Policy OIDs for document signing**
- **Audit requirements**

### Process
1. Apply for subordinate CA certificate
2. Pass security audit (estimated 2-4 weeks)
3. Configure certificate templates
4. Establish secure communication channel

### Alternative: Azure Managed CA
If government CA not available:
- Deploy Azure Certificate Authority
- Cost: ~$300/month
- Can be operational in 1 week

## 6. API Credentials and Endpoints

### Required External APIs

#### ServiceWA Identity API
```
Endpoint: https://api.identity.wa.gov.au/v1
Authentication: OAuth 2.0 + mTLS
Required Scopes: identity.read, credential.issue
```

#### Medicare/Centrelink Integration
```
Endpoint: https://api.services.gov.au/identity/v2
Authentication: SAML 2.0 or OpenID Connect
Required: Agency agreement and security assessment
```

#### Australian Business Register
```
Endpoint: https://api.abr.gov.au/json
Authentication: API Key + IP Whitelisting
Purpose: Business identity verification
```

## 7. Compliance Documentation Requirements

### Required Assessments
1. **IRAP Assessment** (Information Security Registered Assessors Program)
   - Cost: $20,000-50,000
   - Timeline: 4-6 weeks
   - Required for government integration

2. **Privacy Impact Assessment**
   - Required by Office of Australian Information Commissioner
   - Timeline: 2-3 weeks

3. **Protective Security Policy Framework (PSPF) Compliance**
   - Government requirement
   - Self-assessment with validation

## 8. Action Plan and Timeline

### Week 1-2: Initial Requests
- [ ] Submit ICAO PKD access request
- [ ] Contact ServiceWA for trust list specification
- [ ] Schedule government stakeholder meeting
- [ ] Request CSCA certificates from Passport Office

### Week 3-4: Follow-ups and Decisions
- [ ] Blockchain platform decision
- [ ] Azure CA deployment (if needed)
- [ ] API credential requests
- [ ] Begin IRAP assessment process

### Week 5-8: Implementation Preparation
- [ ] Receive initial responses
- [ ] Configure test environments with mock data
- [ ] Document integration procedures
- [ ] Prepare for production cutover

### Week 9-12: Production Integration
- [ ] Integrate real ICAO PKD
- [ ] Configure production trust lists
- [ ] Complete security assessments
- [ ] Go-live preparation

## 9. Risk Mitigation Strategies

### If ICAO PKD Access Delayed
- Use test certificates from ICAO
- Implement with mock validation
- Plan for hot-swap when available

### If Government Trust List Not Available
- Define our own format
- Implement with test data
- Build adapter layer for future format

### If Blockchain Decision Delayed
- Implement with traditional database
- Add cryptographic timestamps
- Use append-only audit log

## 10. Budget Requirements

### One-Time Costs
- IRAP Assessment: $35,000
- Security Audit: $15,000
- Legal Review: $10,000
- **Total: $60,000**

### Recurring Costs (Monthly)
- ICAO PKD Access: $500
- Blockchain Infrastructure: $2,000
- API Subscriptions: $1,000
- **Total: $3,500/month**

## 11. Contact List

### Government Contacts
- ServiceWA Digital Identity: digitalidentity@wa.gov.au
- Australian Passport Office PKI: passport.pki@dfat.gov.au
- Digital Transformation Agency: blockchain@dta.gov.au

### Technical Support
- ICAO PKD Support: pkd@icao.int
- Azure Support: (via Azure Portal)
- Platform Security Team: platform-security@numbatwallet.gov.au

## 12. Escalation Path

If blockers persist:

1. **Level 1**: Technical Lead escalation
2. **Level 2**: Project Manager engagement
3. **Level 3**: Government Liaison Officer
4. **Level 4**: Executive Sponsor intervention

## Conclusion

These external dependencies are **critical path blockers** for POA-125, POA-126, and POA-127. Without resolution, the PKI infrastructure cannot be completed. Immediate action is required to initiate the request processes, as the combined timeline extends 8-12 weeks.

**Recommendation**: Assign a dedicated Government Liaison Officer to manage these external dependencies while the development team implements with mock data.

---
*Document Version: 1.0*
*Last Updated: September 22, 2025*
*Classification: OFFICIAL - Project Management*
*Next Review: October 1, 2025*