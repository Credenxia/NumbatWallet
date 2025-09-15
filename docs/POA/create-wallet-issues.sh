#!/bin/bash

# Wallet Application Issues (POA-100 to POA-109)

echo "Creating wallet application issues..."

# POA-100
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-100: Create proprietary wallet application architecture" \
  --body "## Description
As a system architect, I want to design and implement the core wallet application architecture so that we have a robust foundation for all wallet features.

## Acceptance Criteria
- [ ] Clean Architecture/Onion Architecture implemented
- [ ] Domain-driven design principles applied
- [ ] Dependency injection configured
- [ ] State management solution implemented (Bloc/Riverpod)
- [ ] Navigation framework established
- [ ] Error handling strategy implemented
- [ ] Logging framework integrated

## Technical Requirements
- Framework: Flutter 3.x
- State Management: Bloc or Riverpod
- DI: get_it or injectable
- Architecture: Clean Architecture with feature-first organization

## Dependencies
- Blocks: All other wallet issues
- Required for: POA demonstration

## Test Scenarios
1. Verify layer separation (domain, data, presentation)
2. Test dependency injection
3. Validate state management
4. Check error propagation

## Definition of Done
- [ ] Architecture documented
- [ ] Code structure created
- [ ] Unit tests for core components (>90% coverage)
- [ ] CI/CD pipeline configured
- [ ] Code review completed" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet-app,architecture,critical-path,poa-demo"

# POA-101
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-101: Design wallet UI/UX mockups and user flows" \
  --body "## Description
As a UX designer, I want to create comprehensive UI/UX designs for the wallet application so that users have an intuitive and accessible experience.

## Acceptance Criteria
- [ ] Complete user journey maps created
- [ ] High-fidelity mockups for all screens
- [ ] Design system/component library defined
- [ ] Accessibility requirements documented (WCAG 2.2 AA)
- [ ] Dark mode designs included
- [ ] Responsive layouts for various screen sizes

## Screens to Design
- [ ] Splash/Loading screen
- [ ] Onboarding flow (3-5 screens)
- [ ] Login/Authentication screen
- [ ] Wallet home/Dashboard
- [ ] Credential list view
- [ ] Credential detail view
- [ ] Add credential flow
- [ ] Share/Present credential flow
- [ ] Settings screen
- [ ] Profile management
- [ ] Security settings
- [ ] Help/Support screen

## Technical Requirements
- Design Tool: Figma
- Export: Design tokens, SVG assets
- Handoff: Zeplin or Figma Dev Mode

## Dependencies
- Blocks: POA-102 to POA-109
- Input needed: Brand guidelines from DGov

## Definition of Done
- [ ] All mockups completed in Figma
- [ ] Design system documented
- [ ] Stakeholder approval received
- [ ] Developer handoff completed
- [ ] Accessibility audit passed" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet-app,design,ux,critical-path"

# POA-102
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-102: Implement wallet onboarding and setup flow" \
  --body "## Description
As a new user, I want to complete a simple onboarding process so that I can set up my digital wallet securely and understand its features.

## Acceptance Criteria
- [ ] Welcome screen with app introduction
- [ ] Privacy policy and terms acceptance
- [ ] Identity verification integration (WA IdX)
- [ ] Biometric authentication setup
- [ ] PIN/Passcode backup option
- [ ] Tutorial/feature highlights
- [ ] Successful completion confirmation

## Technical Requirements
- Biometric: local_auth package
- Secure storage: flutter_secure_storage
- Analytics: Track onboarding completion rate

## User Flow
1. Welcome screen ->
2. Terms & Privacy ->
3. Identity verification ->
4. Set up security (biometric/PIN) ->
5. Feature tutorial ->
6. Complete

## Dependencies
- Depends on: POA-100, POA-101
- Blocks: POA-103

## Test Scenarios
1. First-time user completes onboarding
2. User skips optional steps
3. Biometric setup failure handling
4. Return user detection

## Definition of Done
- [ ] All screens implemented
- [ ] Biometric authentication working
- [ ] Identity verification integrated
- [ ] Unit tests (>90% coverage)
- [ ] E2E tests for complete flow
- [ ] Accessibility tested" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet-app,onboarding,critical-path,poa-demo"

# POA-103
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-103: Build credential wallet home screen" \
  --body "## Description
As a wallet user, I want to see all my credentials at a glance on the home screen so that I can quickly access and manage them.

## Acceptance Criteria
- [ ] Display list of credentials with preview cards
- [ ] Show credential status (active, expired, revoked)
- [ ] Quick actions (share, details, delete)
- [ ] Search/filter functionality
- [ ] Pull-to-refresh for updates
- [ ] Empty state for no credentials
- [ ] Add credential CTA button

## UI Components
- [ ] Credential card widget
- [ ] Status indicator badges
- [ ] Search bar
- [ ] Filter chips
- [ ] Floating action button
- [ ] Bottom navigation (if applicable)

## Technical Requirements
- State management for credential list
- Local caching of credentials
- Real-time update subscriptions
- Smooth animations/transitions

## Dependencies
- Depends on: POA-100, POA-101, POA-102
- Blocks: POA-104, POA-105

## Test Scenarios
1. Display multiple credentials
2. Handle empty state
3. Search credentials by name
4. Filter by credential type
5. Pull-to-refresh updates

## Definition of Done
- [ ] Home screen fully functional
- [ ] All UI components implemented
- [ ] State management working
- [ ] Unit tests (>90% coverage)
- [ ] UI tests for interactions
- [ ] Performance optimized (<16ms frame time)" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet-app,ui,critical-path,poa-demo"

# POA-104
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-104: Create credential detail view with actions" \
  --body "## Description
As a wallet user, I want to view detailed information about my credentials and perform actions on them so that I can manage and use them effectively.

## Acceptance Criteria
- [ ] Display all credential fields and metadata
- [ ] Show credential image/photo if applicable
- [ ] Display QR code for sharing
- [ ] Action buttons (Share, Verify, Delete)
- [ ] Credential history/audit log
- [ ] Expiry date and status prominently shown
- [ ] Back navigation to home

## UI Components
- [ ] Credential info cards
- [ ] QR code generator
- [ ] Action sheet/bottom sheet
- [ ] Timeline/history view
- [ ] Status banner
- [ ] Share dialog

## Technical Requirements
- QR code generation library
- Share functionality (platform share sheets)
- Secure credential data handling
- Audit log integration

## Dependencies
- Depends on: POA-103
- Blocks: POA-105

## Test Scenarios
1. View driver license details
2. Generate QR code for sharing
3. Delete credential with confirmation
4. View credential history
5. Handle expired credentials

## Definition of Done
- [ ] Detail view fully implemented
- [ ] All credential types supported
- [ ] QR code generation working
- [ ] Share functionality tested
- [ ] Unit tests (>90% coverage)
- [ ] UI tests completed" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet-app,ui,features,poa-demo"

# POA-105
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-105: Implement credential sharing/presentation flow" \
  --body "## Description
As a wallet user, I want to share my credentials with verifiers securely so that I can prove my identity or attributes when needed.

## Acceptance Criteria
- [ ] QR code generation for offline sharing
- [ ] NFC tap-to-share capability
- [ ] Bluetooth proximity sharing
- [ ] Selective disclosure interface
- [ ] Consent screen before sharing
- [ ] Verification result display
- [ ] Sharing history log

## Sharing Methods
- [ ] QR code display
- [ ] NFC transmission
- [ ] Bluetooth LE
- [ ] Deep link sharing
- [ ] Platform share sheet

## Technical Requirements
- QR: qr_flutter package
- NFC: nfc_manager package
- Bluetooth: flutter_blue_plus
- Selective disclosure UI components

## Dependencies
- Depends on: POA-104
- Related to: POA-138 (offline verification)

## Test Scenarios
1. Share via QR code
2. Selective disclosure of attributes
3. NFC tap-to-share
4. Bluetooth proximity sharing
5. Consent flow validation

## Definition of Done
- [ ] All sharing methods implemented
- [ ] Selective disclosure working
- [ ] Consent flow complete
- [ ] Unit tests (>90% coverage)
- [ ] Integration tests with verifier
- [ ] Security review completed" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet-app,sharing,critical-path,poa-demo"

# POA-106
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-106: Add biometric authentication to wallet" \
  --body "## Description
As a wallet user, I want to use biometric authentication to access my wallet and authorize sensitive operations so that my credentials are secure.

## Acceptance Criteria
- [ ] Face ID support (iOS)
- [ ] Touch ID support (iOS)
- [ ] Fingerprint support (Android)
- [ ] Face unlock support (Android)
- [ ] PIN/Passcode fallback
- [ ] Biometric binding to credentials
- [ ] Re-authentication for sensitive operations

## Security Requirements
- [ ] Biometric data never leaves device
- [ ] Secure enclave/TEE utilization
- [ ] Cryptographic binding to device
- [ ] Anti-tampering measures
- [ ] Rate limiting on attempts

## Technical Requirements
- Package: local_auth
- Secure storage: flutter_secure_storage
- Platform-specific implementations

## Dependencies
- Depends on: POA-102
- Critical for: Security compliance

## Test Scenarios
1. Successful biometric authentication
2. Failed biometric with PIN fallback
3. Biometric not available handling
4. Re-authentication for sharing
5. Multiple failure lockout

## Definition of Done
- [ ] All biometric types supported
- [ ] Fallback mechanisms working
- [ ] Security requirements met
- [ ] Unit tests (>90% coverage)
- [ ] Security audit passed
- [ ] Platform-specific testing completed" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet-app,security,biometric,critical-path"

# POA-107
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-107: Build wallet settings and profile screens" \
  --body "## Description
As a wallet user, I want to manage my wallet settings and profile information so that I can customize my experience and maintain my account.

## Acceptance Criteria
- [ ] Profile information display/edit
- [ ] Security settings (biometric, PIN)
- [ ] Privacy settings
- [ ] Notification preferences
- [ ] Language selection
- [ ] Theme selection (light/dark/system)
- [ ] Help and support links
- [ ] About/Version information
- [ ] Logout functionality

## Settings Categories
- [ ] Account/Profile
- [ ] Security & Privacy
- [ ] Notifications
- [ ] Appearance
- [ ] Help & Support
- [ ] Legal (Terms, Privacy Policy)

## Technical Requirements
- Shared preferences for settings
- Theme management
- Localization support
- Deep linking to OS settings

## Dependencies
- Depends on: POA-103
- Related to: POA-106 (biometric settings)

## Test Scenarios
1. Update profile information
2. Change security settings
3. Toggle notifications
4. Switch themes
5. Change language

## Definition of Done
- [ ] All settings screens implemented
- [ ] Settings persistence working
- [ ] Theme switching functional
- [ ] Unit tests (>90% coverage)
- [ ] UI tests for settings flow
- [ ] Accessibility verified" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet-app,settings,ui"

# POA-108
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-108: Implement wallet backup and recovery" \
  --body "## Description
As a wallet user, I want to backup my wallet and recover it on a new device so that I don't lose access to my credentials if I lose my phone.

## Acceptance Criteria
- [ ] Secure backup generation
- [ ] Recovery phrase/seed generation
- [ ] Cloud backup option (optional)
- [ ] Recovery flow implementation
- [ ] Device migration support
- [ ] Backup encryption
- [ ] Recovery verification

## Backup Methods
- [ ] Recovery phrase (12/24 words)
- [ ] Encrypted file export
- [ ] Cloud backup (iCloud/Google Drive)
- [ ] QR code backup

## Technical Requirements
- Cryptographic key derivation
- Secure storage of backup
- End-to-end encryption
- Platform-specific cloud integration

## Dependencies
- Depends on: POA-100
- Critical for: Production readiness

## Test Scenarios
1. Generate backup phrase
2. Recover wallet with phrase
3. Cloud backup and restore
4. Cross-device migration
5. Invalid recovery attempt

## Definition of Done
- [ ] Backup generation working
- [ ] Recovery flow complete
- [ ] Encryption implemented
- [ ] Unit tests (>90% coverage)
- [ ] Security review completed
- [ ] Recovery tested on new device" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet-app,backup,security"

# POA-109
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-109: Add multi-device sync capabilities" \
  --body "## Description
As a wallet user, I want to access my credentials from multiple devices so that I can use whichever device is convenient.

## Acceptance Criteria
- [ ] Device registration/management
- [ ] Secure sync protocol
- [ ] Conflict resolution
- [ ] Device revocation
- [ ] Sync status indicator
- [ ] Selective sync options
- [ ] Offline capability

## Sync Features
- [ ] Real-time credential sync
- [ ] Settings sync
- [ ] History sync
- [ ] Device management UI
- [ ] Sync conflict resolution

## Technical Requirements
- End-to-end encryption
- WebSocket/REST sync protocol
- Conflict resolution strategy
- Device attestation

## Dependencies
- Depends on: POA-108
- Requires: Backend sync service

## Test Scenarios
1. Add second device
2. Sync credentials between devices
3. Revoke device access
4. Handle sync conflicts
5. Offline/online transitions

## Definition of Done
- [ ] Multi-device sync working
- [ ] Device management UI complete
- [ ] Encryption implemented
- [ ] Unit tests (>90% coverage)
- [ ] Integration tests with backend
- [ ] Performance tested" \
  --milestone "000-PreDev-WalletApp" \
  --label "wallet-app,sync,features"

echo "Wallet application issues created successfully!"