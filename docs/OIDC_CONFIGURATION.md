# OIDC Authentication Configuration Guide

## Overview
NumbatWallet supports two OIDC authentication providers:
1. **Azure AD / Entra ID** - For government officers and administrators
2. **ServiceWA** - For citizens accessing their digital wallets

## Current Status
- **Development**: Uses MockWAIdXService (no external dependencies)
- **Production**: Requires real OIDC configuration

## Configuration Requirements

### 1. Enable Real Authentication
Set in `appsettings.json` or environment variables:
```json
{
  "Authentication": {
    "UseRealAuthentication": true
  }
}
```

### 2. Azure AD / Entra ID Configuration
Required settings for government officer authentication:
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "wa.gov.au",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "CallbackPath": "/signin-oidc",
    "Audience": "api://numbatwallet"
  }
}
```

### 3. ServiceWA Configuration
Required settings for citizen authentication:
```json
{
  "ServiceWA": {
    "Authority": "https://auth.servicewa.wa.gov.au",
    "ClientId": "numbat-wallet",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "MetadataAddress": "https://auth.servicewa.wa.gov.au/.well-known/openid-configuration",
    "ResponseType": "code",
    "Scope": "openid profile email waid",
    "CallbackPath": "/signin-servicewa"
  }
}
```

## Required Azure Resources
1. **App Registration** in Azure AD
   - Redirect URI: `https://YOUR_DOMAIN/signin-oidc`
   - API permissions: User.Read, profile, email
   - Client secret or certificate

2. **ServiceWA Integration**
   - Register application with ServiceWA
   - Obtain client ID and secret
   - Configure allowed redirect URIs

## Environment Variables
For production deployments, use environment variables:
```bash
# Azure AD
export AzureAd__TenantId="YOUR_TENANT_ID"
export AzureAd__ClientId="YOUR_CLIENT_ID"
export AzureAd__ClientSecret="YOUR_CLIENT_SECRET"

# ServiceWA
export ServiceWA__ClientId="YOUR_CLIENT_ID"
export ServiceWA__ClientSecret="YOUR_CLIENT_SECRET"

# Enable real authentication
export Authentication__UseRealAuthentication="true"
```

## Testing Authentication
1. **Development Mode** (Mock):
   - No configuration needed
   - Uses `MockWAIdXService`
   - Returns test users and claims

2. **Integration Testing**:
   - Set `Authentication:UseRealAuthentication` to `false`
   - Mock service automatically provides test identities

3. **Production Testing**:
   - Ensure all OIDC endpoints are accessible
   - Verify redirect URIs are registered
   - Test both Azure AD and ServiceWA flows

## Security Considerations
1. **Never commit secrets** to source control
2. Use **Azure Key Vault** for production secrets
3. Rotate client secrets regularly
4. Use certificate authentication for production Azure AD
5. Implement proper **PKCE flow** for public clients
6. Enable **multi-factor authentication** for admin users

## Troubleshooting
Common issues and solutions:

### Issue: "AADSTS700054: response_type 'id_token' is not enabled"
**Solution**: Enable ID tokens in App Registration authentication settings

### Issue: "Invalid redirect URI"
**Solution**: Ensure redirect URI exactly matches registered URI including protocol and path

### Issue: "Unauthorized client"
**Solution**: Verify client ID and secret are correct and not expired

### Issue: "ServiceWA authentication fails"
**Solution**: Check ServiceWA is configured for your client ID and redirect URIs match

## Migration Path
1. Start with mock authentication in development
2. Configure Azure AD for internal testing
3. Add ServiceWA configuration for citizen testing
4. Enable real authentication in staging
5. Deploy to production with full OIDC configuration

## Related Files
- `/src/NumbatWallet.Web.Api/Authentication/OidcAuthenticationExtensions.cs` - OIDC setup
- `/src/NumbatWallet.Infrastructure/Services/MockWAIdXService.cs` - Mock implementation
- `/src/NumbatWallet.Web.Api/appsettings.Development.json` - Dev configuration
- `/src/NumbatWallet.Web.Api/Program.cs` - Authentication registration

## Contacts
- Azure AD Support: [Azure portal](https://portal.azure.com)
- ServiceWA Integration: Contact WA government IT services
- Internal Support: DevOps team

---
*Last Updated: September 2025*
*Version: 1.0*