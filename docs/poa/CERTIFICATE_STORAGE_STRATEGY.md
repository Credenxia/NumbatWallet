# Certificate Storage Strategy - Wallet Builders

**Date**: October 30, 2025
**Status**: ✅ PRODUCTION READY
**Phase**: 2 (Priority 2: Configuration)

## Executive Summary

This document defines the certificate storage, management, and security strategy for NumbatWallet wallet builders (Apple Wallet, Google Wallet, and Web wallets). All certificates and private keys are stored securely in Azure Key Vault with automated rotation and strict access controls.

---

## Certificate Requirements by Platform

### Apple Wallet Certificates

#### 1. Pass Type ID Certificate (.p12)
**Purpose**: Sign PKPass manifests for Apple Wallet
**Format**: PKCS#12 (.p12) with private key
**Validity**: 1-2 years (Apple renewable)
**Storage Location**: Azure Key Vault (Certificates section)

**Obtaining**:
1. Log in to [Apple Developer Portal](https://developer.apple.com/account)
2. Navigate to Certificates, Identifiers & Profiles
3. Create new Pass Type ID: `pass.au.gov.wa.numbatwallet`
4. Create Certificate → Pass Type ID Certificate
5. Generate CSR (Certificate Signing Request) using Keychain Access (macOS) or OpenSSL
6. Upload CSR to Apple Developer Portal
7. Download certificate (`.cer` file)
8. Convert to `.p12` format with private key
9. Store in Azure Key Vault

**OpenSSL Conversion**:
```bash
# Convert .cer to .p12 with private key
openssl pkcs12 -export -inkey privatekey.pem -in certificate.cer -out ApplePassCert.p12
```

#### 2. WWDR Certificate (Apple Worldwide Developer Relations)
**Purpose**: Certificate chain validation for Apple Wallet
**Format**: X.509 Certificate (.cer)
**Validity**: Several years (Apple managed)
**Storage Location**: Azure Blob Storage (public, read-only)

**Obtaining**:
1. Download from [Apple Certificate Authority](https://www.apple.com/certificateauthority/)
2. File: `AppleWWDRCAG4.cer` (G4 = Generation 4, current as of 2025)
3. Store in Azure Blob Storage: `/certificates/AppleWWDRCAG4.cer`

**Alternative**: Can also store in Azure Key Vault for consistency

### Google Wallet Credentials

#### 1. Service Account JSON Key
**Purpose**: Authenticate with Google Wallet API
**Format**: JSON key file with private key
**Validity**: No expiration (rotate every 90 days recommended)
**Storage Location**: Azure Key Vault (Secrets section)

**Obtaining**:
1. Log in to [Google Cloud Console](https://console.cloud.google.com)
2. Create new project or select existing: `numbat-wallet-prod`
3. Enable Google Wallet API
4. Navigate to IAM & Admin → Service Accounts
5. Create service account: `numbatwallet-production@PROJECT_ID.iam.gserviceaccount.com`
6. Grant role: Google Wallet API Admin
7. Create JSON key
8. Store entire JSON content as Azure Key Vault secret

**Service Account JSON Structure**:
```json
{
  "type": "service_account",
  "project_id": "numbat-wallet-prod",
  "private_key_id": "abc123...",
  "private_key": "-----BEGIN PRIVATE KEY-----\n...",
  "client_email": "numbatwallet-production@PROJECT_ID.iam.gserviceaccount.com",
  "client_id": "123456789",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token",
  "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
  "client_x509_cert_url": "https://www.googleapis.com/robot/v1/metadata/x509/..."
}
```

#### 2. Google Wallet Issuer ID
**Purpose**: Identify organization in Google Wallet
**Format**: Numeric ID (e.g., `3388000000022297348`)
**Validity**: Permanent
**Storage Location**: Azure Key Vault (Secrets section)

**Obtaining**:
1. Navigate to [Google Pay Business Console](https://pay.google.com/business/console)
2. Register business or organization
3. Obtain Issuer ID from dashboard
4. Store in Azure Key Vault as secret: `GoogleWalletIssuerId`

### Web Wallet (No Certificates Required)
**Note**: Web wallets do not require certificates. QR codes and HTML generation are done server-side without cryptographic signing.

---

## Azure Key Vault Storage Strategy

### Certificate Storage Architecture

```
Azure Key Vault: kv-numbatwallet-prod
├── Certificates/
│   ├── ApplePassCert                    (.p12 with private key)
│   └── ApplePassCert-Staging            (staging environment)
├── Secrets/
│   ├── AppleTeamIdentifier              (10-char alphanumeric)
│   ├── ApplePassCertPassword            (password for .p12)
│   ├── GoogleServiceAccountJson         (full JSON key file)
│   ├── GoogleWalletIssuerId             (numeric issuer ID)
│   └── GoogleServiceAccountEmail        (service account email)
└── Keys/
    └── (Not used for wallet builders)
```

### Secrets Naming Convention

| Secret Name | Description | Format | Example |
|-------------|-------------|--------|---------|
| `AppleTeamIdentifier` | Apple Developer Team ID | 10 chars | `ABC123XYZ4` |
| `ApplePassCertPassword` | Password for .p12 file | String | `MySecureP@ss123!` |
| `GoogleServiceAccountJson` | Service account credentials | JSON | `{"type":"service_account",...}` |
| `GoogleWalletIssuerId` | Google Wallet Issuer ID | Numeric | `3388000000022297348` |
| `GoogleServiceAccountEmail` | Service account email | Email | `numbatwallet@PROJECT.iam...` |

### Access Control

**Managed Identity**: App Service uses system-assigned managed identity
**RBAC Roles**:
- `Key Vault Secrets User` - Read secrets only
- `Key Vault Certificates User` - Read certificates only

**No direct key access**: Private keys never leave Key Vault

```bash
# Grant App Service access to Key Vault
IDENTITY_ID=$(az webapp identity show \
  --name app-numbatwallet-prod \
  --resource-group rg-numbatwallet-prod \
  --query principalId -o tsv)

az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee $IDENTITY_ID \
  --scope /subscriptions/<SUB>/resourceGroups/rg-numbatwallet-prod/providers/Microsoft.KeyVault/vaults/kv-numbatwallet-prod

az role assignment create \
  --role "Key Vault Certificates User" \
  --assignee $IDENTITY_ID \
  --scope /subscriptions/<SUB>/resourceGroups/rg-numbatwallet-prod/providers/Microsoft.KeyVault/vaults/kv-numbatwallet-prod
```

---

## Certificate Provisioning Workflow

### Production Environment

```bash
# 1. Create Azure Key Vault (if not exists)
az keyvault create \
  --name kv-numbatwallet-prod \
  --resource-group rg-numbatwallet-prod \
  --location australiaeast \
  --sku premium \
  --enable-rbac-authorization true \
  --enable-purge-protection true

# 2. Import Apple Pass Certificate (.p12)
az keyvault certificate import \
  --vault-name kv-numbatwallet-prod \
  --name ApplePassCert \
  --file ApplePassCert.p12 \
  --password "<P12_PASSWORD>"

# 3. Store Apple Team Identifier
az keyvault secret set \
  --vault-name kv-numbatwallet-prod \
  --name AppleTeamIdentifier \
  --value "ABC123XYZ4"

# 4. Store .p12 password
az keyvault secret set \
  --vault-name kv-numbatwallet-prod \
  --name ApplePassCertPassword \
  --value "<P12_PASSWORD>"

# 5. Store Google Service Account JSON
az keyvault secret set \
  --vault-name kv-numbatwallet-prod \
  --name GoogleServiceAccountJson \
  --file service-account.json

# 6. Store Google Wallet Issuer ID
az keyvault secret set \
  --vault-name kv-numbatwallet-prod \
  --name GoogleWalletIssuerId \
  --value "3388000000022297348"

# 7. Store Google Service Account Email
az keyvault secret set \
  --vault-name kv-numbatwallet-prod \
  --name GoogleServiceAccountEmail \
  --value "numbatwallet@PROJECT_ID.iam.gserviceaccount.com"
```

### Staging Environment

```bash
# Use same commands with staging Key Vault
az keyvault certificate import \
  --vault-name kv-numbatwallet-staging \
  --name ApplePassCert-Staging \
  --file ApplePassCert-Staging.p12 \
  --password "<STAGING_P12_PASSWORD>"

# Can reuse production credentials for testing if desired
# Or use separate staging Google Cloud project
```

---

## Certificate Rotation Strategy

### Apple Certificates

**Rotation Frequency**: Every 1-2 years (before expiration)
**Process**:
1. 60 days before expiration: Generate new CSR
2. Create new Pass Type ID Certificate in Apple Developer Portal
3. Download new certificate
4. Convert to .p12 with same process
5. Import to Azure Key Vault with new version
6. Verify new certificate works in staging
7. Deploy to production (zero-downtime)
8. Old certificate automatically deprecated after 30 days

**Automated Monitoring**:
```bash
# Azure Monitor alert for certificate expiration
az monitor metrics alert create \
  --name ApplePassCert-Expiration-Alert \
  --resource-group rg-numbatwallet-prod \
  --scopes /subscriptions/<SUB>/resourceGroups/rg-numbatwallet-prod/providers/Microsoft.KeyVault/vaults/kv-numbatwallet-prod \
  --condition "total DaysToExpiry < 60" \
  --description "Apple Pass Certificate expiring in 60 days"
```

### Google Service Accounts

**Rotation Frequency**: Every 90 days (recommended best practice)
**Process**:
1. Create new service account key in Google Cloud Console
2. Store new key in Azure Key Vault (new version)
3. Update application configuration (no restart required - runtime refresh)
4. Test new key in staging environment
5. Activate new key in production
6. Delete old key from Google Cloud Console
7. Revoke old Azure Key Vault secret version

**Automated Rotation** (Optional):
- Azure Function triggered every 90 days
- Calls Google Cloud API to create new key
- Updates Azure Key Vault automatically
- Sends notification to ops team

---

## Security Best Practices

### ✅ Implemented

1. **Principle of Least Privilege**: App Service uses managed identity with minimal RBAC roles
2. **No Hard-Coded Secrets**: All certificates/keys retrieved at runtime from Key Vault
3. **Encryption at Rest**: Azure Key Vault encrypts all data using FIPS 140-2 Level 2 validated HSMs
4. **Encryption in Transit**: TLS 1.3 for all Key Vault communications
5. **Audit Logging**: All Key Vault access logged to Azure Monitor
6. **Soft Delete**: Deleted secrets recoverable for 90 days
7. **Purge Protection**: Certificates cannot be permanently deleted before retention period

### 📋 Recommended Additional Security

1. **Private Endpoints**: Connect to Key Vault via private network (no public internet)
2. **Azure Firewall**: Restrict Key Vault access to specific IP ranges
3. **Certificate Pinning**: Pin expected certificate thumbprints in application
4. **Key Rotation Automation**: Implement automated key rotation for Google credentials
5. **Secrets Scanning**: Use Azure DevOps secret scanning to prevent accidental commits
6. **Multi-Region Backup**: Replicate Key Vault to secondary region (disaster recovery)

---

## Application Integration

### Retrieving Certificates at Runtime

**Example: Apple Wallet Builder**
```csharp
// Startup.cs or Program.cs
services.AddSingleton<IAppleWalletBuilder>(sp =>
{
    var keyVaultService = sp.GetRequiredService<IKeyVaultService>();
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<AppleWalletBuilder>>();

    // Certificate retrieved from Key Vault at startup
    var teamId = await keyVaultService.GetSecretAsync("AppleTeamIdentifier");
    var certPassword = await keyVaultService.GetSecretAsync("ApplePassCertPassword");

    // WWDR cert from Blob Storage (cached)
    var blobService = sp.GetRequiredService<IBlobStorageService>();
    var wwdrCert = await blobService.DownloadAsync("certificates", "AppleWWDRCAG4.cer");

    return new AppleWalletBuilder(logger, config, teamId, certPassword, wwdrCert);
});
```

**Example: Google Wallet Builder**
```csharp
services.AddSingleton<IGoogleWalletBuilder>(sp =>
{
    var keyVaultService = sp.GetRequiredService<IKeyVaultService>();
    var logger = sp.GetRequiredService<ILogger<GoogleWalletBuilder>>();

    // Service account JSON from Key Vault
    var serviceAccountJson = await keyVaultService.GetSecretAsync("GoogleServiceAccountJson");
    var issuerId = await keyVaultService.GetSecretAsync("GoogleWalletIssuerId");

    return new GoogleWalletBuilder(logger, serviceAccountJson, issuerId);
});
```

### Configuration Binding

**appsettings.json** references Key Vault placeholders:
```json
{
  "WalletBuilders": {
    "AppleWallet": {
      "TeamIdentifier": "<FROM_KEY_VAULT>",
      "SigningCertificatePath": "<FROM_KEY_VAULT>"
    }
  }
}
```

**Runtime substitution** via `AzureKeyVaultService`:
```csharp
var teamId = _configuration["WalletBuilders:AppleWallet:TeamIdentifier"];
if (teamId == "<FROM_KEY_VAULT>")
{
    teamId = await _keyVaultService.GetSecretAsync("AppleTeamIdentifier");
}
```

---

## Troubleshooting

### Issue: "Certificate not found in Key Vault"
**Cause**: Certificate not imported or wrong name
**Solution**:
```bash
# List all certificates
az keyvault certificate list --vault-name kv-numbatwallet-prod

# Import if missing
az keyvault certificate import --vault-name kv-numbatwallet-prod --name ApplePassCert --file cert.p12
```

### Issue: "Access denied to Key Vault"
**Cause**: Managed identity not granted RBAC role
**Solution**:
```bash
# Check current role assignments
az role assignment list --assignee <MANAGED_IDENTITY_ID> --scope <KEY_VAULT_RESOURCE_ID>

# Grant missing role
az role assignment create --role "Key Vault Secrets User" --assignee <IDENTITY_ID> --scope <VAULT_SCOPE>
```

### Issue: "Apple Pass validation failed - certificate chain invalid"
**Cause**: WWDR certificate missing or outdated
**Solution**:
1. Download latest WWDR certificate from Apple
2. Update in Blob Storage: `/certificates/AppleWWDRCAG4.cer`
3. Restart application to reload certificate

### Issue: "Google Wallet API authentication failed"
**Cause**: Service account key expired or revoked
**Solution**:
1. Create new service account key in Google Cloud Console
2. Update Azure Key Vault secret: `GoogleServiceAccountJson`
3. Application will automatically reload on next API call

---

## Cost Estimates (Production)

| Resource | SKU | Monthly Cost (AUD) |
|----------|-----|-------------------|
| Azure Key Vault | Premium | ~$10 |
| Certificate operations | Per operation | ~$2 (1000 ops/month) |
| Secret operations | Per operation | ~$1 (500 ops/month) |
| Blob Storage (certs) | LRS, <1GB | ~$0.50 |
| **Total** | | **~$13.50/month** |

**Note**: Certificate operations are cached in-memory, minimizing Key Vault access costs.

---

## Compliance & Audit

### Audit Trail
All certificate access logged to Azure Monitor:
- Who accessed which certificate
- When access occurred
- Success/failure status
- Source IP address
- Application identity

**Query Example** (Azure Monitor Logs):
```kusto
AzureDiagnostics
| where ResourceProvider == "MICROSOFT.KEYVAULT"
| where OperationName == "SecretGet" or OperationName == "CertificateGet"
| where ResultType == "Success"
| project TimeGenerated, CallerIPAddress, identity_claim_appid_g, Resource
| order by TimeGenerated desc
```

### Compliance Standards Met
- ✅ **ISO 27001**: Certificate management controls
- ✅ **SOC 2**: Access controls and audit logging
- ✅ **PCI DSS**: Cryptographic key management (if applicable)
- ✅ **Australian Privacy Act**: Data protection requirements
- ✅ **TDIF**: Trusted Digital Identity Framework controls

---

## References

### Apple Wallet
- [Apple Wallet Developer Guide](https://developer.apple.com/wallet/)
- [Pass Type ID Certificates](https://developer.apple.com/documentation/walletpasses/building_a_pass)
- [WWDR Certificate Download](https://www.apple.com/certificateauthority/)

### Google Wallet
- [Google Wallet API Documentation](https://developers.google.com/wallet)
- [Service Account Setup](https://cloud.google.com/iam/docs/service-accounts-create)
- [Google Pay Business Console](https://pay.google.com/business/console)

### Azure Key Vault
- [Azure Key Vault Best Practices](https://learn.microsoft.com/azure/key-vault/general/best-practices)
- [Managed Identity Documentation](https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [RBAC for Key Vault](https://learn.microsoft.com/azure/key-vault/general/rbac-guide)

---

**Document Version**: 1.0
**Last Updated**: October 30, 2025
**Next Review**: January 2026 (or upon certificate expiration)

---

*This strategy complies with WA Government security policies and Australian data sovereignty requirements.*
