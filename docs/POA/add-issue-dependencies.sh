#!/bin/bash

echo "Adding blocking dependencies between issues..."

# Function to add dependency comment
add_dependency() {
  local issue=$1
  local blocks=$2
  local blocked_by=$3
  local description=$4

  comment="## 🔗 Dependencies

**Blocks:** $blocks
**Blocked by:** $blocked_by

$description

---
*Updated by dependency script on $(date)*"

  gh issue comment $issue --repo Credenxia/NumbatWallet --body "$comment"
}

# WALLET APP DEPENDENCIES
echo "Setting wallet app dependencies..."

# POA-100 (Architecture) blocks all other wallet issues
add_dependency 62 "#63, #64, #65, #66, #67, #68, #69, #70, #71" "None - Foundation issue" \
  "This is the foundation architecture that all other wallet features depend on."

# POA-101 (UI/UX Design) blocks UI implementation
add_dependency 63 "#64, #65, #66, #67, #68, #69" "#62" \
  "UI/UX designs must be complete before implementing screens."

# POA-102 (Onboarding) blocks home screen
add_dependency 64 "#65" "#62, #63" \
  "Onboarding must be complete before users can access the home screen."

# POA-103 (Home screen) blocks detail views
add_dependency 65 "#66, #67" "#62, #63, #64" \
  "Home screen is required before implementing detail views."

# POA-104 (Detail view) blocks sharing
add_dependency 66 "#67" "#65" \
  "Detail view needed before sharing functionality."

# POA-105 (Sharing) depends on many
add_dependency 67 "Demo scenarios" "#66, #95 (QR), #96 (NFC), #68 (Bluetooth)" \
  "Sharing is critical for demo and depends on multiple communication methods."

# STANDARDS DEPENDENCIES
echo "Setting standards dependencies..."

# POA-110 (ISO 18013-5) is foundation for mDL
add_dependency 72 "#73, #120" "None - Foundation standard" \
  "ISO 18013-5 is the base standard for mobile driving licenses."

# POA-111 (ISO 18013-7) extends 18013-5
add_dependency 73 "Online verification demo" "#72" \
  "ISO 18013-7 extends 18013-5 for online scenarios."

# POA-112 (W3C VC) blocks presentations
add_dependency 74 "#122" "None - Foundation standard" \
  "W3C VC Data Model is required for verifiable presentations."

# POA-113 (DIDs) blocks VC implementation
add_dependency 75 "#74, #122" "None - Foundation standard" \
  "DIDs are required for W3C VC implementation."

# POA-114 (OpenID4VCI) blocks issuance
add_dependency 76 "#17 (issuance API)" "None" \
  "OpenID4VCI protocol required for standards-compliant issuance."

# POA-115 (OpenID4VP) blocks presentation
add_dependency 77 "Verification demo" "#76" \
  "OpenID4VP protocol required for standards-compliant presentation."

# PKI DEPENDENCIES
echo "Setting PKI dependencies..."

# POA-125 (IACA) blocks all PKI
add_dependency 87 "#88, #89, #90, #129, #130" "None - Root certificates" \
  "IACA root certificates are foundation of PKI hierarchy."

# POA-126 (DSC) depends on IACA
add_dependency 88 "Credential signing" "#87" \
  "Document Signing Certificates must be issued from IACA."

# POA-127 (Trust lists) depends on PKI
add_dependency 89 "Trust verification" "#87, #88" \
  "Trust lists require PKI infrastructure."

# POA-128 (HSM) blocks key operations
add_dependency 90 "#129, #131" "#87" \
  "HSM required for secure key operations."

# BACKEND DEPENDENCIES
echo "Setting backend dependencies..."

# POA-012 (Backend structure) blocks all backend
add_dependency 10 "All backend issues" "None - Foundation" \
  "Backend project structure is foundation for all backend work."

# POA-013 (Health checks) blocks deployment
add_dependency 11 "#37 (deployment)" "#10" \
  "Health checks required before deployment."

# POA-021 (OIDC auth) blocks tenant resolution
add_dependency 13 "#15, #14" "#10" \
  "Authentication required before tenant resolution."

# POA-023 (Tenant resolution) blocks credential ops
add_dependency 15 "#16, #17" "#13" \
  "Tenant resolution required for multi-tenant operations."

# POA-025 (CQRS) blocks API endpoints
add_dependency 16 "#17" "#10" \
  "CQRS pattern required before implementing API endpoints."

# POA-026 (Issuance API) blocks demo
add_dependency 17 "Demo scenarios" "#16, #76" \
  "Issuance API is critical for demonstration."

# INFRASTRUCTURE DEPENDENCIES
echo "Setting infrastructure dependencies..."

# POA-001 (Azure setup) blocks everything
add_dependency 1 "All infrastructure" "None - Foundation" \
  "Azure subscription is foundation for all infrastructure."

# POA-002 (PostgreSQL) blocks migrations
add_dependency 2 "#32" "#1" \
  "Database must exist before migrations."

# POA-004 (Network) blocks gateway
add_dependency 26 "#27" "#1" \
  "Virtual network required before gateway."

# POA-005 (Key Vault) blocks PKI
add_dependency 4 "#87, #90" "#1" \
  "Key Vault required for secrets and PKI."

# INTEGRATION DEPENDENCIES
echo "Setting integration dependencies..."

# POA-031 (Flutter auth) depends on backend
add_dependency 18 "Flutter app auth" "#13" \
  "Flutter authentication module depends on backend OIDC."

# POA-034 (.NET SDK) depends on API
add_dependency 40 "Agency integration" "#17" \
  ".NET SDK depends on backend APIs."

# TESTING DEPENDENCIES
echo "Setting testing dependencies..."

# POA-081 (Test framework) blocks all tests
add_dependency 43 "All test issues" "#10" \
  "Test framework required before writing tests."

# POA-089 (Auth tests) depends on auth
add_dependency 50 "Test suite" "#13" \
  "Authentication tests depend on auth implementation."

# POA-096 (Load testing) depends on features
add_dependency 57 "Performance validation" "#17" \
  "Load testing requires working features."

# DEMO DEPENDENCIES
echo "Setting demo dependencies..."

# POA-041 (Demo app) depends on everything
add_dependency 24 "Live demonstration" "#67, #94, #95, #96" \
  "Demo app requires wallet, offline, QR, NFC features."

# POA-048 (Demo presentation) is final
add_dependency 19 "Contract decision" "All POA tasks" \
  "Demo presentation depends on all components working."

echo "Dependencies added successfully!"

# Create a dependency matrix document
echo "Creating dependency matrix documentation..."

cat > /Users/rodrigolmiranda/repo/NumbatWallet/docs/POA/dependency-matrix.md << 'EOF'
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
EOF

echo "Dependency matrix documentation created!"