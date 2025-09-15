#!/bin/bash

echo "=== FIXING MILESTONE STRUCTURE AND DATES ==="
echo "Starting at $(date)"
echo ""

# Fix milestone prefixes and dates to avoid conflicts and respect dependencies
echo "Step 1: Updating milestone titles and dates..."

# Pre-Development Phase (Sept 16 - Oct 2)
gh api repos/Credenxia/NumbatWallet/milestones/12 --method PATCH \
  -f title="000-PreDev-WalletApp" \
  -f due_on="2025-09-20T23:59:59Z" \
  -f description="Wallet application architecture and UI design (5 days)"

gh api repos/Credenxia/NumbatWallet/milestones/13 --method PATCH \
  -f title="001-PreDev-Standards" \
  -f due_on="2025-09-27T23:59:59Z" \
  -f description="Standards implementation - ISO, W3C, OpenID (5 days)"

gh api repos/Credenxia/NumbatWallet/milestones/14 --method PATCH \
  -f title="002-PreDev-PKI" \
  -f due_on="2025-09-27T23:59:59Z" \
  -f description="PKI infrastructure - Certificates, HSM, Trust lists (5 days, parallel with standards)"

gh api repos/Credenxia/NumbatWallet/milestones/1 --method PATCH \
  -f title="003-PreDev-Infrastructure" \
  -f due_on="2025-09-30T23:59:59Z" \
  -f description="Azure infrastructure and backend foundation (6 days)"

gh api repos/Credenxia/NumbatWallet/milestones/15 --method PATCH \
  -f title="004-PreDev-Integration" \
  -f due_on="2025-10-02T23:59:59Z" \
  -f description="ServiceWA and IdX integration preparation (3 days)"

# Official POA Phase (Oct 3 - Nov 1)
gh api repos/Credenxia/NumbatWallet/milestones/10 --method PATCH \
  -f title="010-Week1-POA-Deployment" \
  -f due_on="2025-10-04T23:59:59Z" \
  -f description="Official Week 1: Infrastructure deployment and SDK delivery (2 days)"

gh api repos/Credenxia/NumbatWallet/milestones/6 --method PATCH \
  -f title="020-Week2-POA-Features" \
  -f due_on="2025-10-11T23:59:59Z" \
  -f description="Official Week 2: Feature implementation and testing (5 days)"

# Fix the duplicate 004 prefix - rename AuthAPIs milestone
gh api repos/Credenxia/NumbatWallet/milestones/2 --method PATCH \
  -f title="025-Week2-POA-AuthAPIs" \
  -f due_on="2025-10-11T23:59:59Z" \
  -f description="Authentication and authorization APIs (parallel with features)"

gh api repos/Credenxia/NumbatWallet/milestones/3 --method PATCH \
  -f title="030-Week3-POA-Demo" \
  -f due_on="2025-10-18T23:59:59Z" \
  -f description="Official Week 3: Live demonstration preparation (5 days)"

gh api repos/Credenxia/NumbatWallet/milestones/4 --method PATCH \
  -f title="040-Week4-POA-Testing" \
  -f due_on="2025-10-25T23:59:59Z" \
  -f description="Official Week 4: DGov UAT support (5 days)"

gh api repos/Credenxia/NumbatWallet/milestones/5 --method PATCH \
  -f title="050-Week5-POA-Evaluation" \
  -f due_on="2025-11-01T23:59:59Z" \
  -f description="Official Week 5: Final evaluation and decision (5 days)"

# Delete unused milestone
gh api repos/Credenxia/NumbatWallet/milestones/7 --method DELETE 2>/dev/null || echo "Milestone 7 already deleted"

echo ""
echo "Step 2: Creating missing wallet application issues..."

# Create the missing wallet issues (POA-100 to POA-113)
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-100: Design wallet application architecture" \
  --body "## Description
Design complete wallet application architecture for production use, not just demo.

## Requirements
- Clean Architecture implementation
- Flutter best practices
- State management (Bloc/Riverpod)
- Dependency injection
- Navigation framework
- Error handling patterns
- Offline capability architecture

## Acceptance Criteria
- [ ] Architecture document created
- [ ] All layers defined (presentation, domain, data)
- [ ] State management approach documented
- [ ] Dependency injection configured
- [ ] Navigation structure defined
- [ ] Error handling comprehensive
- [ ] Offline mode architecture complete

## Dependencies
Blocks all other wallet development work" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet,architecture,critical-path"

gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-101: Create wallet UI/UX designs" \
  --body "## Description
Design all screens and user flows for the wallet application.

## Required Screens
- Splash screen
- Onboarding flow (5 screens)
- Biometric/PIN setup
- Home dashboard
- Credential list
- Credential details
- Add credential flow
- Share/present credential
- Settings
- Profile management
- Help/support

## Acceptance Criteria
- [ ] All screens designed in Figma
- [ ] Design system created
- [ ] Accessibility WCAG 2.2 compliant
- [ ] Dark mode included
- [ ] Responsive layouts for all devices
- [ ] User flows documented
- [ ] Prototype created

## Dependencies
Blocks UI implementation" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet,design,ui-ux"

gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-102: Implement wallet onboarding flow" \
  --body "## Description
Implement the complete onboarding experience for new users.

## Components
- Welcome screens
- Privacy consent
- Biometric enrollment
- PIN setup
- Recovery phrase generation
- Tutorial screens

## Acceptance Criteria
- [ ] All onboarding screens implemented
- [ ] Biometric enrollment working
- [ ] PIN setup functional
- [ ] Recovery phrase secure
- [ ] Skip option available
- [ ] Progress indicator shown
- [ ] Animations smooth

## Dependencies
Depends on POA-101 (UI designs)" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet,frontend,onboarding"

gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-103: Build wallet home dashboard" \
  --body "## Description
Implement the main home screen showing credential summary.

## Features
- Credential count badges
- Quick actions
- Recent activity
- Notifications preview
- Profile summary
- Navigation menu

## Acceptance Criteria
- [ ] Dashboard layout complete
- [ ] Real-time updates working
- [ ] Quick actions functional
- [ ] Navigation working
- [ ] Pull to refresh
- [ ] Loading states
- [ ] Empty states

## Dependencies
Depends on POA-101 (UI designs)" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet,frontend,dashboard"

gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-104: Implement credential list view" \
  --body "## Description
Build the credential list screen with filtering and search.

## Features
- List/grid view toggle
- Search functionality
- Filter by type
- Sort options
- Credential cards
- Status indicators
- Swipe actions

## Acceptance Criteria
- [ ] List view implemented
- [ ] Grid view implemented
- [ ] Search working
- [ ] Filters functional
- [ ] Sort options working
- [ ] Swipe to delete/share
- [ ] Pull to refresh

## Dependencies
Depends on POA-101 (UI designs)" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet,frontend,credentials"

gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-105: Create credential detail screen" \
  --body "## Description
Implement the detailed view for individual credentials.

## Features
- Full credential display
- QR code generation
- Attribute list
- Validity status
- Share options
- History log
- Actions menu

## Acceptance Criteria
- [ ] All attributes displayed
- [ ] QR code generated
- [ ] Share functionality
- [ ] History shown
- [ ] Actions working
- [ ] Status updated real-time
- [ ] Animations smooth

## Dependencies
Depends on POA-101 (UI designs)" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet,frontend,credentials"

gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-106: Build credential sharing UI" \
  --body "## Description
Implement selective disclosure and sharing interface.

## Features
- Attribute selection
- Consent screen
- QR code display
- NFC sharing
- Bluetooth sharing
- Share history
- Revoke access

## Acceptance Criteria
- [ ] Selective disclosure working
- [ ] Consent flow complete
- [ ] QR sharing functional
- [ ] NFC sharing working
- [ ] Bluetooth implemented
- [ ] History tracked
- [ ] Revocation working

## Dependencies
Depends on POA-101 (UI designs)" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet,frontend,sharing,privacy"

gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-107: Implement biometric authentication" \
  --body "## Description
Add biometric authentication for app access and transactions.

## Features
- Face ID support
- Touch ID support
- Android biometric API
- Fallback to PIN
- Security timeout
- Re-authentication flows

## Acceptance Criteria
- [ ] iOS Face ID working
- [ ] iOS Touch ID working
- [ ] Android biometric working
- [ ] PIN fallback functional
- [ ] Timeout implemented
- [ ] Re-auth flows complete
- [ ] Error handling robust

## Dependencies
Depends on POA-100 (architecture)" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet,security,biometric"

gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-108: Add push notification support" \
  --body "## Description
Implement push notifications for credential updates.

## Features
- FCM integration
- APNS integration
- Notification permissions
- In-app notifications
- Notification preferences
- Deep linking
- Badge updates

## Acceptance Criteria
- [ ] FCM configured
- [ ] APNS configured
- [ ] Permissions handled
- [ ] In-app display working
- [ ] Preferences saved
- [ ] Deep links working
- [ ] Badges updating

## Dependencies
Depends on POA-100 (architecture)" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet,notifications,integration"

gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-109: Implement offline mode" \
  --body "## Description
Enable wallet to function without internet connection.

## Features
- Local credential storage
- Offline verification
- Sync queue
- Conflict resolution
- Cache management
- Status indicators

## Acceptance Criteria
- [ ] Credentials cached locally
- [ ] Offline verification working
- [ ] Sync queue implemented
- [ ] Conflicts resolved
- [ ] Cache managed properly
- [ ] UI shows offline status
- [ ] Automatic sync on reconnect

## Dependencies
Depends on POA-100 (architecture)" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet,offline,critical-feature"

gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-110: Create wallet settings screen" \
  --body "## Description
Build comprehensive settings and preferences interface.

## Features
- Account settings
- Security settings
- Privacy controls
- Notification preferences
- Display preferences
- Backup options
- Help/support

## Acceptance Criteria
- [ ] All settings functional
- [ ] Preferences persisted
- [ ] Security options working
- [ ] Privacy controls active
- [ ] Backup configured
- [ ] Help accessible
- [ ] Changes applied immediately

## Dependencies
Depends on POA-101 (UI designs)" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet,frontend,settings"

gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-111: Add wallet backup/recovery" \
  --body "## Description
Implement secure backup and recovery mechanisms.

## Features
- Mnemonic phrase generation
- Secure backup
- Cloud backup option
- Recovery flow
- Migration support
- Export/import

## Acceptance Criteria
- [ ] Mnemonic phrase generated
- [ ] Backup encrypted
- [ ] Cloud backup optional
- [ ] Recovery tested
- [ ] Migration working
- [ ] Export/import functional
- [ ] Security validated

## Dependencies
Depends on POA-100 (architecture)" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet,security,backup"

gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-112: Implement wallet state management" \
  --body "## Description
Set up comprehensive state management for the wallet app.

## Components
- Bloc/Riverpod setup
- State models
- Event handling
- Side effects
- Persistence
- Testing utilities

## Acceptance Criteria
- [ ] State management configured
- [ ] All states modeled
- [ ] Events handled properly
- [ ] Side effects managed
- [ ] State persisted
- [ ] Tests written
- [ ] Documentation complete

## Dependencies
Depends on POA-100 (architecture)" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet,architecture,state-management"

gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-113: Create wallet test suite" \
  --body "## Description
Comprehensive test coverage for wallet application.

## Test Types
- Unit tests
- Widget tests
- Integration tests
- E2E tests
- Performance tests
- Security tests

## Acceptance Criteria
- [ ] 80% unit test coverage
- [ ] All widgets tested
- [ ] Integration tests passing
- [ ] E2E scenarios covered
- [ ] Performance validated
- [ ] Security verified
- [ ] CI/CD integrated

## Dependencies
Depends on all wallet features" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet,testing,quality"

echo ""
echo "Step 3: Reassigning issues to correct milestones..."

# Move authentication issues to the new 025-Week2-POA-AuthAPIs milestone
for issue in 48 49 50 51 52 55; do
  gh issue edit $issue --repo Credenxia/NumbatWallet --milestone "025-Week2-POA-AuthAPIs" 2>/dev/null || echo "Issue $issue not found"
done

# Ensure PKI issues are in correct milestone
for issue in 64 65 66 67 68 69 70; do
  gh issue edit $issue --repo Credenxia/NumbatWallet --milestone "002-PreDev-PKI" 2>/dev/null || echo "Issue $issue not found"
done

# Ensure standards issues are in correct milestone
for issue in 62 63; do
  gh issue edit $issue --repo Credenxia/NumbatWallet --milestone "001-PreDev-Standards" 2>/dev/null || echo "Issue $issue not found"
done

echo ""
echo "=== MILESTONE RESTRUCTURING COMPLETE ==="
echo ""
echo "Summary of changes:"
echo "1. ✅ Fixed duplicate prefix 004 (renamed to 025)"
echo "2. ✅ Created unique prefixes for all milestones"
echo "3. ✅ Adjusted dates to respect dependencies"
echo "4. ✅ Created 14 wallet issues (POA-100 to POA-113)"
echo "5. ✅ Reassigned issues to correct milestones"
echo ""
echo "New timeline:"
echo "- 000: Wallet App (Sept 16-20)"
echo "- 001: Standards (Sept 23-27)"
echo "- 002: PKI (Sept 23-27, parallel)"
echo "- 003: Infrastructure (Sept 25-30)"
echo "- 004: Integration (Sept 30-Oct 2)"
echo "- 010: Week 1 Deployment (Oct 3-4)"
echo "- 020: Week 2 Features (Oct 7-11)"
echo "- 025: Week 2 Auth APIs (Oct 7-11, parallel)"
echo "- 030: Week 3 Demo (Oct 14-18)"
echo "- 040: Week 4 Testing (Oct 21-25)"
echo "- 050: Week 5 Evaluation (Oct 28-Nov 1)"
echo ""
echo "Completed at $(date)"