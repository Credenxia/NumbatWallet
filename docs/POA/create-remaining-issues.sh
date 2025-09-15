#!/bin/bash

echo "Creating remaining critical issues..."

# PKI & Security Issues (POA-125 to POA-132)
echo "Creating PKI and security issues..."

# POA-125
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-125: Set up IACA root certificates" \
  --body "## Description
Set up Issuer Authority Certificate Authorities (IACA) root certificates for the PKI infrastructure as required by ISO 18013-5.

## Acceptance Criteria
- [ ] Generate IACA root certificate
- [ ] Configure certificate chain
- [ ] Implement certificate validation
- [ ] Set up certificate store
- [ ] Configure trust anchors
- [ ] Certificate rotation plan

## Technical Requirements
- X.509 v3 certificates
- RSA 4096 or ECC P-256
- SHA-256 minimum
- HSM integration

## Definition of Done
- [ ] IACA certificates generated
- [ ] Chain validation working
- [ ] HSM configured
- [ ] Security audit passed" \
  --milestone "000-PreDev-PKI" \
  --label "pki,security"

# POA-126
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-126: Implement Document Signing Certificates" \
  --body "## Description
Implement Document Signing Certificates (DSC) for signing credentials according to ISO 18013-5 requirements.

## Acceptance Criteria
- [ ] Generate DSCs from IACA
- [ ] Implement signing operations
- [ ] Certificate lifecycle management
- [ ] Revocation handling
- [ ] Key rotation procedures

## Technical Requirements
- Subordinate to IACA
- COSE signing support
- Key storage in HSM
- Audit logging

## Definition of Done
- [ ] DSCs operational
- [ ] Signing working
- [ ] Lifecycle managed
- [ ] Tests passing" \
  --milestone "000-PreDev-PKI" \
  --label "pki,security"

# POA-127
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-127: Create trust list management" \
  --body "## Description
Create trust list management system for maintaining trusted issuers, verifiers, and certificate authorities.

## Acceptance Criteria
- [ ] Trust list data model
- [ ] CRUD operations for entries
- [ ] Trust list distribution
- [ ] Validation mechanisms
- [ ] Update procedures
- [ ] Caching strategy

## Technical Requirements
- Signed trust lists
- Version control
- Distribution API
- Cache invalidation

## Definition of Done
- [ ] Trust lists operational
- [ ] API implemented
- [ ] Distribution working
- [ ] Documentation complete" \
  --milestone "000-PreDev-PKI" \
  --label "pki,security"

# POA-128
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-128: Implement HSM integration" \
  --body "## Description
Integrate Hardware Security Module (HSM) for secure key storage and cryptographic operations.

## Acceptance Criteria
- [ ] HSM configuration
- [ ] Key generation in HSM
- [ ] Signing operations via HSM
- [ ] Key backup procedures
- [ ] Disaster recovery plan
- [ ] Performance optimization

## Technical Requirements
- Azure Key Vault HSM or equivalent
- PKCS#11 interface
- High availability setup
- Monitoring and alerts

## Definition of Done
- [ ] HSM integrated
- [ ] Keys migrated
- [ ] Operations tested
- [ ] DR plan validated" \
  --milestone "000-PreDev-PKI" \
  --label "pki,security,infrastructure"

# Operations & Admin Issues (POA-133 to POA-137)
echo "Creating operations and admin issues..."

# POA-133
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-133: Build admin dashboard UI" \
  --body "## Description
Build comprehensive admin dashboard for managing credentials, users, and system operations.

## Acceptance Criteria
- [ ] Dashboard layout and navigation
- [ ] Authentication and authorization
- [ ] Role-based access control
- [ ] Responsive design
- [ ] Real-time updates
- [ ] Dark mode support

## Dashboard Sections
- [ ] Overview/metrics
- [ ] Credential management
- [ ] User management
- [ ] Audit logs
- [ ] System settings
- [ ] Reports

## Technical Requirements
- React/Vue/Angular
- GraphQL integration
- WebSocket for real-time
- WCAG 2.2 compliance

## Definition of Done
- [ ] UI implemented
- [ ] Authentication working
- [ ] RBAC functional
- [ ] Tests complete" \
  --milestone "005-Week4-Demo" \
  --label "admin,ui"

# POA-134
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-134: Implement credential CRUD operations" \
  --body "## Description
Implement complete CRUD operations for credential management in the admin portal.

## Acceptance Criteria
- [ ] Create new credentials
- [ ] Read/view credentials
- [ ] Update credential attributes
- [ ] Delete/revoke credentials
- [ ] Bulk operations
- [ ] Search and filter

## Operations
- [ ] Issue credential
- [ ] View details
- [ ] Update status
- [ ] Revoke credential
- [ ] Batch operations
- [ ] Export data

## Technical Requirements
- RESTful API
- GraphQL mutations
- Audit logging
- Transaction support

## Definition of Done
- [ ] All CRUD operations working
- [ ] Bulk operations functional
- [ ] Audit trail complete
- [ ] Tests passing" \
  --milestone "005-Week4-Demo" \
  --label "admin,backend"

# POA-135
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-135: Create audit log viewer" \
  --body "## Description
Create comprehensive audit log viewer for tracking all system operations and credential activities.

## Acceptance Criteria
- [ ] Log display interface
- [ ] Search and filter
- [ ] Export capabilities
- [ ] Real-time updates
- [ ] Log retention settings
- [ ] Compliance reports

## Log Categories
- [ ] Credential operations
- [ ] User activities
- [ ] System events
- [ ] Security events
- [ ] API calls

## Technical Requirements
- Structured logging
- Log aggregation
- Search capabilities
- Export formats (CSV, JSON)

## Definition of Done
- [ ] Viewer implemented
- [ ] Search working
- [ ] Export functional
- [ ] Performance optimized" \
  --milestone "005-Week4-Demo" \
  --label "admin,monitoring"

# Feature Implementation Issues (POA-138 to POA-149)
echo "Creating feature implementation issues..."

# POA-138
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-138: Implement offline verification" \
  --body "## Description
Implement offline verification capability allowing credentials to be verified without internet connectivity.

## Acceptance Criteria
- [ ] Offline mode detection
- [ ] Local verification logic
- [ ] Cryptographic validation
- [ ] Certificate caching
- [ ] Offline QR codes
- [ ] Status checking

## Technical Requirements
- Local crypto libraries
- Certificate bundle caching
- Offline-capable QR format
- Signature verification

## Test Scenarios
1. Airplane mode verification
2. Cached certificate validation
3. Expired cache handling
4. Signature verification

## Definition of Done
- [ ] Offline mode working
- [ ] Verification successful
- [ ] Tests in airplane mode
- [ ] Documentation complete" \
  --milestone "005-Week4-Demo" \
  --label "features,offline"

# POA-139
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-139: Add QR code generation/scanning" \
  --body "## Description
Implement QR code generation for credential sharing and scanning for verification.

## Acceptance Criteria
- [ ] QR code generation
- [ ] Dynamic QR content
- [ ] QR scanner implementation
- [ ] Error correction levels
- [ ] Size optimization
- [ ] Offline QR support

## Technical Requirements
- QR library integration
- Camera permissions
- Image processing
- Data compression

## Test Scenarios
1. Generate QR code
2. Scan QR code
3. Handle damaged QR
4. Large data handling

## Definition of Done
- [ ] Generation working
- [ ] Scanning functional
- [ ] Error handling complete
- [ ] Performance optimized" \
  --milestone "005-Week4-Demo" \
  --label "features,qr"

# POA-140
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-140: Implement NFC credential sharing" \
  --body "## Description
Implement NFC (Near Field Communication) capability for tap-to-share credential presentation.

## Acceptance Criteria
- [ ] NFC detection
- [ ] Data transmission
- [ ] Security protocols
- [ ] Error handling
- [ ] Platform support
- [ ] User feedback

## Technical Requirements
- NFC API integration
- NDEF message format
- Encryption support
- iOS and Android

## Test Scenarios
1. NFC tap to share
2. Connection interruption
3. Data integrity check
4. Cross-platform test

## Definition of Done
- [ ] NFC working on Android
- [ ] NFC working on iOS
- [ ] Security validated
- [ ] Tests complete" \
  --milestone "005-Week4-Demo" \
  --label "features,nfc"

# POA-142
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-142: Create credential revocation flow" \
  --body "## Description
Implement complete credential revocation flow including admin interface and wallet updates.

## Acceptance Criteria
- [ ] Revocation API endpoint
- [ ] Admin revocation UI
- [ ] Wallet notification
- [ ] Status updates
- [ ] Revocation reasons
- [ ] Audit logging

## Technical Requirements
- Revocation registry
- Push notifications
- Status propagation
- Immediate effect

## Test Scenarios
1. Admin revokes credential
2. User receives notification
3. Verification fails
4. Status check works

## Definition of Done
- [ ] Revocation working
- [ ] Notifications sent
- [ ] Status updated
- [ ] Tests passing" \
  --milestone "003-Week2-Backend" \
  --label "features,backend"

# POA-145
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-145: Create data minimization controls" \
  --body "## Description
Implement data minimization controls ensuring only necessary data is collected and shared.

## Acceptance Criteria
- [ ] Minimal data collection
- [ ] Purpose limitation
- [ ] Consent management
- [ ] Data retention policies
- [ ] Deletion capabilities
- [ ] Privacy dashboard

## Technical Requirements
- Privacy by design
- GDPR compliance
- Consent tracking
- Data lifecycle management

## Test Scenarios
1. Minimal attribute sharing
2. Consent verification
3. Data deletion
4. Retention policies

## Definition of Done
- [ ] Controls implemented
- [ ] Privacy validated
- [ ] GDPR compliant
- [ ] Tests complete" \
  --milestone "004-Week3-AuthAPIs" \
  --label "features,privacy"

# POA-147
gh issue create \
  --repo Credenxia/NumbatWallet \
  --title "POA-147: Add transaction history" \
  --body "## Description
Implement transaction history showing all credential operations and sharing activities.

## Acceptance Criteria
- [ ] History data model
- [ ] History API
- [ ] UI display
- [ ] Search/filter
- [ ] Export capability
- [ ] Privacy controls

## History Items
- [ ] Issuance events
- [ ] Sharing events
- [ ] Verification events
- [ ] Updates/revocations

## Technical Requirements
- Event sourcing
- Time-series data
- Efficient querying
- Data retention

## Definition of Done
- [ ] History tracking working
- [ ] UI implemented
- [ ] Search functional
- [ ] Privacy preserved" \
  --milestone "004-Week3-AuthAPIs" \
  --label "features,audit"

echo "All critical missing issues created successfully!"

# Summary
echo ""
echo "=== ISSUE CREATION SUMMARY ==="
echo "✅ Wallet App Issues: POA-100 to POA-109 (10 issues)"
echo "✅ Standards Issues: POA-110 to POA-124 (15 issues)"
echo "✅ PKI Issues: POA-125 to POA-128 (4 issues)"
echo "✅ Admin Issues: POA-133 to POA-135 (3 issues)"
echo "✅ Feature Issues: POA-138, 139, 140, 142, 145, 147 (6 issues)"
echo ""
echo "Total new issues created: 38"
echo ""
echo "Note: Some issues from the original plan were already covered by existing issues"
echo "or merged with related issues for efficiency."