# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This is a tender/proposal documentation repository for a Digital Wallet and Verifiable Credentials Solution for Western Australia (WA). The repository contains technical specifications, requirements, and architectural documentation for DPC2142 - a request for a managed service provider to deliver, host, and support a digital wallet solution integrated with the ServiceWA mobile application.

## Repository Structure

```
/repo/
├── NumbatWallet/                 # Main repository
│   ├── CLAUDE.md                 # This file - AI assistant context
│   ├── README.md                 # Repository overview
│   └── docs/                     # GitHub Pages (Azure Calculator)
│       └── index.html            # Interactive Azure pricing calculator
│
└── NumbatWallet.wiki/            # Wiki repository (separate clone)
    ├── Home.md                   # Master PRD (main documentation)
    ├── Solution-Architecture.md  # Technical architecture
    ├── Security-Privacy-Compliance.md
    ├── Technical-Specification.md
    ├── API-Documentation.md
    ├── SDK-Documentation.md
    ├── SDK-Flutter-Guide.md      # Flutter SDK implementation
    ├── SDK-DotNet-Guide.md       # .NET SDK implementation
    ├── SDK-JavaScript-Guide.md   # Web SDK implementation
    ├── Testing-Strategy.md
    ├── Deployment-Guide.md
    ├── Support-Model.md
    ├── Pricing-Assumptions.md
    ├── Detailed-Cost-Breakdown.md
    ├── Azure-Justification-Pricing.md
    ├── Azure-Calculator-Guide.md # Azure pricing calculator inputs
    ├── Team-Resources.md
    ├── Risk-Matrix.md
    ├── Compliance-Matrix.md
    ├── _Sidebar.md               # Wiki navigation
    └── _Footer.md                # Wiki footer
```

## Documentation Access

- **GitHub Wiki**: https://github.com/Credenxia/NumbatWallet/wiki
- **Azure Calculator**: https://credenxia.github.io/NumbatWallet/
- **Wiki Repository**: The wiki is a separate Git repository at the same level as NumbatWallet
- **All documentation is maintained in the `NumbatWallet.wiki` repository**

## Key Documentation Files

- **Home.md**: Master PRD - Comprehensive product requirements document outlining the digital wallet solution
- **Solution-Architecture.md**: Technical architecture including system components, deployment topology, and integration points
- **API-Documentation.md**: OpenAPI 3.0 specifications, endpoint details, authentication flows
- **SDK-Documentation.md**: Overview of SDK offerings with links to detailed guides
- **SDK-Flutter-Guide.md**: Complete Flutter SDK documentation and examples
- **SDK-DotNet-Guide.md**: Enterprise .NET SDK documentation
- **SDK-JavaScript-Guide.md**: TypeScript/JavaScript SDK for web applications
- **Security-Privacy-Compliance.md**: Security controls, privacy requirements, and compliance mappings
- **Technical-Specification.md**: Data models, state machines, and component specifications
- **Azure-Calculator-Guide.md**: Detailed Azure service configurations for cost estimation

## Project Context

### Solution Overview
- **Cloud-native, multi-tenant wallet platform** built on Microsoft .NET and C#
- PostgreSQL for persistence, running in Azure AU regions
- Implements W3C verifiable credential standards with DIDs and OpenID Connect flows
- Flutter SDK for ServiceWA integration, .NET SDK for agencies, TypeScript/JS SDK for verifiers

### Key Standards and Protocols
- ISO/IEC 18013-5/7 (mobile driving licence)
- ISO/IEC 23220 (mobile eID architecture)
- W3C VC Data Model and DIDs
- OID4VCI/OIDC4VP for credential operations
- Trusted Digital Identity Framework (TDIF)

### Multi-tenancy Architecture
- Option A: Per-tenant database (recommended for pilot)
- Option B: Shared database with Row-Level Security (RLS)

## Important Notes

1. This is a **documentation-only repository** - no source code is present
2. The tender is for a **Proof-of-Operation** followed by a 12-month Pilot Phase
3. All data and operations must remain within **Australian sovereign boundaries**
4. The solution must integrate with existing WA government infrastructure including ServiceWA app and WA Identity Exchange
5. Security and compliance are paramount with requirements for ISO 27001, TDIF, GDPR, and Australian Privacy Act compliance