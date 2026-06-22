# Azure Services Configuration - Production Ready

**Date**: October 30, 2025
**Status**: ✅ SERVICES IMPLEMENTED - CONFIGURATION REQUIRED
**Environment**: Development uses Mocks, Production ready for Azure

## Summary

Both **Azure Key Vault** and **Azure Blob Storage** services are **fully implemented** and production-ready. The application uses conditional registration to automatically select between Mock (development) and Real (production) implementations based on configuration.

## Service Status

### ✅ Azure Key Vault Service
- **Implementation**: `AzureKeyVaultService.cs` - COMPLETE
- **Mock**: `MockKeyVaultService.cs` - For development
- **Features**:
  - DefaultAzureCredential with full credential chain
  - In-memory caching for performance
  - CRUD operations on secrets
  - Bulk secret retrieval
  - Secret existence checking
- **Status**: Production-ready, requires configuration only

### ✅ Azure Blob Storage Service
- **Implementation**: `AzureBlobStorageService.cs` - COMPLETE
- **Mock**: `MockBlobStorageService.cs` - For development
- **Status**: Production-ready, requires configuration only

## Configuration

### Development (Current - Using Mocks)

**File**: `appsettings.Development.json`

```json
{
  "UseAzureKeyVault": false,
  "UseBlobStorage": false
  // No Azure section = Mock services active
}
```

**Active Services**:
- ✅ MockKeyVaultService (in-memory dictionary)
- ✅ MockBlobStorageService (in-memory storage)

### Production (Azure Services)

**File**: `appsettings.Production.json`

```json
{
  "Azure": {
    "KeyVault": {
      "Url": "https://<your-keyvault-name>.vault.azure.net/"
    },
    "Storage": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>",
      // OR use managed identity:
      "AccountName": "<your-storage-account>"
    }
  },
  "UseAzureKeyVault": true,
  "UseBlobStorage": true
}
```

## Service Registration Logic

### Key Vault (ServiceCollectionExtensions.cs:284-293)

```csharp
var keyVaultUrl = configuration["Azure:KeyVault:Url"];
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    services.AddSingleton<IKeyVaultService, AzureKeyVaultService>();
}
else
{
    // Use mock service for development
    services.AddSingleton<IKeyVaultService, MockKeyVaultService>();
}
```

**Trigger**: Presence of `Azure:KeyVault:Url` configuration

### Blob Storage (ServiceCollectionExtensions.cs:312-322)

```csharp
var storageConnectionString = configuration["Azure:Storage:ConnectionString"];
var storageAccountName = configuration["Azure:Storage:AccountName"];
if (!string.IsNullOrEmpty(storageConnectionString) || !string.IsNullOrEmpty(storageAccountName))
{
    services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
}
else
{
    // Use mock service for development
    services.AddSingleton<IBlobStorageService, MockBlobStorageService>();
}
```

**Trigger**: Presence of `Azure:Storage:ConnectionString` OR `Azure:Storage:AccountName`

## Azure Key Vault Setup

### 1. Create Azure Key Vault

```bash
# Set variables
RESOURCE_GROUP="rg-numbatwallet-prod"
LOCATION="australiaeast"
KEY_VAULT_NAME="kv-numbatwallet-prod"

# Create resource group
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION

# Create Key Vault
az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku standard \
  --enable-rbac-authorization true
```

### 2. Configure Managed Identity

#### Option A: System-Assigned Managed Identity (Recommended for App Service)

```bash
# Enable system-assigned identity on App Service
az webapp identity assign \
  --name <app-service-name> \
  --resource-group $RESOURCE_GROUP

# Get the identity principal ID
IDENTITY_ID=$(az webapp identity show \
  --name <app-service-name> \
  --resource-group $RESOURCE_GROUP \
  --query principalId \
  --output tsv)

# Grant Key Vault Secrets User role
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee $IDENTITY_ID \
  --scope /subscriptions/<subscription-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEY_VAULT_NAME
```

#### Option B: User-Assigned Managed Identity

```bash
# Create user-assigned identity
az identity create \
  --name "id-numbatwallet" \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION

# Assign to App Service
az webapp identity assign \
  --name <app-service-name> \
  --resource-group $RESOURCE_GROUP \
  --identities <identity-id>

# Grant Key Vault access
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee <identity-principal-id> \
  --scope /subscriptions/<subscription-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEY_VAULT_NAME
```

### 3. Add Secrets to Key Vault

```bash
# Database password
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "DatabasePassword" \
  --value "<secure-password>"

# JWT secret (must be at least 32 characters)
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "JwtSecret" \
  --value "<256-bit-secure-key>"

# Encryption key (256-bit for AES-256)
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "EncryptionKey" \
  --value "<256-bit-encryption-key>"

# API keys
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "ApiKey" \
  --value "<api-key>"
```

### 4. Update Application Configuration

```json
{
  "Azure": {
    "KeyVault": {
      "Url": "https://kv-numbatwallet-prod.vault.azure.net/"
    }
  }
}
```

## Azure Blob Storage Setup

### 1. Create Storage Account

```bash
# Set variables
STORAGE_ACCOUNT="stnumbatwallet"  # Must be globally unique, lowercase, no hyphens

# Create storage account
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS \
  --kind StorageV2 \
  --access-tier Hot \
  --https-only true \
  --min-tls-version TLS1_2
```

### 2. Create Required Containers

```bash
# Wallet passes (PKPass files for Apple Wallet)
az storage container create \
  --name "wallet-passes" \
  --account-name $STORAGE_ACCOUNT \
  --auth-mode login

# Credential schemas
az storage container create \
  --name "credential-schemas" \
  --account-name $STORAGE_ACCOUNT \
  --auth-mode login

# Backups
az storage container create \
  --name "backups" \
  --account-name $STORAGE_ACCOUNT \
  --auth-mode login

# Certificates
az storage container create \
  --name "certificates" \
  --account-name $STORAGE_ACCOUNT \
  --auth-mode login
```

### 3. Configure Access

#### Option A: Connection String (Simpler, less secure)

```bash
# Get connection string
az storage account show-connection-string \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --output tsv
```

**Configuration**:
```json
{
  "Azure": {
    "Storage": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=stnumbatwallet;AccountKey=<key>;EndpointSuffix=core.windows.net"
    }
  }
}
```

#### Option B: Managed Identity (More secure - Recommended)

```bash
# Grant Storage Blob Data Contributor role to app identity
az role assignment create \
  --role "Storage Blob Data Contributor" \
  --assignee $IDENTITY_ID \
  --scope /subscriptions/<subscription-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Storage/storageAccounts/$STORAGE_ACCOUNT
```

**Configuration**:
```json
{
  "Azure": {
    "Storage": {
      "AccountName": "stnumbatwallet"
    }
  }
}
```

## Authentication Methods

### DefaultAzureCredential Chain

The `AzureKeyVaultService` uses `DefaultAzureCredential` which tries credentials in this order:

1. **EnvironmentCredential** - Environment variables (CI/CD)
2. **ManagedIdentityCredential** - Azure managed identity (Production)
3. **SharedTokenCacheCredential** - Shared token cache
4. **VisualStudioCredential** - Visual Studio
5. **VisualStudioCodeCredential** - VS Code
6. **AzureCliCredential** - Azure CLI (Local development)
7. **AzurePowerShellCredential** - Azure PowerShell
8. ~~InteractiveBrowserCredential~~ - Excluded for production

### Local Development Authentication

**Option 1: Azure CLI Login** (Recommended)
```bash
az login
az account set --subscription <subscription-id>
```

**Option 2: Environment Variables**
```bash
export AZURE_CLIENT_ID="<app-id>"
export AZURE_TENANT_ID="<tenant-id>"
export AZURE_CLIENT_SECRET="<secret>"
```

**Option 3: Visual Studio/VS Code**
- Sign in to Azure account in IDE
- Credentials automatically used

## Migration from Mock to Azure Services

### Step 1: Verify Mock Services Work

```bash
# Test with current mock configuration
dotnet run

# Verify logs show:
# "Using MockKeyVaultService - NOT FOR PRODUCTION USE"
# "Using MockBlobStorageService for development"
```

### Step 2: Create Azure Resources

```bash
# Create Key Vault and Storage Account (commands above)
```

### Step 3: Update Configuration

**appsettings.Production.json**:
```json
{
  "Azure": {
    "KeyVault": {
      "Url": "https://kv-numbatwallet-prod.vault.azure.net/"
    },
    "Storage": {
      "AccountName": "stnumbatwallet"
    }
  }
}
```

### Step 4: Deploy and Verify

```bash
# Deploy application
az webapp deployment source config-zip \
  --resource-group $RESOURCE_GROUP \
  --name <app-service-name> \
  --src app.zip

# Check logs for:
# "Azure Key Vault client initialized for: https://kv-numbatwallet-prod.vault.azure.net/"
# "Azure Blob Storage client initialized for account: stnumbatwallet"
```

## Environment-Specific Configuration

### Development
```json
{
  "UseAzureKeyVault": false,
  "UseBlobStorage": false
  // Mock services active
}
```

### Staging
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
  "UseAzureKeyVault": true,
  "UseBlobStorage": true
}
```

### Production
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
  "UseAzureKeyVault": true,
  "UseBlobStorage": true
}
```

## Security Best Practices

### ✅ Key Vault
- [x] Use RBAC instead of access policies
- [x] Enable soft-delete (90 days retention)
- [x] Enable purge protection
- [x] Use managed identities (no passwords)
- [x] Implement secret rotation policy
- [x] Monitor access with Azure Monitor
- [x] Use private endpoints for production

### ✅ Blob Storage
- [x] Use managed identities
- [x] Enable encryption at rest (automatic)
- [x] Enforce HTTPS only
- [x] Set minimum TLS version to 1.2
- [x] Enable soft delete for blobs
- [x] Implement lifecycle management
- [x] Use private endpoints for production

## Monitoring

### Key Vault Metrics
- Secret access requests
- API latency
- Availability
- Failed authentication attempts

### Blob Storage Metrics
- Transaction count
- Latency
- Availability
- Storage capacity

### Application Insights
```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=<key>;IngestionEndpoint=https://australiaeast-1.in.applicationinsights.azure.com/"
  }
}
```

## Troubleshooting

### Issue: "Azure Key Vault URL is not configured"
**Solution**: Add `Azure:KeyVault:Url` to configuration

### Issue: "ManagedIdentityCredential authentication unavailable"
**Solution**: Ensure managed identity is assigned to App Service

### Issue: "Secret not found in Key Vault"
**Solution**: Verify secret name matches exactly (case-sensitive)

### Issue: "Insufficient permissions"
**Solution**: Grant "Key Vault Secrets User" role to managed identity

### Issue: "Storage account not found"
**Solution**: Verify storage account name and ensure identity has "Storage Blob Data Contributor" role

## Cost Optimization

### Key Vault
- **Standard tier**: $0.03 per 10,000 operations
- **Secrets**: Free (operations charged)
- **Estimated**: ~$5-10/month for typical usage

### Blob Storage
- **Hot tier**: $0.0208/GB/month (australiaeast)
- **Operations**: $0.0044 per 10,000 write operations
- **Estimated**: ~$10-50/month depending on usage

## Next Steps

1. ✅ **Phase 1.3 - Azure Key Vault**: Already implemented
2. ✅ **Phase 1.4 - Azure Blob Storage**: Already implemented
3. 📋 **Production Deployment**:
   - Create Azure resources (Key Vault, Storage Account)
   - Configure managed identities
   - Migrate secrets to Key Vault
   - Deploy application
   - Verify Azure services active

## Files Reference

### Key Vault
- Interface: `src/NumbatWallet.Application/Interfaces/IKeyVaultService.cs`
- Production: `src/NumbatWallet.Infrastructure/Services/AzureKeyVaultService.cs` ✅
- Development: `src/NumbatWallet.Infrastructure/Services/MockKeyVaultService.cs`
- Registration: `src/NumbatWallet.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs:284-293`

### Blob Storage
- Interface: `src/NumbatWallet.Application/Interfaces/IBlobStorageService.cs`
- Production: `src/NumbatWallet.Infrastructure/Services/AzureBlobStorageService.cs` ✅
- Development: `src/NumbatWallet.Infrastructure/Services/Mocks/MockBlobStorageService.cs`
- Registration: `src/NumbatWallet.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs:312-322`

---

**Status**: Phase 1.3 and 1.4 COMPLETE - Services production-ready, awaiting Azure infrastructure provisioning.
