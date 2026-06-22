# Certificate Installation Guide - NumbatWallet

**Date**: October 30, 2025
**Purpose**: Instructions for installing existing Apple and Google certificates
**Status**: Ready for certificate provisioning

---

## Quick Start - Where to Put Your Certificates

You mentioned you already have the certificates. Here's exactly where to put them:

### Option 1: Local Development Setup (Recommended for Testing)

**For immediate testing**, store certificates locally:

```bash
# Create certificates directory
mkdir -p /Users/rodrigolmiranda/repo/NumbatWallet/certificates

# Place your Apple certificates here:
/Users/rodrigolmiranda/repo/NumbatWallet/certificates/
├── ApplePassCert.p12              # Your Apple Pass Type ID certificate (.p12)
├── AppleWWDRCAG4.cer              # Apple WWDR Certificate (download if missing)
└── README.txt                     # Certificate notes (optional)

# Place your Google credentials here:
/Users/rodrigolmiranda/repo/NumbatWallet/certificates/
└── google-service-account.json    # Your Google Cloud service account JSON
```

**Then update `appsettings.Development.json`**:
```json
{
  "WalletBuilders": {
    "AppleWallet": {
      "TeamIdentifier": "YOUR_TEAM_ID_HERE",
      "PassTypeIdentifier": "pass.au.gov.wa.numbatwallet",
      "OrganizationName": "Government of Western Australia",
      "WwdrCertificatePath": "/Users/rodrigolmiranda/repo/NumbatWallet/certificates/AppleWWDRCAG4.cer",
      "SigningCertificatePath": "/Users/rodrigolmiranda/repo/NumbatWallet/certificates/ApplePassCert.p12",
      "SigningCertificatePassword": "YOUR_P12_PASSWORD_HERE"
    },
    "GoogleWallet": {
      "ServiceAccountEmail": "FROM_JSON_FILE",
      "PrivateKey": "FROM_JSON_FILE",
      "IssuerId": "YOUR_ISSUER_ID_HERE"
    }
  }
}
```

### Option 2: Azure Key Vault (Production)

**For production deployment**, use Azure Key Vault:

```bash
# Import Apple certificate to Key Vault
az keyvault certificate import \
  --vault-name kv-numbatwallet-prod \
  --name ApplePassCert \
  --file /path/to/ApplePassCert.p12 \
  --password "YOUR_P12_PASSWORD"

# Store Apple secrets
az keyvault secret set \
  --vault-name kv-numbatwallet-prod \
  --name AppleTeamIdentifier \
  --value "YOUR_TEAM_ID"

az keyvault secret set \
  --vault-name kv-numbatwallet-prod \
  --name ApplePassCertPassword \
  --value "YOUR_P12_PASSWORD"

# Store Google credentials
az keyvault secret set \
  --vault-name kv-numbatwallet-prod \
  --name GoogleServiceAccountJson \
  --file /path/to/google-service-account.json

az keyvault secret set \
  --vault-name kv-numbatwallet-prod \
  --name GoogleWalletIssuerId \
  --value "YOUR_ISSUER_ID"
```

---

## Detailed Instructions

### Step 1: Apple Certificates

#### What You Should Have

1. **Pass Type ID Certificate (.p12)**
   - File format: `.p12` (PKCS#12)
   - Contains: Certificate + Private Key
   - Password protected
   - From: Apple Developer Portal

2. **Team Identifier**
   - Format: 10-character alphanumeric (e.g., `ABC123XYZ4`)
   - From: Apple Developer Portal → Membership Details

3. **WWDR Certificate** (if you don't have it):
   - Download: https://www.apple.com/certificateauthority/
   - File: `AppleWWDRCAG4.cer` (G4 = Generation 4)
   - No password required
   - Public certificate (can share)

#### Installation Steps

**A. Local Development**:

```bash
# 1. Create certificates directory
mkdir -p /Users/rodrigolmiranda/repo/NumbatWallet/certificates

# 2. Copy your Apple .p12 certificate
cp /path/to/your/ApplePassCert.p12 /Users/rodrigolmiranda/repo/NumbatWallet/certificates/

# 3. Download WWDR certificate (if you don't have it)
curl -o /Users/rodrigolmiranda/repo/NumbatWallet/certificates/AppleWWDRCAG4.cer \
  https://www.apple.com/certificateauthority/AppleWWDRCAG4.cer

# 4. Verify files exist
ls -lh /Users/rodrigolmiranda/repo/NumbatWallet/certificates/
```

**B. Update Configuration**:

Edit `src/NumbatWallet.Web.Api/appsettings.Development.json`:

```json
{
  "WalletBuilders": {
    "AppleWallet": {
      "TeamIdentifier": "ABC123XYZ4",  // ← YOUR TEAM ID HERE
      "PassTypeIdentifier": "pass.au.gov.wa.numbatwallet",
      "OrganizationName": "Government of Western Australia",
      "WwdrCertificatePath": "/Users/rodrigolmiranda/repo/NumbatWallet/certificates/AppleWWDRCAG4.cer",
      "SigningCertificatePath": "/Users/rodrigolmiranda/repo/NumbatWallet/certificates/ApplePassCert.p12",
      "SigningCertificatePassword": "YOUR_P12_PASSWORD"  // ← YOUR CERTIFICATE PASSWORD
    }
  }
}
```

**C. Test Certificate**:

```bash
# Verify certificate can be loaded
openssl pkcs12 -info -in /Users/rodrigolmiranda/repo/NumbatWallet/certificates/ApplePassCert.p12
# Enter password when prompted
# Should show certificate details
```

---

### Step 2: Google Wallet Credentials

#### What You Should Have

1. **Service Account JSON Key**
   - File format: `.json`
   - Contains: Private key, project ID, client email
   - From: Google Cloud Console

2. **Issuer ID**
   - Format: Numeric (e.g., `3388000000022297348`)
   - From: Google Pay Business Console

#### Installation Steps

**A. Local Development**:

```bash
# 1. Copy your Google service account JSON
cp /path/to/your/service-account.json /Users/rodrigolmiranda/repo/NumbatWallet/certificates/google-service-account.json

# 2. Verify JSON structure
cat /Users/rodrigolmiranda/repo/NumbatWallet/certificates/google-service-account.json | jq .
# Should show: type, project_id, private_key_id, private_key, client_email, etc.
```

**B. Extract Information from JSON**:

```bash
# Extract service account email
cat /Users/rodrigolmiranda/repo/NumbatWallet/certificates/google-service-account.json | jq -r .client_email

# Example output: numbatwallet-prod@PROJECT_ID.iam.gserviceaccount.com
```

**C. Update Configuration**:

Edit `src/NumbatWallet.Web.Api/appsettings.Development.json`:

```json
{
  "WalletBuilders": {
    "GoogleWallet": {
      "ServiceAccountEmail": "numbatwallet-prod@PROJECT_ID.iam.gserviceaccount.com",  // ← FROM JSON
      "PrivateKeyPath": "/Users/rodrigolmiranda/repo/NumbatWallet/certificates/google-service-account.json",  // ← JSON FILE PATH
      "IssuerId": "3388000000022297348",  // ← YOUR ISSUER ID
      "DefaultBackgroundColor": "#003087"
    }
  }
}
```

---

### Step 3: Add Certificates Directory to .gitignore

**IMPORTANT**: Never commit certificates to git!

```bash
# Add to .gitignore
echo "/certificates/" >> .gitignore
echo "*.p12" >> .gitignore
echo "*service-account*.json" >> .gitignore
echo "*.cer" >> .gitignore

# Verify certificates are ignored
git status | grep certificates
# Should show nothing (files ignored)
```

---

### Step 4: Verify Installation

#### Test Apple Wallet Builder

```bash
# Run Apple Wallet builder tests
dotnet test src/Tests/NumbatWallet.Infrastructure.Tests/NumbatWallet.Infrastructure.Tests.csproj \
  --filter "FullyQualifiedName~AppleWalletBuilderTests" \
  --logger "console;verbosity=detailed"

# All 24 tests should pass
```

#### Test Google Wallet Builder

```bash
# Run Google Wallet builder tests
dotnet test src/Tests/NumbatWallet.Infrastructure.Tests/NumbatWallet.Infrastructure.Tests.csproj \
  --filter "FullyQualifiedName~GoogleWalletBuilderTests" \
  --logger "console;verbosity=detailed"

# All 23 tests should pass
```

#### Test Actual Wallet Generation (Manual)

Create a test script: `test-wallet-generation.sh`:

```bash
#!/bin/bash

# Test Apple Wallet generation
curl -X POST http://localhost:7000/api/v1/wallets/generate/apple \
  -H "Content-Type: application/json" \
  -d '{
    "templateId": "test-template-id",
    "data": {
      "name": "John Doe",
      "dateOfBirth": "1990-01-01"
    }
  }'

# Test Google Wallet generation
curl -X POST http://localhost:7000/api/v1/wallets/generate/google \
  -H "Content-Type: application/json" \
  -d '{
    "templateId": "test-template-id",
    "data": {
      "name": "John Doe",
      "dateOfBirth": "1990-01-01"
    }
  }'

# Test Web Wallet generation
curl -X POST http://localhost:7000/api/v1/wallets/generate/web \
  -H "Content-Type: application/json" \
  -d '{
    "templateId": "test-template-id",
    "data": {
      "name": "John Doe",
      "dateOfBirth": "1990-01-01"
    }
  }'
```

```bash
# Make executable and run
chmod +x test-wallet-generation.sh

# Start application
dotnet run --project src/NumbatWallet.Web.Api

# In another terminal, run tests
./test-wallet-generation.sh
```

---

## Certificate Information Checklist

Please provide the following information (you can share this via secure method):

### Apple Wallet
- [ ] **Team Identifier**: `__________` (10 chars)
- [ ] **Pass Type ID**: `pass.au.gov.wa.numbatwallet` (or your registered ID)
- [ ] **Certificate File**: ApplePassCert.p12 (upload to `/certificates/`)
- [ ] **Certificate Password**: `__________` (secure password)
- [ ] **WWDR Certificate**: Download from Apple if missing

### Google Wallet
- [ ] **Service Account Email**: `________@PROJECT_ID.iam.gserviceaccount.com`
- [ ] **Service Account JSON**: Upload to `/certificates/google-service-account.json`
- [ ] **Issuer ID**: `__________` (numeric, 16+ digits)
- [ ] **Project ID**: `__________` (from Google Cloud)

---

## How to Share Certificates with Claude Code

### Option 1: Local File Path (Recommended)

If you've already saved the certificates to your machine:

```bash
# Just tell me the file paths, I can read them:
"My Apple certificate is at: /Users/rodrigolmiranda/Downloads/ApplePassCert.p12"
"My Google JSON is at: /Users/rodrigolmiranda/Downloads/service-account.json"

# I can then:
1. Read the files
2. Move them to /certificates/ directory
3. Update configuration with correct values
```

### Option 2: Direct Values

Share the certificate information (NOT the actual private keys):

```
Apple Team Identifier: ABC123XYZ4
Google Issuer ID: 3388000000022297348
Certificate Password: MySecurePassword123!
Service Account Email: numbatwallet@PROJECT.iam.gserviceaccount.com
```

I can then guide you to place the actual certificate files in the right location.

### Option 3: Azure Key Vault (Production)

If you want to use Azure Key Vault directly:

```bash
# I can help you run these commands to upload certificates
az keyvault certificate import --vault-name <VAULT> --name ApplePassCert --file <PATH>
az keyvault secret set --vault-name <VAULT> --name GoogleServiceAccountJson --file <PATH>
```

---

## Security Notes

⚠️ **IMPORTANT**:

1. **Never commit certificates to git**
   - Added `/certificates/` to `.gitignore`
   - Certificate files are ignored by default

2. **Never share private keys publicly**
   - Use Azure Key Vault for production
   - Use environment variables for CI/CD
   - Only share Team IDs and Issuer IDs (these are safe)

3. **Protect .p12 passwords**
   - Store in environment variables
   - Use Azure Key Vault secrets
   - Never hardcode in source code

4. **Certificate expiration**
   - Apple certificates: Valid for 1-2 years
   - Google keys: Rotate every 90 days
   - Set up expiration alerts

---

## Troubleshooting

### Issue: "Certificate file not found"
**Solution**: Verify file path is absolute and correct
```bash
ls -lh /Users/rodrigolmiranda/repo/NumbatWallet/certificates/ApplePassCert.p12
```

### Issue: "Invalid certificate password"
**Solution**: Verify password is correct
```bash
openssl pkcs12 -info -in ApplePassCert.p12
# Enter password - should show certificate details
```

### Issue: "Google service account authentication failed"
**Solution**: Verify JSON structure
```bash
cat google-service-account.json | jq .type
# Should output: "service_account"
```

### Issue: "Team Identifier doesn't match"
**Solution**: Get Team ID from Apple Developer Portal
1. Go to: https://developer.apple.com/account
2. Navigate to: Membership → Team ID
3. Copy the 10-character ID

---

## Next Steps After Certificate Installation

1. ✅ Place certificates in `/certificates/` directory
2. ✅ Update `appsettings.Development.json` with values
3. ✅ Run tests to verify configuration
4. ✅ Test wallet generation endpoints
5. ✅ Validate PKPass files open in Apple Wallet
6. ✅ Validate Google Wallet links work
7. ✅ Deploy to staging environment
8. ✅ Production deployment with Azure Key Vault

---

## Contact & Support

**Ready to provide certificates?**

Just let me know:
1. Where the certificate files are located on your machine
2. What the Team Identifier and Issuer ID values are
3. What the certificate password is

I can then:
- Move the files to the correct location
- Update configuration files with correct values
- Verify the setup works
- Run tests to ensure everything is working

---

**Last Updated**: October 30, 2025
**Status**: Ready for certificate provisioning
**Next**: Waiting for certificate file paths from user
