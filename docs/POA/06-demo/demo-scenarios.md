# POA Demo Scenarios Guide

**Version:** 1.0  
**Date:** September 10, 2025  
**Phase:** Proof-of-Operation (POA)

## Table of Contents
- [Overview](#overview)
- [Demo Environment](#demo-environment)
- [Demo Personas](#demo-personas)
- [Core Scenarios](#core-scenarios)
- [Advanced Scenarios](#advanced-scenarios)
- [Demo Script](#demo-script)
- [Troubleshooting](#troubleshooting)
- [Demo Checklist](#demo-checklist)

## Overview

This document provides comprehensive demo scenarios for the POA evaluation, designed to showcase all key capabilities of the NumbatWallet solution to WA Government stakeholders.

### Demo Objectives
- **Prove Capability** - Demonstrate all required features
- **Show Integration** - ServiceWA seamless experience
- **Validate Security** - Privacy and security controls
- **Confirm Performance** - Real-time operations
- **Build Confidence** - Production readiness

### Demo Duration
- **Quick Demo**: 15 minutes (core features)
- **Standard Demo**: 30 minutes (full workflow)
- **Deep Dive**: 60 minutes (technical details)
- **Workshop**: 2 hours (hands-on experience)

## Demo Environment

### Environment Setup

```yaml
Demo Infrastructure:
  URL: https://demo.wallet.wa.gov.au
  API: https://api-demo.wallet.wa.gov.au
  Admin: https://admin-demo.wallet.wa.gov.au
  
Test Accounts:
  - Admin:
      email: admin@demo.wa.gov.au
      password: Demo2025!Admin
      mfa: DEMO-TOTP-SEED
  
  - Issuer (Transport):
      email: issuer.transport@demo.wa.gov.au
      password: Demo2025!Issuer
  
  - Issuer (Health):
      email: issuer.health@demo.wa.gov.au
      password: Demo2025!Health
  
  - Citizen:
      email: john.citizen@demo.wa.gov.au
      password: Demo2025!User
      
  - Verifier:
      email: verifier@demo.wa.gov.au
      password: Demo2025!Verify

Demo Devices:
  - iPhone 14 Pro (iOS 17)
  - Samsung Galaxy S23 (Android 14)
  - iPad Pro (iPadOS 17)
  - Windows Laptop (Chrome/Edge)
```

### Pre-loaded Data

```json
{
  "tenants": [
    {
      "id": "transport-wa",
      "name": "Department of Transport",
      "credentials": ["drivers-license", "vehicle-registration"]
    },
    {
      "id": "health-wa",
      "name": "Department of Health",
      "credentials": ["vaccination-certificate", "health-care-card"]
    }
  ],
  "users": [
    {
      "name": "John Citizen",
      "credentials": [
        {
          "type": "drivers-license",
          "status": "active",
          "issued": "2024-01-15"
        },
        {
          "type": "proof-of-age",
          "status": "active",
          "issued": "2024-06-01"
        }
      ]
    },
    {
      "name": "Jane Smith",
      "credentials": [
        {
          "type": "working-with-children",
          "status": "active",
          "issued": "2024-03-20"
        }
      ]
    }
  ]
}
```

## Demo Personas

### 1. John Citizen - Primary User
- **Role**: WA resident
- **Age**: 35
- **Credentials**: Driver's License, Proof of Age
- **Use Case**: Daily credential usage
- **Device**: iPhone 14 Pro with ServiceWA app

### 2. Sarah Manager - Government Officer
- **Role**: Department of Transport officer
- **Responsibility**: Issue driver licenses
- **Access**: Admin portal, credential issuance
- **Device**: Windows laptop

### 3. Mike Checker - Venue Operator
- **Role**: Nightclub security
- **Responsibility**: Age verification
- **Tool**: Verification app on Android tablet
- **Requirement**: Quick, reliable verification

### 4. Dr. Emma Health - Healthcare Provider
- **Role**: GP at medical center
- **Responsibility**: Verify healthcare credentials
- **Access**: Professional verification portal
- **Requirement**: Detailed credential validation

## Core Scenarios

### Scenario 1: Credential Issuance Flow

**Duration**: 5 minutes  
**Persona**: Sarah Manager + John Citizen

```markdown
## Steps:

1. **Admin Portal Login** (Sarah)
   - Navigate to https://admin-demo.wallet.wa.gov.au
   - Login with Transport issuer credentials
   - Show MFA authentication
   - Display dashboard with statistics

2. **Search for Citizen**
   - Search "John Citizen" in user database
   - Show existing records and eligibility
   - Verify identity requirements met

3. **Issue Digital Driver License**
   - Click "Issue Credential"
   - Select "Driver License" template
   - Review pre-filled information:
     - License number: WA123456
     - Class: C
     - Expiry: 2029-01-15
   - Add restrictions: "S - Corrective lenses"
   - Click "Issue and Sign"

4. **Digital Signature**
   - Show PKI certificate selection
   - Display signing process
   - Generate cryptographic proof
   - Create audit log entry

5. **Citizen Receives Credential** (John)
   - Open ServiceWA app
   - Show push notification
   - Navigate to Wallet section
   - See new Driver License
   - Animate credential appearing

## Key Points to Emphasize:
- Real-time issuance
- Cryptographic security
- Audit trail
- User experience
```

### Scenario 2: Verification Flow - Age Check

**Duration**: 3 minutes  
**Persona**: John Citizen + Mike Checker

```markdown
## Steps:

1. **Initiate Verification** (John)
   - Open ServiceWA app
   - Navigate to Wallet
   - Select Driver License
   - Tap "Share" button
   - Choose "Proof of Age Only"

2. **Generate QR Code**
   - Show selective disclosure options
   - Hide address, license number
   - Only share: Name, Photo, DOB, Over 18
   - Generate time-limited QR (60 seconds)
   - Display QR code

3. **Scan and Verify** (Mike)
   - Open Verifier app
   - Tap "Scan QR"
   - Point at John's phone
   - Instant verification

4. **Verification Result**
   - Green checkmark animation
   - Display: "Valid - Over 18"
   - Show: John Citizen (name only)
   - Display photo for visual check
   - No personal details retained

5. **Offline Verification Demo**
   - Enable airplane mode
   - Repeat verification
   - Show it still works
   - Explain cryptographic validation

## Key Points:
- Privacy preservation
- Selective disclosure
- Offline capability
- No data retention
```

### Scenario 3: Multi-Credential Presentation

**Duration**: 4 minutes  
**Persona**: Jane Smith

```markdown
## Steps:

1. **Employment Verification Scenario**
   - Jane applying for childcare position
   - Needs: WWC Check + First Aid + ID

2. **Create Credential Bundle**
   - Select multiple credentials
   - Working with Children Check
   - First Aid Certificate
   - Proof of Identity
   - Create combined presentation

3. **Share via Secure Link**
   - Generate secure sharing link
   - Set expiry: 24 hours
   - Set view limit: 3 times
   - Add PIN protection: 1234
   - Copy link to clipboard

4. **Employer Verification**
   - Open link in browser
   - Enter PIN
   - View all credentials
   - Download verification report
   - Show audit trail

## Key Points:
- Multiple credential support
- Secure sharing methods
- Time and access controls
- Professional use cases
```

### Scenario 4: Credential Revocation

**Duration**: 3 minutes  
**Persona**: Sarah Manager

```markdown
## Steps:

1. **Identify Credential**
   - Search for specific credential
   - Show credential details
   - View issuance history
   - Check current status

2. **Initiate Revocation**
   - Click "Revoke Credential"
   - Select reason: "License Suspended"
   - Add notes: "Traffic violation"
   - Confirm revocation

3. **Immediate Effect**
   - Credential status: Revoked
   - Push notification sent
   - Wallet shows revoked status
   - Verification now fails

4. **Attempt Verification**
   - Try to share revoked credential
   - Show warning message
   - Verification returns "Invalid"
   - Red X mark displayed

## Key Points:
- Immediate revocation
- Real-time updates
- Clear status indication
- Audit compliance
```

## Advanced Scenarios

### Scenario 5: Biometric Authentication

**Duration**: 2 minutes

```markdown
## Steps:

1. **Enable Biometric Protection**
   - Settings → Security
   - Enable Face ID/Touch ID
   - Confirm with password
   - Test biometric scan

2. **Access Protected Credential**
   - Try to view sensitive credential
   - Biometric prompt appears
   - Authenticate with face/finger
   - Credential displays

3. **Failed Authentication**
   - Demonstrate failed attempt
   - Show fallback to PIN
   - Security lockout after 5 attempts

## Key Points:
- Device-level security
- Biometric integration
- Fallback mechanisms
```

### Scenario 6: Cross-Agency Integration

**Duration**: 5 minutes

```markdown
## Transport + Health Departments Demo

1. **Multi-Tenant Setup**
   - Show Transport admin portal
   - Show Health admin portal
   - Different branding/credentials
   - Complete isolation

2. **Citizen Experience**
   - Single wallet view
   - Mixed credentials from both
   - Consistent experience
   - Unified notifications

3. **Cross-Verification**
   - Health facility verifies driver license
   - Transport verifies health card
   - Seamless interoperability

## Key Points:
- Multi-tenancy
- Agency independence
- Citizen convenience
- Interoperability
```

### Scenario 7: Bulk Operations

**Duration**: 4 minutes

```markdown
## Bulk Issuance Demo

1. **Upload CSV File**
   - 100 student IDs
   - Drag and drop CSV
   - Validate data
   - Show preview

2. **Batch Processing**
   - Start bulk issuance
   - Progress bar
   - Real-time status
   - Error handling

3. **Completion**
   - 98 successful
   - 2 errors (show reasons)
   - Download report
   - Email notifications sent

## Key Points:
- Scalability
- Efficiency
- Error handling
- Reporting
```

## Demo Script

### 30-Minute Standard Demo

```markdown
## Opening (2 minutes)
"Welcome to the NumbatWallet Digital Credential demonstration. Today we'll show how WA citizens can securely manage and share their government-issued credentials through the ServiceWA app."

## Act 1: Issuance (8 minutes)
- Login to admin portal
- Issue driver license
- Show in citizen wallet
- Explain security features

## Act 2: Verification (8 minutes)
- Age verification at venue
- Selective disclosure demo
- Offline capability
- Privacy features

## Act 3: Advanced Features (8 minutes)
- Multi-credential sharing
- Biometric security
- Revocation process
- Cross-agency support

## Act 4: Technical Deep-Dive (3 minutes)
- Show API calls
- Display cryptographic proofs
- Review audit logs
- Performance metrics

## Closing (1 minute)
"The NumbatWallet solution provides a secure, privacy-preserving, and user-friendly platform for digital credentials, ready for production deployment."

## Q&A
[Reserved time for questions]
```

### Key Talking Points

```markdown
## Security & Privacy
- "End-to-end encryption protects all credential data"
- "Citizens control what information they share"
- "No tracking or data retention by verifiers"
- "Cryptographic proofs prevent forgery"

## User Experience
- "Seamless integration with ServiceWA"
- "Works offline for critical scenarios"
- "Instant verification - under 1 second"
- "Intuitive interface requires no training"

## Scalability
- "Supports millions of credentials"
- "Multi-tenant architecture for all agencies"
- "Cloud-native auto-scaling"
- "99.99% availability SLA"

## Compliance
- "Meets all TDIF requirements"
- "ISO 18013-5 compliant for mDL"
- "W3C Verifiable Credentials standard"
- "Australian data sovereignty"
```

## Demo Tips

### Do's and Don'ts

```markdown
## DO:
✓ Test everything beforehand
✓ Have backup plans ready
✓ Keep demos under 30 seconds per action
✓ Emphasize user benefits
✓ Show real-world scenarios
✓ Pause for questions
✓ Use citizen-friendly language

## DON'T:
✗ Show development/debug screens
✗ Use technical jargon
✗ Rush through important features
✗ Ignore error messages
✗ Make unrealistic promises
✗ Show incomplete features
✗ Forget offline demo
```

### Common Questions & Answers

```markdown
Q: "What happens if I lose my phone?"
A: "Credentials can be recovered using your ServiceWA account. Biometric protection prevents unauthorized access to lost devices."

Q: "Can this replace physical cards?"
A: "Yes, legally recognized under the Electronic Transactions Act. Physical cards remain available as backup."

Q: "How is this different from a PDF?"
A: "Cryptographically secured, can't be forged, selective disclosure, real-time verification, instant revocation."

Q: "What about elderly citizens?"
A: "Physical cards remain available. Family members can assist. Simple interface designed for all ages."

Q: "Is my data sold or tracked?"
A: "No. Complete privacy. Verifiers can't track usage. No commercial use of data. Government privacy laws apply."
```

## Troubleshooting

### Common Demo Issues

```markdown
## Issue: QR Code Won't Scan
- Solution: Increase screen brightness
- Backup: Type verification code manually
- Prevention: Test lighting beforehand

## Issue: Offline Mode Not Working
- Solution: Pre-cache credentials
- Backup: Use mobile hotspot
- Prevention: Download before demo

## Issue: Login Fails
- Solution: Use backup account
- Backup: Show video recording
- Prevention: Test all accounts

## Issue: Slow Performance
- Solution: Switch to backup environment
- Backup: Explain and continue
- Prevention: Load test beforehand
```

### Emergency Procedures

```markdown
## Complete System Failure
1. Apologize briefly
2. Switch to backup video
3. Explain this is demo environment
4. Offer to reschedule live demo

## Partial Feature Failure
1. Acknowledge the issue
2. Explain the feature conceptually
3. Show screenshots/mockups
4. Note for follow-up

## Network Issues
1. Switch to mobile hotspot
2. Use offline features
3. Show pre-recorded segments
4. Focus on architecture discussion
```

## Demo Checklist

### Pre-Demo (Day Before)

```markdown
- [ ] Test all user accounts
- [ ] Verify demo data loaded
- [ ] Check all devices charged
- [ ] Test network connectivity
- [ ] Practice demo flow
- [ ] Prepare backup materials
- [ ] Send reminder to attendees
- [ ] Prepare name tags
```

### Pre-Demo (1 Hour Before)

```markdown
- [ ] Arrive at venue
- [ ] Test AV equipment
- [ ] Connect all devices
- [ ] Load demo environment
- [ ] Clear browser cache
- [ ] Disable notifications
- [ ] Open all needed tabs
- [ ] Brief support team
```

### During Demo

```markdown
- [ ] Welcome attendees
- [ ] Introduce team
- [ ] Set expectations
- [ ] Record if permitted
- [ ] Follow script
- [ ] Engage audience
- [ ] Handle questions
- [ ] Note feedback
```

### Post-Demo

```markdown
- [ ] Thank attendees
- [ ] Distribute materials
- [ ] Schedule follow-ups
- [ ] Document questions
- [ ] Team debrief
- [ ] Send thank you email
- [ ] Update demo based on feedback
- [ ] Report to management
```

## Demo Assets

### Required Materials

```markdown
## Digital Assets
- Demo slide deck (10 slides max)
- Architecture diagrams
- Security overview
- Comparison matrix
- ROI calculations

## Physical Materials
- Printed handouts
- Business cards
- Device stands
- Backup battery packs
- HDMI adapters

## Giveaways
- Branded USB with materials
- Quick reference cards
- Contact information
- Next steps document
```

### Support During Demo

```markdown
## On-Site Support
- Technical lead: Available
- Support engineer: On standby
- Remote team: Online

## Contact During Demo
- Slack: #poa-demo-support
- Phone: +61 400 DEMO (3366)
- Email: demo-support@numbat.com
```

## Success Metrics

### Demo Evaluation Criteria

```markdown
## Technical (40%)
- All features demonstrated: ___/10
- Performance acceptable: ___/10
- Integration working: ___/10
- Security validated: ___/10

## Business (30%)
- Solves stated problems: ___/10
- ROI demonstrated: ___/10
- Scalability proven: ___/10

## User Experience (30%)
- Intuitive interface: ___/10
- Citizen-friendly: ___/10
- Agency-friendly: ___/10

## Overall Score: ___/100
## Recommendation: [Proceed/Revise/Reject]
```