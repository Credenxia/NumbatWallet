# Credenxia Integration Guide for NumbatWallet
*Version: 1.0.0*
*Last Updated: September 22, 2025*
*Classification: CONFIDENTIAL - Integration Partners Only*

## Executive Summary

This document provides comprehensive technical guidelines for integrating Credenxia's workforce management system with NumbatWallet's digital identity platform. The integration will enable Credenxia to issue, verify, and manage digital credentials for workforce access control in high-regulated sectors (mining, construction, etc.).

## 1. Integration Overview

### 1.1 Purpose
Enable Credenxia to:
- Issue digital workforce credentials (licenses, certifications, access cards)
- Verify worker identities and qualifications
- Manage credential lifecycle (issue, renew, suspend, revoke)
- Monitor credential status for gate access control
- Track compliance and audit trails

### 1.2 Architecture
```
Credenxia System                    NumbatWallet Platform
┌─────────────────┐                 ┌──────────────────┐
│ Workforce Mgmt  │ <-- REST API -->│  Identity API    │
│   - Workers     │                 │   - Credentials  │
│   - Licenses    │ <-- GraphQL --> │   - Wallets     │
│   - Gates       │                 │   - Verification │
└─────────────────┘                 └──────────────────┘
        │                                    │
        └──────── Webhook Events ───────────┘
```

## 2. Prerequisites on Credenxia Side

### 2.1 Technical Requirements
```yaml
Minimum Requirements:
  - .NET 6.0+ or compatible REST client
  - TLS 1.3 support
  - JSON/JWT processing capability
  - Webhook endpoint (HTTPS)
  - Database for credential mapping

Recommended:
  - Redis for caching
  - Message queue (Azure Service Bus/RabbitMQ)
  - Monitoring (Application Insights/ELK)
```

### 2.2 Security Requirements
- **API Keys**: Implement secure storage for NumbatWallet API keys
- **mTLS**: Client certificate for mutual TLS authentication
- **Request Signing**: HMAC-SHA256 for request integrity
- **Data Encryption**: AES-256-GCM for sensitive data at rest

## 3. API Integration Specifications

### 3.1 Authentication Setup

#### Step 1: Register as Trusted Issuer
```http
POST https://api.numbatwallet.com.au/v1/issuers/register
Content-Type: application/json
X-API-Key: {PROVIDED_BY_NUMBATWALLET}

{
  "organizationName": "Credenxia Pty Ltd",
  "organizationType": "WORKFORCE_MANAGER",
  "issuerDomain": "credenxia.com.au",
  "contactEmail": "integration@credenxia.com.au",
  "credentialTypes": [
    "WorkerIdentity",
    "LicenseCertificate",
    "AccessPermit",
    "TrainingRecord"
  ],
  "webhookUrl": "https://api.credenxia.com.au/webhooks/numbatwallet",
  "publicKey": "-----BEGIN PUBLIC KEY-----\n{YOUR_RSA_PUBLIC_KEY}\n-----END PUBLIC KEY-----"
}

Response:
{
  "issuerId": "ISS-CREDENXIA-001",
  "apiKey": "sk_live_xxxxxxxxxxxxx",
  "apiSecret": "ss_xxxxxxxxxxxxx",
  "webhookSecret": "whsec_xxxxxxxxxxxxx",
  "certificateThumbprint": "SHA256:xxxxxx",
  "status": "PENDING_VERIFICATION"
}
```

#### Step 2: Implement Request Signing
```csharp
// C# Example for Credenxia
public class NumbaWalletClient
{
    private readonly string _apiKey;
    private readonly string _apiSecret;

    public string GenerateSignature(HttpMethod method, string path, string body)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString("N");

        var message = $"{method.Method}\n{path}\n{timestamp}\n{nonce}\n{body}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiSecret));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(message)));

        return $"HMAC-SHA256 timestamp={timestamp},nonce={nonce},signature={signature}";
    }

    public async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string path, object? payload = null)
    {
        var body = payload != null ? JsonSerializer.Serialize(payload) : "";
        var signature = GenerateSignature(method, path, body);

        var request = new HttpRequestMessage(method, $"https://api.numbatwallet.com.au{path}");
        request.Headers.Add("X-API-Key", _apiKey);
        request.Headers.Add("X-Signature", signature);

        if (payload != null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        return await _httpClient.SendAsync(request);
    }
}
```

### 3.2 Worker Identity Management

#### Create Worker Wallet
```http
POST /v1/wallets/create
{
  "externalId": "CRX-WORKER-123456",  // Your worker ID
  "holderType": "PERSON",
  "holderInfo": {
    "firstName": "John",
    "lastName": "Smith",
    "dateOfBirth": "1985-03-15",
    "email": "john.smith@example.com",
    "phone": "+61412345678",
    "employeeId": "EMP-123456"
  },
  "metadata": {
    "company": "Mining Corp Pty Ltd",
    "site": "SITE-001",
    "department": "Operations"
  }
}

Response:
{
  "walletId": "WALLET-xxxxx",
  "did": "did:numb:xxxxx",
  "status": "ACTIVE",
  "createdAt": "2025-09-22T10:30:00Z"
}
```

### 3.3 Credential Issuance

#### Issue Worker License
```http
POST /v1/credentials/issue
{
  "walletId": "WALLET-xxxxx",
  "credentialType": "LicenseCertificate",
  "credentialData": {
    "licenseType": "HIGH_RISK_WORK",
    "licenseNumber": "HRW-2025-123456",
    "licenseClass": "RB",  // Rigging Basic
    "issuedBy": "SafeWork Australia",
    "issuedDate": "2025-09-01",
    "expiryDate": "2027-09-01",
    "restrictions": [],
    "competencies": [
      "Dogging",
      "Basic Rigging",
      "Crane Operations"
    ]
  },
  "validFrom": "2025-09-01T00:00:00Z",
  "validUntil": "2027-09-01T00:00:00Z",
  "evidenceDocuments": [
    {
      "type": "PDF",
      "name": "license_certificate.pdf",
      "hash": "SHA256:xxxxx",
      "url": "https://docs.credenxia.com.au/evidence/xxxxx"
    }
  ]
}

Response:
{
  "credentialId": "CRED-xxxxx",
  "credentialSubject": "did:numb:xxxxx",
  "issuanceDate": "2025-09-22T10:35:00Z",
  "proof": {
    "type": "JwtProof2020",
    "jwt": "eyJhbGciOiJSUzI1NiIs..."
  },
  "status": "ISSUED"
}
```

### 3.4 Credential Verification

#### Verify Worker Credentials for Gate Access
```http
POST /v1/credentials/verify
{
  "credentialId": "CRED-xxxxx",
  "purpose": "GATE_ACCESS",
  "requiredCredentials": [
    {
      "type": "LicenseCertificate",
      "minimumClass": "RB",
      "mustBeValid": true
    },
    {
      "type": "TrainingRecord",
      "trainingType": "SITE_INDUCTION",
      "validWithinDays": 365
    }
  ],
  "contextData": {
    "siteId": "SITE-001",
    "gateId": "GATE-NORTH",
    "timestamp": "2025-09-22T06:00:00Z"
  }
}

Response:
{
  "verified": true,
  "validUntil": "2025-09-22T18:00:00Z",
  "verificationResult": {
    "allCredentialsValid": true,
    "details": [
      {
        "credentialType": "LicenseCertificate",
        "status": "VALID",
        "expiresIn": "730 days"
      },
      {
        "credentialType": "TrainingRecord",
        "status": "VALID",
        "completedOn": "2025-08-15"
      }
    ]
  },
  "accessToken": "ACCESS-xxxxx",  // For gate system
  "restrictions": []
}
```

### 3.5 Credential Lifecycle Management

#### Suspend Worker Credential (Lost License)
```http
PUT /v1/credentials/{credentialId}/suspend
{
  "reason": "LICENSE_LOST",
  "suspendedBy": "admin@credenxia.com.au",
  "suspensionDate": "2025-09-22T11:00:00Z",
  "expectedResolutionDate": "2025-09-29T11:00:00Z",
  "notes": "Worker reported license lost on site"
}
```

#### Revoke Worker Credential (Expired/Terminated)
```http
PUT /v1/credentials/{credentialId}/revoke
{
  "reason": "LICENSE_EXPIRED",
  "revokedBy": "system@credenxia.com.au",
  "revocationDate": "2025-09-22T00:00:00Z",
  "permanent": true
}
```

## 4. Webhook Integration

### 4.1 Webhook Events from NumbatWallet

Configure your webhook endpoint to receive:

```csharp
// Webhook endpoint in Credenxia
[HttpPost("/webhooks/numbatwallet")]
public async Task<IActionResult> HandleNumbaWalletWebhook(
    [FromHeader("X-Webhook-Signature")] string signature,
    [FromBody] WebhookEvent webhookEvent)
{
    // Verify signature
    if (!VerifyWebhookSignature(signature, webhookEvent))
        return Unauthorized();

    switch (webhookEvent.EventType)
    {
        case "credential.issued":
            await HandleCredentialIssued(webhookEvent.Data);
            break;

        case "credential.expired":
            await HandleCredentialExpired(webhookEvent.Data);
            await NotifyGateControllers(webhookEvent.Data.WorkerId);
            break;

        case "credential.revoked":
            await ImmediatelyBlockGateAccess(webhookEvent.Data.WorkerId);
            break;

        case "wallet.suspended":
            await SuspendWorkerAccess(webhookEvent.Data.WalletId);
            break;

        case "verification.failed":
            await LogSecurityEvent(webhookEvent.Data);
            break;
    }

    return Ok(new { received = true });
}
```

### 4.2 Event Types

```typescript
// TypeScript definitions for clarity
interface WebhookEvent {
  eventId: string;
  eventType: EventType;
  timestamp: string;
  data: EventData;
  metadata: {
    issuerId: string;
    environment: 'production' | 'sandbox';
  };
}

type EventType =
  | 'credential.issued'
  | 'credential.expired'
  | 'credential.revoked'
  | 'credential.renewed'
  | 'wallet.created'
  | 'wallet.suspended'
  | 'wallet.reactivated'
  | 'verification.success'
  | 'verification.failed';
```

## 5. Data Models

### 5.1 Worker Credential Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "@context": {
      "type": "array",
      "items": { "type": "string" }
    },
    "type": {
      "type": "array",
      "items": { "type": "string" },
      "contains": { "const": "WorkforceCredential" }
    },
    "credentialSubject": {
      "type": "object",
      "properties": {
        "id": { "type": "string", "format": "uri" },
        "workerProfile": {
          "type": "object",
          "properties": {
            "employeeId": { "type": "string" },
            "fullName": { "type": "string" },
            "role": { "type": "string" },
            "company": { "type": "string" },
            "site": { "type": "string" }
          }
        },
        "licenses": {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "type": { "type": "string" },
              "number": { "type": "string" },
              "class": { "type": "string" },
              "validFrom": { "type": "string", "format": "date-time" },
              "validTo": { "type": "string", "format": "date-time" }
            }
          }
        },
        "trainings": {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "name": { "type": "string" },
              "provider": { "type": "string" },
              "completedDate": { "type": "string", "format": "date" },
              "expiryDate": { "type": "string", "format": "date" }
            }
          }
        }
      }
    },
    "issuer": {
      "type": "object",
      "properties": {
        "id": { "type": "string" },
        "name": { "type": "string" }
      }
    },
    "issuanceDate": { "type": "string", "format": "date-time" },
    "expirationDate": { "type": "string", "format": "date-time" }
  }
}
```

### 5.2 Gate Access Token Format

```json
{
  "tokenId": "ACCESS-xxxxx",
  "workerId": "CRX-WORKER-123456",
  "validFrom": "2025-09-22T06:00:00Z",
  "validUntil": "2025-09-22T18:00:00Z",
  "permissions": {
    "sites": ["SITE-001", "SITE-002"],
    "areas": ["OPERATIONS", "MAINTENANCE"],
    "equipment": ["CRANE-01", "FORKLIFT-*"]
  },
  "restrictions": {
    "requiresSupervisor": false,
    "requiresPPE": ["HARD_HAT", "SAFETY_BOOTS", "HI_VIS"],
    "timeLimits": {
      "maxHoursPerDay": 10,
      "maxHoursPerWeek": 60
    }
  },
  "verificationProof": {
    "credentialsVerified": ["CRED-xxx", "CRED-yyy"],
    "verifiedAt": "2025-09-22T05:59:00Z",
    "verifiedBy": "gate-system@credenxia.com.au"
  }
}
```

## 6. Implementation Checklist

### 6.1 Credenxia Development Tasks

#### Phase 1: Foundation (Week 1)
- [ ] Create NumbatWallet integration service/module
- [ ] Implement API client with request signing
- [ ] Setup webhook endpoint with signature verification
- [ ] Create database schema for credential mapping
- [ ] Implement error handling and retry logic

#### Phase 2: Worker Management (Week 2)
- [ ] Implement worker-to-wallet mapping
- [ ] Create wallet for existing workers (bulk)
- [ ] Setup worker onboarding flow
- [ ] Implement worker status synchronization

#### Phase 3: Credential Operations (Week 3)
- [ ] Implement license issuance workflow
- [ ] Create training record management
- [ ] Setup credential verification for gates
- [ ] Implement expiry monitoring

#### Phase 4: Gate Integration (Week 4)
- [ ] Update gate controllers for digital verification
- [ ] Implement offline verification fallback
- [ ] Create access logging and audit trails
- [ ] Setup real-time monitoring dashboard

### 6.2 Configuration Required

#### Environment Variables (Credenxia)
```env
# NumbatWallet Integration
NUMBATWALLET_API_URL=https://api.numbatwallet.com.au
NUMBATWALLET_API_KEY=sk_live_xxxxx
NUMBATWALLET_API_SECRET=ss_xxxxx
NUMBATWALLET_WEBHOOK_SECRET=whsec_xxxxx
NUMBATWALLET_ISSUER_ID=ISS-CREDENXIA-001

# Security
NUMBATWALLET_CERT_THUMBPRINT=SHA256:xxxxx
NUMBATWALLET_REQUEST_TIMEOUT=30000
NUMBATWALLET_MAX_RETRIES=3

# Feature Flags
ENABLE_DIGITAL_CREDENTIALS=true
ENABLE_OFFLINE_VERIFICATION=true
ENABLE_WEBHOOK_PROCESSING=true
```

## 7. Testing Strategy

### 7.1 Sandbox Environment

```
Sandbox API: https://sandbox.api.numbatwallet.com.au
Sandbox Credentials:
  API Key: sk_test_xxxxx
  API Secret: ss_test_xxxxx
```

### 7.2 Test Scenarios

1. **Worker Onboarding**
   - Create new worker → Create wallet → Issue credentials

2. **Daily Gate Access**
   - Worker presents ID → Verify credentials → Grant/Deny access

3. **License Expiry**
   - Monitor expiry → Notify worker → Suspend access → Renew → Restore access

4. **Emergency Revocation**
   - Incident occurs → Revoke credential → Block all gates immediately

### 7.3 Test Data

```json
{
  "testWorkers": [
    {
      "id": "TEST-001",
      "name": "John Test",
      "licenses": ["HRW", "FORKLIFT"],
      "scenario": "VALID_ACCESS"
    },
    {
      "id": "TEST-002",
      "name": "Jane Expired",
      "licenses": ["HRW-EXPIRED"],
      "scenario": "EXPIRED_LICENSE"
    },
    {
      "id": "TEST-003",
      "name": "Bob Suspended",
      "licenses": ["HRW"],
      "scenario": "SUSPENDED_ACCESS"
    }
  ]
}
```

## 8. Security Considerations

### 8.1 Data Protection
- **PII Encryption**: All personal data encrypted with AES-256-GCM
- **Key Management**: Keys rotated every 90 days
- **Data Residency**: All data stored in Australian data centers

### 8.2 Access Control
- **API Rate Limiting**: 1000 requests/minute per API key
- **IP Whitelisting**: Optional restriction to known Credenxia IPs
- **Audit Logging**: All API calls logged for 7 years

### 8.3 Incident Response
```
Security Incident Contact:
  Email: security@numbatwallet.gov.au
  Phone: +61 2 XXXX XXXX (24/7)

Include:
  - Issuer ID
  - Incident timestamp
  - Affected credentials/workers
  - Actions taken
```

## 9. Migration Path

### 9.1 Existing Worker Migration

```python
# Python script for bulk migration
import requests
import csv
from datetime import datetime

def migrate_workers(csv_file):
    """Migrate existing Credenxia workers to NumbatWallet"""

    with open(csv_file, 'r') as file:
        workers = csv.DictReader(file)

        for worker in workers:
            # Create wallet
            wallet_response = create_wallet(worker)

            # Issue existing licenses as credentials
            for license in get_worker_licenses(worker['id']):
                issue_credential(wallet_response['walletId'], license)

            # Map in database
            save_wallet_mapping(
                worker_id=worker['id'],
                wallet_id=wallet_response['walletId']
            )

            print(f"Migrated worker {worker['id']}")

def create_wallet(worker):
    response = requests.post(
        f"{API_URL}/v1/wallets/create",
        json={
            "externalId": worker['id'],
            "holderInfo": {
                "firstName": worker['first_name'],
                "lastName": worker['last_name'],
                "email": worker['email']
            }
        },
        headers=get_auth_headers()
    )
    return response.json()
```

### 9.2 Phased Rollout

1. **Phase 1**: Pilot with 10 workers at one site
2. **Phase 2**: Expand to 100 workers across 3 sites
3. **Phase 3**: Full rollout to all workers
4. **Phase 4**: Deprecate physical cards

## 10. Support & Resources

### 10.1 Technical Support

```
Integration Support Team
  Email: integration@numbatwallet.com.au
  Slack: #credenxia-integration

Office Hours: Mon-Fri 9AM-5PM AWST
Emergency: +61 8 XXXX XXXX
```

### 10.2 Documentation

- API Reference: https://docs.numbatwallet.com.au/api
- SDKs: https://github.com/numbatwallet/sdks
- Postman Collection: https://postman.numbatwallet.com.au/credenxia
- Status Page: https://status.numbatwallet.com.au

### 10.3 Code Examples

Repository: https://github.com/numbatwallet/credenxia-integration-examples

Includes:
- C# client library
- Python migration scripts
- Node.js webhook handler
- Gate controller firmware update

## 11. SLA & Performance

### 11.1 Service Level Agreement

| Metric | Target | Measurement |
|--------|--------|-------------|
| API Availability | 99.9% | Monthly |
| Response Time (p95) | <500ms | Per endpoint |
| Webhook Delivery | 99.99% | Daily |
| Credential Issuance | <2 seconds | Per request |
| Verification Time | <200ms | Per check |

### 11.2 Performance Optimization

```csharp
// Implement caching for frequently verified credentials
public class CredentialCache
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(15);

    public async Task<VerificationResult> VerifyWithCacheAsync(string credentialId)
    {
        var cacheKey = $"verify_{credentialId}";

        if (_cache.TryGetValue<VerificationResult>(cacheKey, out var cached))
        {
            if (cached.ValidUntil > DateTime.UtcNow)
                return cached;
        }

        var result = await VerifyCredentialAsync(credentialId);
        _cache.Set(cacheKey, result, _cacheExpiry);

        return result;
    }
}
```

## 12. Compliance & Audit

### 12.1 Regulatory Requirements

- **Privacy Act 1988**: Personal information handling
- **Work Health and Safety Act**: License verification requirements
- **Fair Work Act**: Employment record keeping

### 12.2 Audit Trail Requirements

Every credential operation must log:
- Timestamp (UTC)
- Actor (user/system)
- Action (issue/verify/revoke)
- Target (worker/credential ID)
- Result (success/failure)
- Context (site/gate/reason)

### 12.3 Reporting

Monthly reports available via API:
- Credentials issued/revoked
- Verification statistics
- Failed access attempts
- System performance metrics

## Appendix A: Error Codes

| Code | Description | Action |
|------|-------------|--------|
| 400 | Bad Request | Check request format |
| 401 | Unauthorized | Verify API key |
| 403 | Forbidden | Check permissions |
| 404 | Not Found | Verify resource ID |
| 409 | Conflict | Resource already exists |
| 429 | Rate Limited | Implement backoff |
| 500 | Server Error | Retry with backoff |
| 503 | Service Unavailable | Check status page |

## Appendix B: Glossary

- **DID**: Decentralized Identifier
- **VC**: Verifiable Credential
- **VP**: Verifiable Presentation
- **JWT**: JSON Web Token
- **mTLS**: Mutual TLS
- **HMAC**: Hash-based Message Authentication Code
- **PII**: Personally Identifiable Information

---

**Document Control**
- Version: 1.0.0
- Author: NumbatWallet Integration Team
- Approved By: [Pending]
- Next Review: October 22, 2025

**Questions?**
Contact: integration@numbatwallet.com.au