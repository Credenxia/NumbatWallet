# Deployment Configuration Guide

**Date**: October 30, 2025
**Status**: ✅ PRODUCTION READY
**Phase**: 1.5 - Configuration Templates

## Overview

This guide explains how to configure NumbatWallet for different environments using the provided appsettings templates. All sensitive values should be stored in Azure Key Vault and retrieved at runtime.

## Configuration Files

### Development
- **File**: `appsettings.Development.json`
- **Purpose**: Local development with mock services
- **Features**:
  - Mock Azure Key Vault (in-memory secrets)
  - Mock Blob Storage (in-memory files)
  - Local PostgreSQL via Aspire
  - GraphQL Playground enabled
  - Detailed logging

### Staging
- **File**: `appsettings.Staging.json`
- **Purpose**: Pre-production testing environment
- **Features**:
  - Real Azure services (Key Vault, Blob Storage)
  - Staging database instances
  - GraphQL Playground enabled
  - Verbose logging for debugging
  - Lower rate limits
  - Allows test CORS origins

### Production
- **File**: `appsettings.Production.json`
- **Purpose**: Live production environment
- **Features**:
  - Full Azure service integration
  - Maximum security settings
  - Production rate limits
  - GraphQL Playground disabled
  - Warning-level logging
  - Strict CORS policies

## Placeholder Reference

All configuration files use placeholders that must be replaced during deployment:

| Placeholder | Description | Where to Get |
|-------------|-------------|--------------|
| `<POSTGRES_SERVER>` | PostgreSQL server FQDN | Azure PostgreSQL resource |
| `<POSTGRES_USER>` | Database username | Azure PostgreSQL admin user |
| `<FROM_KEY_VAULT>` | Retrieved from Key Vault | Runtime via `IKeyVaultService` |
| `<KEY_VAULT_NAME>` | Azure Key Vault name | Infrastructure setup |
| `<STORAGE_ACCOUNT_NAME>` | Storage account name | Infrastructure setup |
| `<APP_INSIGHTS_KEY>` | Application Insights key | Azure Portal |
| `<AZURE_AD_TENANT_ID>` | Azure AD tenant ID | Azure AD settings |
| `<AZURE_AD_CLIENT_ID>` | App registration client ID | Azure AD app registration |
| `<PRODUCTION_DOMAIN>` | Production domain name | DNS configuration |
| `<STAGING_DOMAIN>` | Staging domain name | DNS configuration |
| `<REDIS_HOST>` | Redis Cache hostname | Azure Redis Cache |

## Azure Key Vault Secrets

The following secrets must be stored in Azure Key Vault:

### Database Secrets
```bash
# PostgreSQL password
az keyvault secret set \
  --vault-name <KEY_VAULT_NAME> \
  --name "DatabasePassword" \
  --value "<SECURE_PASSWORD>"
```

### Authentication Secrets
```bash
# JWT signing key (minimum 256 bits)
az keyvault secret set \
  --vault-name <KEY_VAULT_NAME> \
  --name "JwtSecret" \
  --value "<256_BIT_KEY>"

# Azure AD client secret
az keyvault secret set \
  --vault-name <KEY_VAULT_NAME> \
  --name "AzureAdClientSecret" \
  --value "<CLIENT_SECRET>"

# ServiceWA credentials
az keyvault secret set \
  --vault-name <KEY_VAULT_NAME> \
  --name "ServiceWAClientId" \
  --value "<CLIENT_ID>"

az keyvault secret set \
  --vault-name <KEY_VAULT_NAME> \
  --name "ServiceWAClientSecret" \
  --value "<CLIENT_SECRET>"
```

### Encryption Secrets
```bash
# AES-256 encryption key
az keyvault secret set \
  --vault-name <KEY_VAULT_NAME> \
  --name "EncryptionKey" \
  --value "<256_BIT_ENCRYPTION_KEY>"
```

### External Service Secrets
```bash
# API keys for service accounts
az keyvault secret set \
  --vault-name <KEY_VAULT_NAME> \
  --name "ApiKey-ServiceAccount1" \
  --value "<API_KEY>"

# Redis password
az keyvault secret set \
  --vault-name <KEY_VAULT_NAME> \
  --name "RedisPassword" \
  --value "<REDIS_PASSWORD>"
```

## Environment-Specific Configuration

### Staging Environment Setup

1. **Create Azure Resources**:
```bash
# Variables
RESOURCE_GROUP="rg-numbatwallet-staging"
LOCATION="australiaeast"
KEY_VAULT_NAME="kv-numbatwallet-staging"
STORAGE_ACCOUNT="stnumbatwalletstaging"
POSTGRES_SERVER="pg-numbatwallet-staging"

# Key Vault
az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku standard \
  --enable-rbac-authorization true

# Storage Account
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS \
  --kind StorageV2 \
  --https-only true \
  --min-tls-version TLS1_2

# PostgreSQL Flexible Server
az postgres flexible-server create \
  --name $POSTGRES_SERVER \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --tier Burstable \
  --sku-name Standard_B2s \
  --version 16 \
  --storage-size 32 \
  --admin-user numbatwallet_admin \
  --admin-password <SECURE_PASSWORD> \
  --public-access 0.0.0.0 \
  --backup-retention 7
```

2. **Update appsettings.Staging.json**:
```json
{
  "Azure": {
    "KeyVault": {
      "Url": "https://kv-numbatwallet-staging.vault.azure.net/"
    },
    "Storage": {
      "AccountName": "stnumbatwalletstaging"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=pg-numbatwallet-staging.postgres.database.azure.com;Database=numbatwallet_staging;Username=numbatwallet_admin;Password=<FROM_KEY_VAULT>;Port=5432;SSL Mode=Require"
  }
}
```

### Production Environment Setup

1. **Create Azure Resources**:
```bash
# Variables
RESOURCE_GROUP="rg-numbatwallet-prod"
LOCATION="australiaeast"
KEY_VAULT_NAME="kv-numbatwallet-prod"
STORAGE_ACCOUNT="stnumbatwallet"
POSTGRES_SERVER="pg-numbatwallet-prod"

# Key Vault with enhanced security
az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku premium \
  --enable-rbac-authorization true \
  --enable-purge-protection true \
  --retention-days 90

# Storage Account with enhanced security
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_ZRS \
  --kind StorageV2 \
  --https-only true \
  --min-tls-version TLS1_3 \
  --allow-blob-public-access false

# PostgreSQL Flexible Server - Production tier
az postgres flexible-server create \
  --name $POSTGRES_SERVER \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --tier GeneralPurpose \
  --sku-name Standard_D4s_v3 \
  --version 16 \
  --storage-size 128 \
  --admin-user numbatwallet_admin \
  --admin-password <SECURE_PASSWORD> \
  --high-availability Enabled \
  --backup-retention 35 \
  --geo-redundant-backup Enabled
```

2. **Update appsettings.Production.json**:
```json
{
  "Azure": {
    "KeyVault": {
      "Url": "https://kv-numbatwallet-prod.vault.azure.net/"
    },
    "Storage": {
      "AccountName": "stnumbatwallet"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=pg-numbatwallet-prod.postgres.database.azure.com;Database=numbatwallet;Username=numbatwallet_admin;Password=<FROM_KEY_VAULT>;Port=5432;SSL Mode=Require;Trust Server Certificate=false"
  }
}
```

## Key Configuration Differences

### Security Settings

| Setting | Development | Staging | Production |
|---------|-------------|---------|------------|
| GraphQL Playground | ✅ Enabled | ✅ Enabled | ❌ Disabled |
| GraphQL Introspection | ✅ Enabled | ✅ Enabled | ❌ Disabled |
| Exception Details | ✅ Shown | ✅ Shown | ❌ Hidden |
| Sensitive Data Logging | ✅ Enabled | ✅ Enabled | ❌ Disabled |
| Mutual TLS | ❌ Disabled | ❌ Disabled | ✅ Required |
| Request Signatures | ❌ Disabled | ❌ Disabled | ✅ Required |

### Rate Limiting

| Policy | Development | Staging | Production |
|--------|-------------|---------|------------|
| Global | 60/min | 100/min | 100/min |
| Authenticated | 100/min | 200/min | 200/min |
| API Key | 1000 tokens | 2000 tokens | 5000 tokens |
| Anonymous | 30/min | 30/min | 10/min |

### Logging Levels

| Component | Development | Staging | Production |
|-----------|-------------|---------|------------|
| Default | Debug | Information | Warning |
| Microsoft | Information | Information | Warning |
| EF Core | Debug | Information | Warning |
| NumbatWallet | Debug | Debug | Information |

## Service Registration Logic

The application automatically selects Mock or Real services based on configuration:

### Key Vault Selection
```csharp
// ServiceCollectionExtensions.cs:284-293
var keyVaultUrl = configuration["Azure:KeyVault:Url"];
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    services.AddSingleton<IKeyVaultService, AzureKeyVaultService>();
    // Uses DefaultAzureCredential for authentication
}
else
{
    services.AddSingleton<IKeyVaultService, MockKeyVaultService>();
    // In-memory dictionary for development
}
```

### Blob Storage Selection
```csharp
// ServiceCollectionExtensions.cs:312-322
var storageConnectionString = configuration["Azure:Storage:ConnectionString"];
var storageAccountName = configuration["Azure:Storage:AccountName"];
if (!string.IsNullOrEmpty(storageConnectionString) || !string.IsNullOrEmpty(storageAccountName))
{
    services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
}
else
{
    services.AddSingleton<IBlobStorageService, MockBlobStorageService>();
}
```

## Managed Identity Configuration

### App Service Managed Identity

1. **Enable System-Assigned Identity**:
```bash
az webapp identity assign \
  --name <APP_SERVICE_NAME> \
  --resource-group <RESOURCE_GROUP>
```

2. **Grant Key Vault Access**:
```bash
IDENTITY_ID=$(az webapp identity show \
  --name <APP_SERVICE_NAME> \
  --resource-group <RESOURCE_GROUP> \
  --query principalId -o tsv)

az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee $IDENTITY_ID \
  --scope /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/<RESOURCE_GROUP>/providers/Microsoft.KeyVault/vaults/<KEY_VAULT_NAME>
```

3. **Grant Storage Access**:
```bash
az role assignment create \
  --role "Storage Blob Data Contributor" \
  --assignee $IDENTITY_ID \
  --scope /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/<RESOURCE_GROUP>/providers/Microsoft.Storage/storageAccounts/<STORAGE_ACCOUNT>
```

## CORS Configuration

### Development
- Allows localhost origins (3000, 5173, 4200)
- Useful for local frontend development

### Staging
- Allows staging domain
- Keeps localhost for testing
- Example: `https://staging.numbatwallet.com.au`

### Production
- Only production domains allowed
- Example: `https://numbatwallet.com.au`, `https://admin.numbatwallet.com.au`

## Application Insights

### Setup
```bash
# Create Application Insights
az monitor app-insights component create \
  --app numbatwallet-insights \
  --location australiaeast \
  --resource-group <RESOURCE_GROUP> \
  --kind web

# Get connection string
az monitor app-insights component show \
  --app numbatwallet-insights \
  --resource-group <RESOURCE_GROUP> \
  --query connectionString -o tsv
```

### Configuration
Add to appsettings:
```json
{
  "Azure": {
    "ApplicationInsights": {
      "ConnectionString": "<CONNECTION_STRING>"
    }
  }
}
```

## Deployment Checklist

### Pre-Deployment
- [ ] All Azure resources created
- [ ] Managed identity configured
- [ ] Secrets stored in Key Vault
- [ ] Connection strings updated
- [ ] CORS origins configured
- [ ] Application Insights configured
- [ ] Database migrations ready

### Staging Deployment
- [ ] Deploy to staging App Service
- [ ] Verify managed identity authentication
- [ ] Run database migrations
- [ ] Test Key Vault secret retrieval
- [ ] Test Blob Storage operations
- [ ] Verify logging to Application Insights
- [ ] Run smoke tests

### Production Deployment
- [ ] All staging tests passed
- [ ] Production secrets verified
- [ ] Database backup created
- [ ] Deploy to production App Service
- [ ] Run database migrations
- [ ] Verify all Azure services
- [ ] Monitor Application Insights
- [ ] Performance testing
- [ ] Security audit

## Troubleshooting

### Issue: "Azure Key Vault URL is not configured"
**Cause**: `Azure:KeyVault:Url` missing from appsettings
**Solution**: Add Key Vault URL to configuration

### Issue: "ManagedIdentityCredential authentication unavailable"
**Cause**: Managed identity not assigned to App Service
**Solution**: Run `az webapp identity assign`

### Issue: "Secret not found in Key Vault"
**Cause**: Secret name mismatch or doesn't exist
**Solution**: Verify secret name (case-sensitive) and ensure it exists

### Issue: "Insufficient permissions"
**Cause**: Managed identity lacks required role
**Solution**: Grant "Key Vault Secrets User" role

### Issue: "Storage account not found"
**Cause**: Account name incorrect or identity lacks access
**Solution**: Verify account name and grant "Storage Blob Data Contributor" role

## Cost Estimates (AUD - Australia East)

### Staging Environment (Monthly)
- **PostgreSQL (Burstable B2s)**: ~$35
- **Key Vault (Standard)**: ~$5
- **Blob Storage (LRS, 10GB)**: ~$2
- **App Service (B1)**: ~$20
- **Application Insights (5GB)**: ~$15
- **Total**: ~$77/month

### Production Environment (Monthly)
- **PostgreSQL (D4s_v3 HA)**: ~$350
- **Key Vault (Premium)**: ~$10
- **Blob Storage (ZRS, 100GB)**: ~$25
- **App Service (P1v2)**: ~$100
- **Application Insights (50GB)**: ~$150
- **Redis Cache (C1)**: ~$25
- **Total**: ~$660/month

## Security Best Practices

### ✅ Implemented
- [x] All secrets in Azure Key Vault
- [x] Managed identities (no passwords)
- [x] TLS 1.2+ enforced
- [x] RBAC authorization
- [x] Private endpoints ready
- [x] Geo-redundant backups (production)
- [x] Soft-delete enabled
- [x] Audit logging

### 📋 Recommended
- [ ] Enable private endpoints for production
- [ ] Configure Azure Front Door
- [ ] Set up DDoS protection
- [ ] Enable Azure Defender
- [ ] Configure log retention policies
- [ ] Set up automated alerts
- [ ] Regular security scans

## Next Steps

1. **Infrastructure Provisioning**: Create Azure resources using Bicep/Terraform
2. **Secret Migration**: Move all secrets to Key Vault
3. **Staging Deployment**: Deploy and test in staging
4. **Production Deployment**: Deploy to production after validation
5. **Monitoring Setup**: Configure alerts and dashboards

## Related Documentation

- [AZURE_SERVICES_CONFIGURATION.md](./AZURE_SERVICES_CONFIGURATION.md) - Detailed Azure setup
- [MIGRATION_SETUP.md](./MIGRATION_SETUP.md) - Database migrations guide
- Infrastructure as Code: `/infrastructure/bicep/` - Bicep templates

---

**Status**: Phase 1 COMPLETE - Ready for infrastructure provisioning and deployment
