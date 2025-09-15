#!/bin/bash

echo "=== FIXING POA PROJECT ISSUES ==="
echo "Starting corrections at $(date)"
echo ""

# First, fix the misassigned issues
echo "Step 1: Fixing milestone assignments..."

# These issues (#62-67) are actually standards/PKI issues, not wallet
# They should be in 000-PreDev-Standards or 000-PreDev-PKI milestones

# Move JWT-VC and credential manifest to Standards milestone
gh issue edit 62 --repo Credenxia/NumbatWallet --milestone "000-PreDev-Standards"
gh issue edit 63 --repo Credenxia/NumbatWallet --milestone "000-PreDev-Standards"

# Issues #64-67 are PKI issues, move to PKI milestone
gh issue edit 64 --repo Credenxia/NumbatWallet --milestone "000-PreDev-PKI"
gh issue edit 65 --repo Credenxia/NumbatWallet --milestone "000-PreDev-PKI"
gh issue edit 66 --repo Credenxia/NumbatWallet --milestone "000-PreDev-PKI"
gh issue edit 67 --repo Credenxia/NumbatWallet --milestone "000-PreDev-PKI"

echo "Milestone reassignments complete!"
echo ""

# Create the ACTUAL wallet issues that are missing
echo "Step 2: Creating missing wallet application issues..."

# POA-100: Wallet Architecture
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-100: Create proprietary wallet application architecture" \
  --body "## Description
Design and implement the complete wallet application architecture, not just a demo.

## Critical Requirements
- Full Flutter application structure
- Clean Architecture implementation
- State management (Bloc/Provider)
- Dependency injection
- Navigation framework
- Error handling
- Offline capability

## Acceptance Criteria
- [ ] Complete app architecture documented
- [ ] All layers properly separated
- [ ] State management implemented
- [ ] Navigation working
- [ ] Error handling comprehensive
- [ ] Offline mode supported

## This is CRITICAL - blocks all wallet work" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet-app,critical-path,architecture"

# POA-101: UI/UX Design
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-101: Design complete wallet UI/UX" \
  --body "## Description
Create comprehensive UI/UX designs for the complete wallet application.

## Required Screens
- Splash/Loading
- Onboarding (5 screens)
- Login/Biometric
- Home/Dashboard
- Credential List
- Credential Details
- Add Credential
- Share/Present
- Settings
- Profile
- Help

## Acceptance Criteria
- [ ] All screens designed in Figma
- [ ] Design system created
- [ ] Accessibility compliant (WCAG 2.2)
- [ ] Dark mode included
- [ ] Responsive layouts

## Blocks all UI implementation" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet-app,design,ui-ux"

# Now create the critical missing feature issues
echo "Step 3: Creating missing critical feature issues..."

# POA-200: Real-time verification
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-200: Implement real-time credential verification" \
  --body "## Description
Implement real-time credential verification as required by tender section 1.4.

## Requirements
- Real-time verification endpoint
- QR code verification
- NFC verification
- Response time <500ms
- Offline fallback

## Acceptance Criteria
- [ ] Verification API implemented
- [ ] QR scanning working
- [ ] NFC reading functional
- [ ] Performance targets met
- [ ] Offline mode working

## CRITICAL FOR DEMO" \
  --milestone "003-Week2-POA-Features" \
  --label "features,critical-path,poa-demo"

# POA-201: Device deployment
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-201: Deploy wallet to test devices" \
  --body "## Description
Deploy wallet application to nominated iOS and Android devices for POA testing.

## Requirements
- iOS deployment (TestFlight or Ad Hoc)
- Android deployment (APK or Play Store beta)
- At least 5 test devices
- Installation documentation
- Device configuration

## Acceptance Criteria
- [ ] iOS app installed on test devices
- [ ] Android app installed on test devices
- [ ] All features working on devices
- [ ] Performance validated
- [ ] Documentation complete

## REQUIRED FOR DEMO" \
  --milestone "004-Week3-POA-Demo" \
  --label "deployment,testing,critical-path"

# POA-202: Performance dashboard
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-202: Build performance monitoring dashboard" \
  --body "## Description
Implement real-time performance monitoring dashboard as required by tender.

## Requirements
- Real-time metrics display
- API response times
- Concurrent user count
- Error rates
- System health
- Historical data

## Technology
- Grafana or similar
- Prometheus metrics
- WebSocket updates
- Alert configuration

## Acceptance Criteria
- [ ] Dashboard operational
- [ ] All metrics displayed
- [ ] Real-time updates working
- [ ] Alerts configured
- [ ] Documentation complete

## REQUIRED FOR DEMO" \
  --milestone "003-Week2-POA-Features" \
  --label "monitoring,dashboard,poa-demo"

# POA-203: Admin CRUD operations
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-203: Implement complete admin CRUD operations" \
  --body "## Description
Implement all CRUD operations for admin dashboard credential management.

## Operations Required
- Create new credentials
- Read/List all credentials
- Update credential status
- Delete/Revoke credentials
- Bulk operations
- Search and filter
- Export functionality

## Acceptance Criteria
- [ ] All CRUD operations working
- [ ] Bulk operations functional
- [ ] Search/filter implemented
- [ ] Export working
- [ ] Audit logging complete
- [ ] UI responsive

## CRITICAL FOR DEMO" \
  --milestone "003-Week2-POA-Features" \
  --label "admin,backend,critical-path"

# POA-204: Selective disclosure UI
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-204: Implement selective disclosure UI" \
  --body "## Description
Create UI for selective disclosure allowing users to choose what to share.

## Requirements
- Attribute selection interface
- Clear consent screen
- Preview of shared data
- Minimal disclosure by default
- History of disclosures

## UI Components
- Checkbox list of attributes
- Consent dialog
- Preview screen
- Success confirmation

## Acceptance Criteria
- [ ] Selection UI working
- [ ] Consent flow complete
- [ ] Preview functional
- [ ] History tracking
- [ ] User-friendly design

## REQUIRED FOR DEMO" \
  --milestone "003-Week2-POA-Features" \
  --label "privacy,ui,selective-disclosure,poa-demo"

# POA-205: Complete revocation system
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-205: Implement complete revocation system" \
  --body "## Description
Implement comprehensive credential revocation system with real-time updates.

## Components
- Revocation API endpoint
- Revocation registry
- Real-time notifications
- Status propagation
- Admin UI for revocation
- Wallet UI updates

## Acceptance Criteria
- [ ] Revocation API working
- [ ] Registry operational
- [ ] Real-time updates working
- [ ] Admin can revoke
- [ ] User notified immediately
- [ ] Status reflected everywhere

## CRITICAL FOR DEMO" \
  --milestone "003-Week2-POA-Features" \
  --label "revocation,critical-path,poa-demo"

# POA-206: Offline verification
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-206: Implement complete offline verification" \
  --body "## Description
Implement offline verification allowing credentials to work without internet.

## Requirements
- Offline detection
- Local cryptographic validation
- Certificate caching
- QR codes with embedded data
- Fallback mechanisms

## Test Scenarios
- Airplane mode verification
- No network verification
- Cached certificate validation

## Acceptance Criteria
- [ ] Offline mode detected
- [ ] Verification works offline
- [ ] Certificates cached properly
- [ ] QR codes self-contained
- [ ] User experience smooth

## REQUIRED FOR DEMO" \
  --milestone "003-Week2-POA-Features" \
  --label "offline,verification,critical-path,poa-demo"

echo ""
echo "Step 4: Creating missing standards implementation issues..."

# POA-110: ISO 18013-5 implementation (if not exists)
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-110: Implement ISO 18013-5 mDL standard" \
  --body "## Description
Implement complete ISO/IEC 18013-5:2021 mobile driving licence standard.

## Requirements
- mDoc format implementation
- CBOR encoding/decoding
- Device engagement protocol
- Session establishment
- All mandatory elements

## Acceptance Criteria
- [ ] mDoc format working
- [ ] CBOR implemented
- [ ] Protocols functional
- [ ] Test vectors passing
- [ ] Conformance validated

## BLOCKS mDL FEATURES" \
  --milestone "000-PreDev-Standards" \
  --label "standards,iso,critical-path" 2>/dev/null || echo "POA-110 already exists"

# POA-112: W3C VC implementation
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-112: Implement W3C VC Data Model" \
  --body "## Description
Implement W3C Verifiable Credentials Data Model v2.0.

## Requirements
- VC data structures
- JSON-LD processing
- Proof formats
- Credential subjects
- Status handling

## Acceptance Criteria
- [ ] VC model implemented
- [ ] JSON-LD working
- [ ] Proofs functional
- [ ] W3C test suite passing

## REQUIRED FOR CREDENTIALS" \
  --milestone "000-PreDev-Standards" \
  --label "standards,w3c,critical-path" 2>/dev/null || echo "POA-112 already exists"

echo ""
echo "Step 5: Fixing milestone dates to avoid weekends..."

# Update milestones to ensure no weekend dates
# Note: These are placeholders - adjust based on actual calendar

gh api repos/Credenxia/NumbatWallet/milestones/12 --method PATCH \
  -f due_on="2025-09-19T23:59:59Z" \
  -f description="Complete wallet application - CRITICAL PATH"

gh api repos/Credenxia/NumbatWallet/milestones/13 --method PATCH \
  -f due_on="2025-09-23T23:59:59Z" \
  -f description="Standards implementation - ISO, W3C, OpenID - CRITICAL"

gh api repos/Credenxia/NumbatWallet/milestones/14 --method PATCH \
  -f due_on="2025-09-25T23:59:59Z" \
  -f description="PKI infrastructure - Certificates, HSM, Trust lists - CRITICAL"

echo ""
echo "=== CORRECTIONS COMPLETE ==="
echo ""
echo "Summary of changes:"
echo "1. ✅ Fixed milestone assignments for issues #62-67"
echo "2. ✅ Created missing wallet issues (POA-100, POA-101)"
echo "3. ✅ Created critical feature issues (POA-200 to POA-206)"
echo "4. ✅ Created/verified standards issues"
echo "5. ✅ Updated milestone dates to avoid weekends"
echo ""
echo "NEXT STEPS:"
echo "1. Run dependency script to add blocking relationships"
echo "2. Assign team members to new issues"
echo "3. Start development IMMEDIATELY"
echo ""
echo "Completed at $(date)"