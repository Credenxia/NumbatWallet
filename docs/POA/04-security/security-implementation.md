# POA Security Implementation Guide

**Version:** 1.0  
**Date:** September 10, 2025  
**Phase:** Proof-of-Operation (POA)

## Table of Contents
- [Overview](#overview)
- [Security Architecture](#security-architecture)
- [Authentication & Authorization](#authentication--authorization)
- [PKI Infrastructure](#pki-infrastructure)
- [Data Protection](#data-protection)
- [Secure Communications](#secure-communications)
- [Key Management](#key-management)
- [Threat Mitigation](#threat-mitigation)
- [Security Testing](#security-testing)
- [Compliance Validation](#compliance-validation)

## Overview

This document outlines the security implementation for the POA phase, establishing a production-ready security foundation that meets WA Government requirements while demonstrating compliance with industry standards.

### Security Objectives
- **Zero Trust Architecture** - Never trust, always verify
- **Defense in Depth** - Multiple security layers
- **Least Privilege** - Minimal access rights
- **Data Sovereignty** - Australian data residency
- **Compliance Ready** - ISO 27001, TDIF alignment

### POA Security Scope
- Authentication and authorization flows
- PKI certificate management
- Encryption at rest and in transit
- Secure credential storage
- Audit logging and monitoring
- Vulnerability assessment

## Security Architecture

### Security Layers

```
┌─────────────────────────────────────────────────────────┐
│                    Azure WAF/DDoS                       │
├─────────────────────────────────────────────────────────┤
│                  API Gateway (APIM)                     │
│               • Rate limiting                           │
│               • API key validation                      │
│               • OAuth 2.0 enforcement                   │
├─────────────────────────────────────────────────────────┤
│              Container Apps Environment                  │
│               • Network isolation                       │
│               • Managed identities                      │
│               • Container security                      │
├─────────────────────────────────────────────────────────┤
│                 Application Layer                       │
│               • JWT validation                          │
│               • RBAC enforcement                        │
│               • Input validation                        │
├─────────────────────────────────────────────────────────┤
│                   Data Layer                            │
│               • Encryption at rest                      │
│               • Row-level security                      │
│               • Connection encryption                   │
└─────────────────────────────────────────────────────────┘
```

### Network Security

```yaml
Virtual Network:
  name: vnet-numbat-poa
  address_space: 10.0.0.0/16
  
  subnets:
    - name: snet-container-apps
      address: 10.0.1.0/24
      nsg_rules:
        - allow_https_inbound: 443
        - deny_all_else: *
    
    - name: snet-database
      address: 10.0.2.0/24
      nsg_rules:
        - allow_postgres: 5432 from snet-container-apps
        - deny_all_else: *
    
    - name: snet-private-endpoints
      address: 10.0.3.0/24
      nsg_rules:
        - allow_keyvault: 443 from snet-container-apps
        - deny_all_else: *

Private Endpoints:
  - Azure Key Vault
  - Azure Storage
  - PostgreSQL Server
```

## Authentication & Authorization

### OIDC Integration

```csharp
// Startup configuration
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://login.wa.gov.au";
        options.Audience = "numbat-wallet-api";
        options.RequireHttpsMetadata = true;
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            RequireExpirationTime = true,
            RequireSignedTokens = true
        };
        
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                logger.LogWarning($"Authentication failed: {context.Exception}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var tenantId = context.Principal.FindFirst("tenant_id")?.Value;
                context.HttpContext.Items["TenantId"] = tenantId;
                return Task.CompletedTask;
            }
        };
    });
```

### Role-Based Access Control

```csharp
public enum SystemRole
{
    SuperAdmin,      // Cross-tenant operations
    TenantAdmin,     // Tenant management
    CredentialIssuer,// Issue credentials
    Auditor,         // Read-only access
    ServiceAccount   // API access
}

[Authorize(Policy = "RequireTenantAdmin")]
public class TenantManagementController : ApiController
{
    [HttpPost("tenants/{tenantId}/users")]
    [RequirePermission(Permission.ManageUsers)]
    public async Task<IActionResult> CreateUser(
        [FromRoute] Guid tenantId,
        [FromBody] CreateUserRequest request)
    {
        // Verify tenant access
        if (!User.HasTenantAccess(tenantId))
            return Forbid();
            
        // Implementation
    }
}
```

### Multi-Factor Authentication

```csharp
public class MfaService : IMfaService
{
    public async Task<MfaChallenge> InitiateMfa(UserId userId, MfaMethod method)
    {
        return method switch
        {
            MfaMethod.Totp => await GenerateTotpChallenge(userId),
            MfaMethod.Sms => await SendSmsChallenge(userId),
            MfaMethod.Email => await SendEmailChallenge(userId),
            MfaMethod.Biometric => await InitiateBiometricChallenge(userId),
            _ => throw new NotSupportedException($"MFA method {method} not supported")
        };
    }
    
    public async Task<bool> VerifyMfa(UserId userId, string code)
    {
        var challenge = await _cache.GetAsync<MfaChallenge>($"mfa:{userId}");
        if (challenge == null || challenge.IsExpired)
            return false;
            
        var isValid = challenge.Method switch
        {
            MfaMethod.Totp => VerifyTotp(userId, code),
            MfaMethod.Sms => challenge.Code == code,
            MfaMethod.Email => challenge.Code == code,
            _ => false
        };
        
        if (isValid)
        {
            await _auditLog.LogMfaSuccess(userId, challenge.Method);
            await _cache.RemoveAsync($"mfa:{userId}");
        }
        else
        {
            await _auditLog.LogMfaFailure(userId, challenge.Method);
        }
        
        return isValid;
    }
}
```

## PKI Infrastructure

### Certificate Hierarchy

```
Root CA (Offline)
├── Intermediate CA (Azure Key Vault)
│   ├── Credential Signing Certificate
│   ├── Document Signing Certificate
│   └── Device Certificates
└── Recovery CA (Cold Storage)
```

### Certificate Management

```csharp
public class PkiService : IPkiService
{
    private readonly IKeyVaultService _keyVault;
    private readonly ICertificateStore _certStore;
    
    public async Task<SignedCredential> SignCredential(
        CredentialDocument document,
        SigningAlgorithm algorithm = SigningAlgorithm.ES256)
    {
        // Get signing certificate
        var certificate = await _keyVault.GetCertificateAsync("credential-signing");
        
        // Create signature
        var signature = algorithm switch
        {
            SigningAlgorithm.ES256 => await SignWithES256(document, certificate),
            SigningAlgorithm.RS256 => await SignWithRS256(document, certificate),
            SigningAlgorithm.EdDSA => await SignWithEdDSA(document, certificate),
            _ => throw new NotSupportedException()
        };
        
        // Create proof
        var proof = new LinkedDataProof
        {
            Type = "JsonWebSignature2020",
            Created = DateTime.UtcNow,
            VerificationMethod = $"did:web:numbat.wa.gov.au#key-1",
            ProofPurpose = "assertionMethod",
            Jws = signature
        };
        
        return new SignedCredential
        {
            Document = document,
            Proof = proof,
            Certificate = certificate.Thumbprint
        };
    }
    
    public async Task<bool> VerifySignature(SignedCredential credential)
    {
        try
        {
            // Verify certificate chain
            var cert = await _certStore.GetCertificateAsync(credential.Certificate);
            if (!await VerifyCertificateChain(cert))
                return false;
            
            // Check revocation
            if (await IsCertificateRevoked(cert))
                return false;
            
            // Verify signature
            return await VerifyJws(credential.Proof.Jws, cert.PublicKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Signature verification failed");
            return false;
        }
    }
}
```

### HSM Integration

```csharp
public class HsmKeyService : IKeyService
{
    private readonly KeyClient _keyClient;
    
    public HsmKeyService(IConfiguration configuration)
    {
        var hsmUri = configuration["AzureKeyVault:HsmUri"];
        _keyClient = new KeyClient(new Uri(hsmUri), new DefaultAzureCredential());
    }
    
    public async Task<KeyVaultKey> CreateSigningKey(string keyName, TenantId tenantId)
    {
        var keyOptions = new CreateEcKeyOptions($"{tenantId}-{keyName}", hardwareProtected: true)
        {
            KeyOperations = { KeyOperation.Sign, KeyOperation.Verify },
            Curve = KeyCurveName.P256,
            ExpiresOn = DateTimeOffset.UtcNow.AddYears(2)
        };
        
        keyOptions.Tags.Add("TenantId", tenantId.ToString());
        keyOptions.Tags.Add("Purpose", "CredentialSigning");
        
        return await _keyClient.CreateKeyAsync(keyOptions);
    }
}
```

## Data Protection

### Encryption at Rest

```csharp
public class EncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;
    
    public EncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("NumbatWallet.Credentials");
    }
    
    public async Task<EncryptedData> EncryptSensitiveData(
        string data,
        EncryptionContext context)
    {
        // Generate DEK (Data Encryption Key)
        var dek = GenerateDataEncryptionKey();
        
        // Encrypt data with DEK
        var encryptedData = await EncryptWithAes256(data, dek);
        
        // Encrypt DEK with KEK (Key Encryption Key) from Key Vault
        var kek = await _keyVault.GetKeyAsync($"kek-{context.TenantId}");
        var encryptedDek = await _keyVault.WrapKeyAsync(kek, dek);
        
        return new EncryptedData
        {
            Data = encryptedData,
            EncryptedKey = encryptedDek,
            Algorithm = "AES-256-GCM",
            KeyId = kek.Id,
            Nonce = GenerateNonce(),
            AuthTag = GenerateAuthTag(encryptedData, context)
        };
    }
}
```

### Database Security

```sql
-- Row Level Security for multi-tenancy
CREATE POLICY tenant_isolation ON credentials
    FOR ALL
    USING (tenant_id = current_setting('app.tenant_id')::uuid);

-- Column encryption for PII
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE wallet_users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    email_encrypted BYTEA NOT NULL,
    phone_encrypted BYTEA NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Audit table
CREATE TABLE audit_log (
    id BIGSERIAL PRIMARY KEY,
    tenant_id UUID NOT NULL,
    user_id UUID,
    action VARCHAR(100) NOT NULL,
    resource_type VARCHAR(50),
    resource_id UUID,
    ip_address INET,
    user_agent TEXT,
    success BOOLEAN NOT NULL,
    details JSONB,
    created_at TIMESTAMPTZ DEFAULT NOW()
) PARTITION BY RANGE (created_at);
```

## Secure Communications

### TLS Configuration

```yaml
TLS Settings:
  minimum_version: TLS 1.2
  preferred_version: TLS 1.3
  
  cipher_suites:
    - TLS_AES_256_GCM_SHA384
    - TLS_AES_128_GCM_SHA256
    - TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384
    - TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256
  
  excluded_ciphers:
    - RC4
    - 3DES
    - MD5
    - SHA1
  
  certificate_validation:
    check_revocation: true
    require_sni: true
    strict_transport_security: max-age=31536000; includeSubDomains
```

### API Security Headers

```csharp
public class SecurityHeadersMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Add("Content-Security-Policy", 
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'");
        context.Response.Headers.Add("Permissions-Policy", 
            "geolocation=(), microphone=(), camera=()");
        
        await next(context);
    }
}
```

## Key Management

### Key Rotation Policy

```csharp
public class KeyRotationService : IHostedService
{
    private readonly Timer _timer;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(CheckKeyRotation, null, TimeSpan.Zero, TimeSpan.FromDays(1));
        return Task.CompletedTask;
    }
    
    private async void CheckKeyRotation(object state)
    {
        var keys = await _keyVault.ListKeysAsync();
        
        foreach (var key in keys)
        {
            var daysUntilExpiry = (key.ExpiresOn - DateTime.UtcNow).TotalDays;
            
            if (daysUntilExpiry <= 30)
            {
                await RotateKey(key);
            }
            else if (daysUntilExpiry <= 90)
            {
                await _alertService.SendKeyExpiryWarning(key, daysUntilExpiry);
            }
        }
    }
    
    private async Task RotateKey(KeyVaultKey key)
    {
        // Create new key version
        var newKey = await _keyVault.CreateKeyAsync(key.Name + "-new");
        
        // Update references
        await UpdateKeyReferences(key.Name, newKey.Name);
        
        // Schedule old key for deletion
        await _keyVault.ScheduleKeyDeletion(key.Name, DateTime.UtcNow.AddDays(30));
        
        // Audit
        await _auditLog.LogKeyRotation(key.Name, newKey.Name);
    }
}
```

### Secret Management

```csharp
public class SecretService : ISecretService
{
    private readonly SecretClient _secretClient;
    
    public async Task<string> GetSecretAsync(string secretName, TenantId tenantId)
    {
        try
        {
            var secret = await _secretClient.GetSecretAsync($"{tenantId}-{secretName}");
            
            // Audit access
            await _auditLog.LogSecretAccess(secretName, tenantId, success: true);
            
            return secret.Value.Value;
        }
        catch (Exception ex)
        {
            await _auditLog.LogSecretAccess(secretName, tenantId, success: false);
            throw;
        }
    }
}
```

## Threat Mitigation

### OWASP Top 10 Protection

```csharp
// 1. Injection Prevention
public class SqlInjectionProtection
{
    public async Task<User> GetUserAsync(string email)
    {
        // Always use parameterized queries
        using var command = new NpgsqlCommand(
            "SELECT * FROM users WHERE email = @email", 
            _connection);
        command.Parameters.AddWithValue("@email", email);
        
        return await ExecuteQueryAsync(command);
    }
}

// 2. Broken Authentication
public class AuthenticationProtection
{
    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        // Rate limiting
        if (await IsRateLimited(request.Username))
            return LoginResult.TooManyAttempts;
        
        // Account lockout
        if (await IsAccountLocked(request.Username))
            return LoginResult.AccountLocked;
        
        // Validate credentials
        var user = await ValidateCredentials(request);
        if (user == null)
        {
            await RecordFailedAttempt(request.Username);
            return LoginResult.InvalidCredentials;
        }
        
        // Require MFA
        if (user.MfaEnabled)
        {
            return LoginResult.RequiresMfa;
        }
        
        return LoginResult.Success(user);
    }
}

// 3. Sensitive Data Exposure
public class DataProtection
{
    public object SanitizeForLogging(object data)
    {
        var json = JsonSerializer.Serialize(data);
        
        // Remove sensitive fields
        var patterns = new[] { "password", "ssn", "creditcard", "pin" };
        foreach (var pattern in patterns)
        {
            json = Regex.Replace(json, 
                $"\"{pattern}\"\\s*:\\s*\"[^\"]*\"", 
                $"\"{pattern}\":\"***REDACTED***\"", 
                RegexOptions.IgnoreCase);
        }
        
        return JsonSerializer.Deserialize<object>(json);
    }
}
```

### DDoS Protection

```yaml
Azure DDoS Protection:
  tier: Standard
  
  mitigation_policies:
    - tcp_syn_flood:
        threshold: 1000/sec
        action: block
    
    - udp_flood:
        threshold: 10000/sec
        action: block
    
    - http_flood:
        threshold: 5000/sec
        action: challenge

API Rate Limiting:
  policies:
    - name: standard_user
      requests_per_minute: 60
      requests_per_hour: 1000
    
    - name: service_account
      requests_per_minute: 300
      requests_per_hour: 10000
    
    - name: public_api
      requests_per_minute: 20
      requests_per_hour: 100
```

## Security Testing

### Vulnerability Scanning

```bash
# OWASP Dependency Check
dotnet tool install --global dotnet-dependency-check
dependency-check --project "NumbatWallet" --scan ./src --format HTML

# Security Code Analysis
dotnet tool install --global security-scan
security-scan --project ./src/NumbatWallet.sln --output ./security-report.html

# Container Scanning
trivy image numbat-wallet:latest
```

### Penetration Testing Checklist

```markdown
## Authentication Testing
- [ ] Password brute force protection
- [ ] Session management vulnerabilities
- [ ] OAuth/OIDC implementation flaws
- [ ] MFA bypass attempts
- [ ] Account enumeration

## Authorization Testing
- [ ] Privilege escalation
- [ ] IDOR vulnerabilities
- [ ] JWT manipulation
- [ ] Role bypass attempts
- [ ] Tenant isolation

## Input Validation
- [ ] SQL injection
- [ ] XSS attacks
- [ ] XXE injection
- [ ] Command injection
- [ ] Path traversal

## Cryptography
- [ ] Weak encryption algorithms
- [ ] Improper key management
- [ ] Certificate validation
- [ ] Random number generation
```

### Security Test Automation

```csharp
[TestFixture]
public class SecurityTests
{
    [Test]
    public async Task Should_Prevent_SQL_Injection()
    {
        // Arrange
        var maliciousInput = "'; DROP TABLE users; --";
        
        // Act
        var response = await _client.GetAsync($"/api/users?email={maliciousInput}");
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await TableExists("users"), Is.True);
    }
    
    [Test]
    public async Task Should_Enforce_Rate_Limiting()
    {
        // Arrange
        var requests = new List<Task<HttpResponseMessage>>();
        
        // Act
        for (int i = 0; i < 100; i++)
        {
            requests.Add(_client.GetAsync("/api/credentials"));
        }
        var responses = await Task.WhenAll(requests);
        
        // Assert
        var rateLimited = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        Assert.That(rateLimited, Is.GreaterThan(0));
    }
    
    [Test]
    public async Task Should_Validate_JWT_Signature()
    {
        // Arrange
        var tamperedToken = GenerateTamperedJwt();
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", tamperedToken);
        
        // Act
        var response = await _client.GetAsync("/api/protected");
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
```

## Compliance Validation

### TDIF Compliance Checklist

```markdown
## Identity Proofing
- [ ] LOA2 verification implemented
- [ ] Document verification service integrated
- [ ] Biometric matching capability
- [ ] Fraud detection mechanisms

## Privacy Requirements
- [ ] Privacy policy implemented
- [ ] Consent management system
- [ ] Data minimization enforced
- [ ] Right to erasure supported

## Security Controls
- [ ] Encryption at rest (AES-256)
- [ ] Encryption in transit (TLS 1.2+)
- [ ] Key management procedures
- [ ] Incident response plan
```

### ISO 27001 Controls

```yaml
Access Control:
  - User registration and de-registration
  - User access provisioning
  - Privileged access management
  - Password management
  - Review of user access rights

Cryptography:
  - Cryptographic policy
  - Key management procedures
  - Use of encryption

Physical Security:
  - Not applicable (cloud-native)

Operations Security:
  - Change management
  - Capacity management
  - Malware protection
  - Logging and monitoring
  - Vulnerability management

Communications Security:
  - Network security management
  - Information transfer policies
  - Electronic messaging security
```

## Security Monitoring

### Azure Monitor Configuration

```json
{
  "securityAlerts": {
    "highSeverity": {
      "action": "email,sms,ticket",
      "recipients": ["security@numbat.com"],
      "conditions": [
        "Multiple failed login attempts",
        "Privilege escalation detected",
        "Suspicious API activity"
      ]
    },
    "mediumSeverity": {
      "action": "email,ticket",
      "conditions": [
        "Certificate expiry warning",
        "Unusual traffic pattern",
        "Configuration change"
      ]
    }
  },
  "logAnalytics": {
    "workspace": "log-numbat-poa",
    "retention": 90,
    "queries": [
      {
        "name": "Failed Authentication",
        "query": "SecurityEvent | where EventID == 4625"
      },
      {
        "name": "Privilege Escalation",
        "query": "AuditLog | where Action == 'RoleAssignment'"
      }
    ]
  }
}
```

## POA Security Deliverables

### Week 1
- [ ] Azure security baseline configured
- [ ] Network security groups deployed
- [ ] Key Vault provisioned
- [ ] Service principals created

### Week 2
- [ ] OIDC integration tested
- [ ] PKI certificates generated
- [ ] Encryption implemented
- [ ] Audit logging enabled

### Week 3
- [ ] Security scan completed
- [ ] Penetration test executed
- [ ] Vulnerabilities remediated
- [ ] Security report delivered

### Week 4-5
- [ ] Security monitoring dashboard
- [ ] Incident response procedures
- [ ] Security documentation
- [ ] Compliance evidence package