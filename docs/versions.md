# Technology Versions

> **⚠️ CRITICAL**: This document MUST be updated before starting any development work.
> Run the version check script to ensure all versions are current.

## Last Updated: 2024-12-15

## Version Check Script

```bash
#!/bin/bash
# Run this script before starting development
./scripts/check-versions.sh
```

## Backend Stack (.NET Ecosystem)

| Technology | Current Version | Latest Stable | Notes |
|------------|----------------|---------------|-------|
| **.NET SDK** | 9.0 | 9.0 | Standard Term Support (STS) |
| **.NET Aspire** | 9.4 | 9.4+ | ⚠️ Check weekly - frequent releases |
| **Entity Framework Core** | 9.0.9 | 9.0.9 | Latest patch |
| **PostgreSQL** | 17.6 | 17.6 | Released Sept 2024 |
| **Redis** | 7.4 | 7.4 | Part of Redis Software 7.8.4 |
| **HotChocolate** | 14.0 | 14.0 | v15 expected Dec/Jan |
| **MediatR** | 12.x | 12.x | |
| **FluentValidation** | 11.x | 11.x | |
| **Serilog** | 4.x | 4.x | |
| **Azure SDK for .NET** | 5.x | 5.x | |

## SDK Development

| Technology | Current Version | Latest Stable | Notes |
|------------|----------------|---------------|-------|
| **Flutter** | 3.35 | 3.35 | Latest stable |
| **Dart** | 3.5 | 3.5 | |
| **TypeScript** | 5.9.2 | 5.9.2 | |
| **Node.js** | 22.12.0 LTS | 22.12.0 | "Jod" LTS |

## Infrastructure & DevOps

| Technology | Current Version | Latest Stable | Notes |
|------------|----------------|---------------|-------|
| **Docker** | 27.x | 27.x | |
| **GitHub Actions** | - | - | Uses latest runners |
| **Azure Container Apps** | - | - | Managed service |

## NuGet Packages

### Domain Layer
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>
```

### Application Layer
```xml
<PackageReference Include="MediatR" Version="12.*" />
<PackageReference Include="FluentValidation" Version="11.*" />
<PackageReference Include="AutoMapper" Version="13.*" />
```

### Infrastructure Layer
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.9" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.*" />
<PackageReference Include="StackExchange.Redis" Version="2.8.*" />
```

### Web API
```xml
<PackageReference Include="HotChocolate.AspNetCore" Version="14.0.*" />
<PackageReference Include="Serilog.AspNetCore" Version="8.*" />
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.*" />
```

### Aspire AppHost
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="Aspire.AppHost.Sdk" Version="9.4.0" />
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.PostgreSQL" Version="9.4.*" />
    <PackageReference Include="Aspire.Hosting.Redis" Version="9.4.*" />
    <PackageReference Include="Aspire.Hosting.Azure.Storage" Version="9.4.*" />
  </ItemGroup>
</Project>
```

## Version Update Policy

### Automatic Updates
- **Patch versions** (e.g., 9.0.8 → 9.0.9): Update immediately
- **Security patches**: Update within 24 hours

### Manual Review Required
- **Minor versions** (e.g., 9.3 → 9.4): Test and update within 1 week
- **Major versions** (e.g., 8.x → 9.x): Evaluate, plan migration

### Special Attention
- **.NET Aspire**: Check weekly for updates (rapid release cycle)
- **PostgreSQL**: Test thoroughly before minor version updates
- **Flutter**: Test UI components after updates

## Checking for Updates

### .NET Packages
```bash
dotnet list package --outdated
```

### npm Packages
```bash
npm outdated
```

### Flutter Packages
```bash
flutter pub outdated
```

### Docker Images
```bash
docker images | grep -E "postgres|redis"
```

## Version Lock Justifications

> Document any intentional version locks here with reasons

| Package | Locked Version | Reason | Review Date |
|---------|---------------|--------|-------------|
| _(none)_ | - | - | - |

## Related Issues

- #99: POA-VERSIONS-001 - Version Management Strategy
- #100-103: POA-ASPIRE - .NET Aspire setup
- #94-98: POA-BACKEND - Backend architecture

## GitHub Actions

- **Weekly Version Check**: `.github/workflows/version-check.yml`
- Runs every Monday at 9 AM UTC
- Creates issue if updates are available

## Updating This Document

1. Run `./scripts/check-versions.sh`
2. Update version tables above
3. Document any version locks
4. Commit with message: "chore: update versions.md - [date]"
5. Tag significant updates: `git tag versions-2024-12-15`