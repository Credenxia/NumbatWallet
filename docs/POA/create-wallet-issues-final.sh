#!/bin/bash

echo "=== CREATING WALLET ISSUES #72-85 ==="
echo "Starting at $(date)"
echo ""

# Create all 14 wallet issues for complete application

# Issue #72: POA-100: Wallet Architecture
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-100: Design wallet application architecture" \
  --milestone "000-PreDev-WalletApp" \
  --body "## Description
Design the complete wallet application architecture following Clean Architecture principles and Flutter best practices.

## Requirements
- Define feature-based folder structure
- Select state management (Riverpod 2.0)
- Design data flow architecture
- Plan offline-first approach
- Define security architecture
- Create component hierarchy

## Technical Specifications
- Flutter 3.24+ with Dart 3.5+
- Riverpod for state management
- Dio for networking
- Hive for local storage
- flutter_secure_storage for sensitive data
- local_auth for biometrics

## Acceptance Criteria
- [ ] Architecture diagram created
- [ ] Folder structure defined
- [ ] State management patterns documented
- [ ] Security model defined
- [ ] Data flow documented
- [ ] Component hierarchy established

## Dependencies
- Blocks all other wallet features

## Timeline
- Start: Sept 16, 2025
- End: Sept 19, 2025"

# Issue #73: POA-101: UI/UX Design
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-101: Create UI/UX designs for wallet" \
  --milestone "000-PreDev-WalletApp" \
  --body "## Description
Design the complete UI/UX for the wallet application following Material Design 3 and iOS Human Interface Guidelines.

## Requirements
- Design system with components
- Color schemes (light/dark mode)
- Typography scales
- Icon set
- Screen layouts
- User flows

## Screens to Design
1. Splash screen
2. Onboarding flow (3-4 screens)
3. Biometric setup
4. PIN setup
5. Dashboard/Home
6. Credential list
7. Credential detail
8. Share credential
9. Settings
10. Backup/Recovery

## Acceptance Criteria
- [ ] Figma designs completed
- [ ] Design system documented
- [ ] User flows mapped
- [ ] Accessibility reviewed
- [ ] Dark mode designs
- [ ] Responsive layouts

## Timeline
- Start: Sept 16, 2025
- End: Sept 17, 2025"

# Issue #74: POA-102: Onboarding Screen
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-102: Implement onboarding flow" \
  --milestone "000-PreDev-WalletApp" \
  --body "## Description
Implement the complete onboarding flow for new users.

## Features
- Welcome screens
- Privacy policy acceptance
- Terms of service
- Biometric enrollment
- PIN setup as fallback
- Secure storage initialization

## Technical Requirements
- PageView for onboarding screens
- Animations between pages
- Progress indicators
- Skip option for returning users
- Local storage of completion state

## Acceptance Criteria
- [ ] 3-4 onboarding screens implemented
- [ ] Biometric setup integrated
- [ ] PIN fallback working
- [ ] Privacy/Terms acceptance tracked
- [ ] Onboarding completion persisted
- [ ] Tests written (>80% coverage)

## Timeline
- Start: Sept 17, 2025
- End: Sept 18, 2025"

# Issue #75: POA-103: Dashboard Screen
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-103: Build dashboard/home screen" \
  --milestone "000-PreDev-WalletApp" \
  --body "## Description
Implement the main dashboard screen showing credential summary and quick actions.

## Features
- Credential count summary
- Recent credentials carousel
- Quick action buttons
- Notifications badge
- Pull to refresh
- Empty state design

## Technical Requirements
- Riverpod state management
- Cached data display
- Smooth animations
- Responsive layout
- Performance optimized

## Acceptance Criteria
- [ ] Dashboard layout implemented
- [ ] Credential summary displayed
- [ ] Quick actions functional
- [ ] Pull to refresh working
- [ ] Empty state handled
- [ ] Tests written (>80% coverage)

## Dependencies
- Requires #72 (architecture)

## Timeline
- Start: Sept 17, 2025
- End: Sept 18, 2025"

# Issue #76: POA-104: Credential List
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-104: Create credential list view" \
  --milestone "000-PreDev-WalletApp" \
  --body "## Description
Implement the credential list screen with search, filter, and sort capabilities.

## Features
- List/Grid view toggle
- Search by name/issuer
- Filter by type/status
- Sort options
- Swipe actions
- Bulk selection mode

## Technical Requirements
- ListView.builder for performance
- Search with debouncing
- Animated list changes
- Lazy loading for large lists
- State preservation

## Acceptance Criteria
- [ ] List view implemented
- [ ] Grid view implemented
- [ ] Search functionality working
- [ ] Filter/sort options functional
- [ ] Swipe to delete/archive
- [ ] Tests written (>80% coverage)

## Timeline
- Start: Sept 18, 2025
- End: Sept 19, 2025"

# Issue #77: POA-105: Credential Detail
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-105: Build credential detail screen" \
  --milestone "000-PreDev-WalletApp" \
  --body "## Description
Implement the credential detail view with all attributes and actions.

## Features
- Full credential display
- Attribute list
- QR code generation
- Share options
- Verification status
- Expiry warnings
- Action buttons

## Technical Requirements
- QR code generation library
- Share functionality
- Animated transitions
- Pinch to zoom for QR
- Screenshot prevention

## Acceptance Criteria
- [ ] Detail view layout complete
- [ ] All attributes displayed
- [ ] QR code generated dynamically
- [ ] Share options working
- [ ] Status indicators functional
- [ ] Tests written (>80% coverage)

## Timeline
- Start: Sept 18, 2025
- End: Sept 19, 2025"

# Issue #78: POA-106: Share Credential
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-106: Implement credential sharing UI" \
  --milestone "000-PreDev-WalletApp" \
  --body "## Description
Implement selective disclosure and credential sharing interface.

## Features
- Attribute selection UI
- QR code presentation
- NFC sharing (Android)
- Bluetooth sharing
- Share history
- Consent management

## Technical Requirements
- Selective disclosure logic
- QR code with selected attributes
- NFC implementation (Android)
- Bluetooth LE support
- Audit logging

## Acceptance Criteria
- [ ] Attribute selection UI working
- [ ] QR code generation with selection
- [ ] NFC sharing functional (Android)
- [ ] Consent flow implemented
- [ ] Share history tracked
- [ ] Tests written (>80% coverage)

## Timeline
- Start: Sept 18, 2025
- End: Sept 19, 2025"

# Issue #79: POA-107: Biometric Auth
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-107: Add biometric authentication" \
  --milestone "000-PreDev-WalletApp" \
  --body "## Description
Implement biometric authentication for app access and sensitive operations.

## Features
- Face ID (iOS)
- Touch ID (iOS)
- Fingerprint (Android)
- Face unlock (Android)
- PIN fallback
- Biometric settings

## Technical Requirements
- local_auth package
- Secure storage integration
- Fallback mechanisms
- Error handling
- Device capability detection

## Acceptance Criteria
- [ ] Face ID working on iOS
- [ ] Touch ID working on iOS
- [ ] Fingerprint working on Android
- [ ] PIN fallback implemented
- [ ] Settings UI complete
- [ ] Tests written (>80% coverage)

## Timeline
- Start: Sept 17, 2025
- End: Sept 18, 2025"

# Issue #80: POA-108: Push Notifications
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-108: Setup push notifications" \
  --milestone "000-PreDev-WalletApp" \
  --body "## Description
Implement push notifications for credential updates and alerts.

## Features
- FCM integration (Android)
- APNS integration (iOS)
- In-app notifications
- Notification settings
- Deep linking
- Badge updates

## Technical Requirements
- firebase_messaging package
- Local notifications
- Background handling
- Permission management
- Token management

## Acceptance Criteria
- [ ] FCM configured for Android
- [ ] APNS configured for iOS
- [ ] Notifications received in foreground
- [ ] Background handling working
- [ ] Deep links functional
- [ ] Tests written (>80% coverage)

## Timeline
- Start: Sept 18, 2025
- End: Sept 19, 2025"

# Issue #81: POA-109: Offline Mode
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-109: Implement offline mode" \
  --milestone "000-PreDev-WalletApp" \
  --body "## Description
Implement complete offline functionality for credential storage and verification.

## Features
- Local credential storage
- Offline verification
- Sync queue for online
- Conflict resolution
- Cache management
- Storage encryption

## Technical Requirements
- Hive for local database
- Encryption for stored data
- Sync mechanism
- Queue for pending operations
- Cache invalidation strategy

## Acceptance Criteria
- [ ] Credentials stored locally
- [ ] Offline verification working
- [ ] Sync queue implemented
- [ ] Encryption enabled
- [ ] Cache management functional
- [ ] Tests written (>80% coverage)

## Dependencies
- Critical for tender requirement

## Timeline
- Start: Sept 19, 2025
- End: Sept 19, 2025"

# Issue #82: POA-110: Settings Screen
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-110: Create settings screen" \
  --milestone "000-PreDev-WalletApp" \
  --body "## Description
Implement the settings screen with all configuration options.

## Features
- Profile management
- Security settings
- Notification preferences
- Theme selection
- Language selection
- About section
- Help/Support
- Privacy policy
- Terms of service

## Technical Requirements
- Preferences storage
- Theme switching
- Language switching
- Settings export/import
- Version information

## Acceptance Criteria
- [ ] Settings UI implemented
- [ ] All preferences functional
- [ ] Theme switching working
- [ ] Settings persisted
- [ ] About section complete
- [ ] Tests written (>80% coverage)

## Timeline
- Start: Sept 19, 2025
- End: Sept 19, 2025"

# Issue #83: POA-111: Backup/Recovery
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-111: Add backup and recovery" \
  --milestone "000-PreDev-WalletApp" \
  --body "## Description
Implement backup and recovery mechanisms for wallet data.

## Features
- Encrypted backup
- Cloud backup (optional)
- Recovery phrase
- Backup reminders
- Recovery flow
- Data migration

## Technical Requirements
- Encryption for backups
- Mnemonic phrase generation
- iCloud/Google Drive integration
- Import/export functionality
- Version compatibility

## Acceptance Criteria
- [ ] Backup creation working
- [ ] Encryption implemented
- [ ] Recovery phrase generated
- [ ] Recovery flow functional
- [ ] Cloud backup optional
- [ ] Tests written (>80% coverage)

## Timeline
- Start: Sept 18, 2025
- End: Sept 19, 2025"

# Issue #84: POA-112: State Management
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-112: Setup state management" \
  --milestone "000-PreDev-WalletApp" \
  --body "## Description
Implement Riverpod 2.0 state management architecture for the entire application.

## Features
- Provider setup
- State models
- Repository pattern
- Service layer
- Error handling
- Loading states

## Technical Requirements
- Riverpod 2.0
- Code generation setup
- State persistence
- Error boundaries
- Performance optimization

## Acceptance Criteria
- [ ] Riverpod configured
- [ ] All providers created
- [ ] State models defined
- [ ] Repository pattern implemented
- [ ] Error handling complete
- [ ] Tests written (>80% coverage)

## Dependencies
- Required by all screens

## Timeline
- Start: Sept 16, 2025
- End: Sept 17, 2025"

# Issue #85: POA-113: Test Suite
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-113: Create comprehensive test suite" \
  --milestone "000-PreDev-WalletApp" \
  --body "## Description
Create comprehensive test suite for the wallet application achieving >80% coverage.

## Test Types
- Unit tests
- Widget tests
- Integration tests
- Golden tests
- Performance tests

## Coverage Requirements
- Business logic: 90%
- UI components: 80%
- State management: 85%
- Services: 85%
- Overall: >80%

## Technical Requirements
- flutter_test
- mockito for mocking
- golden_toolkit
- integration_test package
- coverage reporting

## Acceptance Criteria
- [ ] Unit tests complete
- [ ] Widget tests complete
- [ ] Integration tests complete
- [ ] Golden tests for UI
- [ ] Coverage >80%
- [ ] CI/CD integration

## Timeline
- Start: Sept 19, 2025
- End: Sept 19, 2025"

echo ""
echo "=== WALLET ISSUES CREATED ==="
echo "Created 14 issues (#72-85) for complete wallet application"
echo "All assigned to 000-PreDev-WalletApp milestone"
echo "Timeline: Sept 16-19, 2025"
echo ""
echo "Completed at $(date)"