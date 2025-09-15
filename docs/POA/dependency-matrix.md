# POA Issue Dependency Matrix

## Critical Path Issues

These issues block the most other work and should be prioritized:

### Foundation Issues (Must Complete First)
1. **#1** - POA-001: Azure subscription setup → Blocks ALL infrastructure
2. **#10** - POA-012: Backend project structure → Blocks ALL backend work
3. **#62** - POA-100: Wallet app architecture → Blocks ALL wallet features
4. **#72** - POA-110: ISO 18013-5 implementation → Blocks mDL features
5. **#87** - POA-125: IACA root certificates → Blocks ALL PKI work

### Critical Path for Demo (Sequential)
1. Infrastructure Setup (#1)
   ↓
2. Backend Structure (#10)
   ↓
3. Authentication (#13)
   ↓
4. Credential APIs (#17)
   ↓
5. Wallet Architecture (#62)
   ↓
6. Wallet UI Implementation (#64-67)
   ↓
7. Communication Features (#95, #96, #68)
   ↓
8. Demo App Integration (#24)
   ↓
9. Live Demonstration (#19)

## Dependency Categories

### Wallet Dependencies
- **POA-100** (#62) → Blocks: All wallet features
- **POA-101** (#63) → Blocks: UI implementation
- **POA-102** (#64) → Blocks: Home screen access
- **POA-103** (#65) → Blocks: Detail views
- **POA-104** (#66) → Blocks: Sharing features
- **POA-105** (#67) → Blocks: Demo scenarios

### Standards Dependencies
- **POA-110** (#72) → Blocks: mDL features
- **POA-112** (#74) → Blocks: Verifiable presentations
- **POA-113** (#75) → Blocks: W3C VC implementation
- **POA-114** (#76) → Blocks: Issuance flow
- **POA-115** (#77) → Blocks: Presentation flow

### PKI Dependencies
- **POA-125** (#87) → Blocks: All PKI operations
- **POA-126** (#88) → Blocks: Credential signing
- **POA-127** (#89) → Blocks: Trust verification
- **POA-128** (#90) → Blocks: Key operations

### Backend Dependencies
- **POA-012** (#10) → Blocks: All backend
- **POA-021** (#13) → Blocks: Tenant operations
- **POA-023** (#15) → Blocks: Credential operations
- **POA-025** (#16) → Blocks: API implementation
- **POA-026** (#17) → Blocks: Demo scenarios

## Parallel Work Streams

These can be worked on independently:

### Stream 1: Infrastructure
- Azure setup (#1)
- Database (#2)
- Networking (#26)
- Key Vault (#4)
- Container Registry (#3)

### Stream 2: Standards
- ISO 18013-5 (#72)
- W3C VC (#74)
- W3C DID (#75)
- OpenID4VCI (#76)
- OpenID4VP (#77)

### Stream 3: Wallet UI/UX
- Design mockups (#63)
- UI components
- Accessibility
- Localization

### Stream 4: PKI
- IACA setup (#87)
- DSC implementation (#88)
- Trust lists (#89)
- HSM integration (#90)

## Demo Requirements

For successful demo, these must be complete:

### Week 1 Deliverables
- [ ] Infrastructure deployed (#1-9)
- [ ] Backend operational (#10-11)
- [ ] SDKs delivered (#12, #39, #41)

### Week 2 Deliverables
- [ ] Authentication working (#13)
- [ ] Credential APIs (#17)
- [ ] Wallet core features (#62-67)

### Week 3 Demo Requirements
- [ ] Offline verification (#94)
- [ ] QR codes (#95)
- [ ] NFC support (#96)
- [ ] Demo app (#24)
- [ ] Live presentation (#19)

## Risk Mitigation

### High-Risk Dependencies
1. **ServiceWA Integration** - Start early, daily sync
2. **PKI Certificates** - Order immediately
3. **Standards Compliance** - Engage consultant
4. **Device Testing** - Procure devices now
5. **Performance** - Continuous testing

### Contingency Plans
- If wallet delayed → Use white-label solution
- If PKI delayed → Use test certificates
- If standards fail → Document compliance roadmap
- If integration blocked → Mock endpoints
- If performance poor → Reduce concurrent users

---

*Last Updated: September 15, 2025*
