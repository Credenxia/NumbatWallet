# NumbatWallet - Digital Wallet and Verifiable Credentials Platform

## 🎯 Overview

NumbatWallet is Western Australia's official Digital Wallet and Verifiable Credentials solution, integrating with ServiceWA to provide citizens with secure, privacy-preserving digital identity services.

This repository contains the backend services, documentation, and infrastructure code for the NumbatWallet platform.

## 📁 Repository Structure

```
NumbatWallet/
├── src/                          # .NET 9 backend services
│   ├── NumbatWallet.Domain/      # Domain layer
│   ├── NumbatWallet.Application/ # Application layer
│   ├── NumbatWallet.Infrastructure/ # Infrastructure layer
│   ├── NumbatWallet.Web.Api/    # REST API project
│   ├── NumbatWallet.Web.Admin/  # Admin portal project
│   └── Tests/                   # Test projects
├── docs/
│   ├── standards/      # Development standards and guidelines
│   ├── poa/           # Proof of Authority documentation
│   └── tender/        # Original tender documents
├── infrastructure/     # Azure Bicep templates and deployment scripts
└── .github/           # GitHub Actions and templates
```

### Related Repositories

- **[NumbatWallet SDKs](https://github.com/Credenxia/NumbatWallet-sdks)** - Client SDKs for .NET, Flutter, and TypeScript
- **[NumbatWallet Wiki](https://github.com/Credenxia/NumbatWallet/wiki)** - Comprehensive documentation and specifications

## 🚀 Quick Start

### Prerequisites

- .NET 9 SDK
- Azure CLI
- Docker Desktop
- PostgreSQL 15+

### Development Setup

```bash
# Clone the repository
git clone https://github.com/Credenxia/NumbatWallet.git
cd NumbatWallet

# Navigate to source
cd src

# Restore dependencies
dotnet restore

# Run database migrations
dotnet ef database update --project NumbatWallet.Infrastructure --startup-project NumbatWallet.Web.Api

# Run the API
dotnet run --project NumbatWallet.Web.Api

# Run the Admin Portal (in another terminal)
dotnet run --project NumbatWallet.Web.Admin
```

## 🏗️ Architecture

### Technology Stack

- **Backend**: .NET 9, ASP.NET Core, Entity Framework Core
- **Database**: PostgreSQL with multi-tenant support
- **Cloud**: Microsoft Azure (Australia regions)
- **Identity**: Azure Entra ID, ServiceWA integration
- **Standards**: W3C VC, DIDs, OpenID4VCI, ISO 18013-5

### Key Components

1. **Multi-Tenant Wallet Service** - Manages digital wallets for citizens
2. **Credential Issuance Service** - Issues verifiable credentials
3. **Verification Service** - Validates and verifies credentials
4. **PKI Management** - Certificate and key lifecycle management

## 📋 Project Status

### Current Phase: Proof of Operation (POA)

- **Duration**: 5 weeks (Oct 6 - Nov 6, 2025)
- **Milestone**: Demonstrable prototype with core functionality
- **Tracking**: [GitHub Project #18](https://github.com/Credenxia/NumbatWallet/projects/18)

### Deliverables

- ✅ Backend API services
- ✅ Three SDKs (.NET, Flutter, TypeScript)
- ✅ Security and compliance documentation
- ✅ Deployment automation
- ✅ Comprehensive test coverage

## 🔒 Security

- **Compliance**: ISO 27001, TDIF, GDPR, Australian Privacy Act
- **Encryption**: AES-256 at rest, TLS 1.3 in transit
- **Authentication**: mTLS, OAuth 2.0, OpenID Connect
- **Audit**: Comprehensive logging and monitoring

## 📚 Documentation

### Development

- [Coding Standards](./docs/standards/master/architecture.md)
- [API Design Guidelines](./docs/standards/master/api-design.md)
- [Testing Standards](./docs/standards/master/testing.md)
- [Security Requirements](./docs/standards/master/security.md)

### POA Documentation

- [Task Breakdown](./docs/poa/task-breakdown.md)
- [Weekly Deliverables](./docs/poa/weekly-deliverables.md)
- [Test Plan](./docs/poa/test-plan.md)

### SDKs

- [SDK Repository](https://github.com/Credenxia/NumbatWallet-sdks)
- [Flutter SDK Guide](https://github.com/Credenxia/NumbatWallet/wiki/SDK-Flutter-Guide)
- [.NET SDK Guide](https://github.com/Credenxia/NumbatWallet/wiki/SDK-DotNet-Guide)
- [TypeScript SDK Guide](https://github.com/Credenxia/NumbatWallet/wiki/SDK-JavaScript-Guide)

## 🧪 Testing

```bash
# Run unit tests
dotnet test --filter Category=Unit

# Run integration tests
dotnet test --filter Category=Integration

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run conformance tests
dotnet test --filter Category=Conformance
```

## 🚢 Deployment

### Local Development

```bash
docker-compose up -d
```

### Azure Deployment

```bash
cd infrastructure/bicep
az deployment sub create --location australiaeast --template-file main.bicep
```

## 🤝 Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md) for development guidelines.

## 📄 License

This project is licensed under the terms specified in the tender agreement DPC2142.

## 🔗 Links

- [Project Wiki](https://github.com/Credenxia/NumbatWallet/wiki)
- [Issue Tracker](https://github.com/Credenxia/NumbatWallet/issues)
- [Project Board](https://github.com/Credenxia/NumbatWallet/projects/18)
- [Azure Pricing Calculator](https://credenxia.github.io/NumbatWallet/)

## 📞 Support

For technical inquiries related to the POA, please contact the development team through GitHub Issues.

---

**NumbatWallet** - Empowering Western Australians with secure digital identity