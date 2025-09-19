# NumbatWallet Infrastructure as Code (Bicep)

This directory contains Azure Bicep templates for deploying the NumbatWallet infrastructure.

## Directory Structure

```
bicep/
├── main.bicep                 # Main orchestration file
├── modules/                   # Reusable Bicep modules
│   ├── key-vault.bicep       # Azure Key Vault configuration
│   ├── storage-account.bicep # Storage Account (to be created)
│   ├── redis-cache.bicep     # Redis Cache (to be created)
│   ├── postgresql.bicep      # PostgreSQL Flexible Server (to be created)
│   ├── container-apps.bicep  # Container Apps (to be created)
│   └── ...                   # Other modules
├── parameters/               # Environment-specific parameters
│   ├── dev.parameters.json  # Development environment
│   ├── test.parameters.json # Test environment (to be created)
│   └── prod.parameters.json # Production environment
└── scripts/                  # Deployment scripts
    ├── deploy.ps1           # PowerShell deployment script (to be created)
    └── deploy.sh            # Bash deployment script (to be created)
```

## Prerequisites

1. Azure CLI installed (version 2.50.0 or later)
2. Bicep CLI installed (version 0.20.0 or later)
3. Azure subscription with appropriate permissions
4. Service Principal or Managed Identity for deployment

## Deployment

### Using Azure CLI

```bash
# Login to Azure
az login

# Set subscription
az account set --subscription "<subscription-id>"

# Create resource group (if not exists)
az group create --name rg-numbatwallet-dev-aue --location australiaeast

# Deploy to development environment
az deployment sub create \
  --location australiaeast \
  --template-file main.bicep \
  --parameters parameters/dev.parameters.json \
  --name "numbatwallet-dev-$(date +%Y%m%d-%H%M%S)"

# Deploy to production environment
az deployment sub create \
  --location australiaeast \
  --template-file main.bicep \
  --parameters parameters/prod.parameters.json \
  --name "numbatwallet-prod-$(date +%Y%m%d-%H%M%S)"
```

### Using PowerShell

```powershell
# Login to Azure
Connect-AzAccount

# Set subscription
Set-AzContext -SubscriptionId "<subscription-id>"

# Deploy to development environment
New-AzSubscriptionDeployment `
  -Location "australiaeast" `
  -TemplateFile "main.bicep" `
  -TemplateParameterFile "parameters/dev.parameters.json" `
  -Name "numbatwallet-dev-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
```

## Module Status

| Module | Status | Issue |
|--------|--------|-------|
| Key Vault | ✅ Completed | #145 |
| Storage Account | ✅ Completed | #149 |
| Redis Cache | ✅ Completed | #148 |
| Application Insights | ✅ Completed | #147 |
| Container Apps Environment | ✅ Completed | #146 |
| PostgreSQL Flexible Server | ✅ Completed | - |
| Container Registry | ✅ Completed | - |
| Log Analytics | ✅ Completed | - |
| Deployment Scripts | ✅ Completed | #151 |
| GitHub Actions CI/CD | ✅ Completed | #152 |

## Security Considerations

1. **Private Endpoints**: Production environment uses private endpoints for all services
2. **Key Vault**: All secrets stored in Azure Key Vault with RBAC
3. **Network Isolation**: Services deployed with network ACLs and firewall rules
4. **Encryption**: All data encrypted at rest and in transit
5. **Managed Identity**: Use managed identities for service-to-service authentication

## Parameter Files

Before deployment, update the following in parameter files:

1. `administratorObjectId`: Azure AD object ID of the administrator
2. `managedIdentityObjectId`: Object ID of the managed identity (after creation)
3. `privateEndpointSubnetId`: Subnet ID for private endpoints (production)
4. `postgresAdminPassword`: Store in Key Vault before deployment

## Validation

Validate templates before deployment:

```bash
# Validate main template
az deployment sub validate \
  --location australiaeast \
  --template-file main.bicep \
  --parameters parameters/dev.parameters.json

# What-if deployment (preview changes)
az deployment sub what-if \
  --location australiaeast \
  --template-file main.bicep \
  --parameters parameters/dev.parameters.json
```

## Tags

All resources are tagged with:
- `Application`: NumbatWallet
- `Environment`: dev/test/prod
- `ManagedBy`: Bicep
- `DeploymentDate`: Auto-generated
- `CostCenter`: Based on environment
- `Owner`: Based on environment

Production adds:
- `Compliance`: TDIF
- `DataClassification`: Sensitive
- `SLA`: 99.5

## Next Steps

1. Complete remaining Bicep modules (Storage, Redis, App Insights, etc.)
2. Create deployment scripts for automation
3. Set up GitHub Actions for CI/CD validation
4. Configure Azure Policy for compliance
5. Implement cost management alerts

## References

- [Azure Bicep Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [NumbatWallet Architecture](../../docs/architecture/)
- [Security Guidelines](../../docs/security/)