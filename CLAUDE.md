# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This is a tender/proposal documentation repository for a Digital Wallet and Verifiable Credentials Solution for Western Australia (WA). The repository contains technical specifications, requirements, and architectural documentation for DPC2142 - a request for a managed service provider to deliver, host, and support a digital wallet solution integrated with the ServiceWA mobile application.

## Repository Structure

```
/tender
├── docs/                         # Main documentation directory
│   ├── ChatGPT/                  # Processed documentation in Markdown format
│   │   ├── PRD_Master.md         # Master Product Requirements Document
│   │   ├── Appendix_*.md         # Technical appendices covering various aspects
│   │   └── PDF AND DOC/          # Original Word and PDF versions
│   └── DPC2142 Attachment*.pdf   # Official tender documents and specifications
```

## Key Documentation Files

- **PRD_Master.md**: Comprehensive product requirements document outlining the digital wallet solution, including executive summary, goals, stakeholders, functional/non-functional requirements, and standards compliance
- **Appendix_SolutionArchitecture.md**: Technical architecture including system components, deployment topology, and integration points
- **Appendix_APIs_SDKs.md**: API specifications and SDK requirements for Flutter, .NET, and TypeScript/JS
- **Appendix_Security_Privacy_Compliance.md**: Security controls, privacy requirements, and compliance mappings
- **Appendix_Workflows.md**: Detailed credential issuance, presentation, and verification workflows
- **Appendix_DataModel.md**: Data structures for credentials, trust lists, and wallet operations

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