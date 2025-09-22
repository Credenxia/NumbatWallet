# Credenxia API Contract Specification
*For NumbatWallet Integration*
*Version: 1.0.0*

## What Credenxia Must Implement

This document specifies the exact APIs and webhooks that Credenxia must implement on their side to enable bi-directional integration with NumbatWallet.

## 1. Credenxia Must Expose These Endpoints

### 1.1 Worker Information API

**Purpose**: Allow NumbatWallet to fetch worker details when creating wallets

```yaml
Endpoint: GET https://api.credenxia.com.au/v1/workers/{workerId}
Authentication: Bearer {NUMBATWALLET_API_TOKEN}
Headers:
  - X-Request-ID: {unique-request-id}
  - X-Timestamp: {unix-timestamp}

Response (200 OK):
{
  "workerId": "CRX-WORKER-123456",
  "personalInfo": {
    "firstName": "John",
    "lastName": "Smith",
    "dateOfBirth": "1985-03-15",  // ISO 8601
    "nationality": "AU",
    "email": "john.smith@example.com",
    "phone": "+61412345678",
    "address": {
      "line1": "123 Main St",
      "city": "Perth",
      "state": "WA",
      "postcode": "6000",
      "country": "AU"
    }
  },
  "employmentInfo": {
    "employeeId": "EMP-123456",
    "company": "Mining Corp Pty Ltd",
    "site": "SITE-001",
    "siteName": "Northwest Mine",
    "department": "Operations",
    "role": "Senior Operator",
    "startDate": "2020-01-15",
    "status": "ACTIVE"  // ACTIVE|SUSPENDED|TERMINATED
  },
  "licenses": [
    {
      "licenseId": "LIC-001",
      "type": "HIGH_RISK_WORK",
      "number": "HRW-2025-123456",
      "class": "RB",
      "issuedBy": "SafeWork Australia",
      "issuedDate": "2023-09-01",
      "expiryDate": "2025-09-01",
      "status": "ACTIVE",  // ACTIVE|EXPIRED|SUSPENDED|REVOKED
      "documentUrl": "https://docs.credenxia.com.au/licenses/xxx.pdf"
    }
  ],
  "trainings": [
    {
      "trainingId": "TRN-001",
      "name": "Site Safety Induction",
      "provider": "Safety Training Co",
      "completedDate": "2024-01-15",
      "expiryDate": "2025-01-15",
      "certificateNumber": "CERT-123456",
      "status": "VALID"
    }
  ],
  "metadata": {
    "createdAt": "2020-01-15T08:00:00Z",
    "updatedAt": "2025-09-22T10:00:00Z",
    "lastAccessDate": "2025-09-22T06:30:00Z",
    "riskLevel": "STANDARD"  // LOW|STANDARD|HIGH
  }
}

Error Responses:
- 404: Worker not found
- 401: Authentication failed
- 429: Rate limit exceeded
```

### 1.2 License Validation Webhook

**Purpose**: Real-time validation when NumbatWallet needs to verify license authenticity

```yaml
Endpoint: POST https://api.credenxia.com.au/v1/licenses/validate
Authentication: Bearer {NUMBATWALLET_API_TOKEN}
Request Body:
{
  "licenseNumber": "HRW-2025-123456",
  "workerId": "CRX-WORKER-123456",
  "validationContext": {
    "purpose": "CREDENTIAL_ISSUANCE",
    "requestedBy": "numbatwallet",
    "timestamp": "2025-09-22T10:00:00Z"
  }
}

Response (200 OK):
{
  "valid": true,
  "license": {
    "number": "HRW-2025-123456",
    "type": "HIGH_RISK_WORK",
    "class": "RB",
    "holder": "John Smith",
    "issuedDate": "2023-09-01",
    "expiryDate": "2025-09-01",
    "status": "ACTIVE"
  },
  "validation": {
    "checkedWith": "SafeWork Australia Registry",
    "checkedAt": "2025-09-22T10:00:05Z",
    "nextCheckDue": "2025-09-23T10:00:00Z"
  }
}
```

### 1.3 Gate Access Notification

**Purpose**: Notify Credenxia when worker credentials are used for gate access

```yaml
Endpoint: POST https://api.credenxia.com.au/v1/access/notify
Authentication: Bearer {NUMBATWALLET_API_TOKEN}
Request Body:
{
  "eventType": "GATE_ACCESS",
  "workerId": "CRX-WORKER-123456",
  "accessDetails": {
    "gateId": "GATE-NORTH-001",
    "siteId": "SITE-001",
    "timestamp": "2025-09-22T06:30:00Z",
    "accessGranted": true,
    "credentialsVerified": [
      {
        "type": "LicenseCertificate",
        "status": "VALID",
        "credentialId": "CRED-xxxxx"
      },
      {
        "type": "TrainingRecord",
        "status": "VALID",
        "credentialId": "CRED-yyyyy"
      }
    ]
  },
  "location": {
    "latitude": -20.7256,
    "longitude": 116.8471
  }
}

Response (200 OK):
{
  "received": true,
  "processedAt": "2025-09-22T06:30:01Z",
  "actions": [
    {
      "type": "LOG_ACCESS",
      "status": "COMPLETED"
    },
    {
      "type": "UPDATE_TIMESHEET",
      "status": "COMPLETED"
    }
  ]
}
```

### 1.4 Credential Status Update

**Purpose**: Receive updates when credential status changes in NumbatWallet

```yaml
Endpoint: POST https://api.credenxia.com.au/webhooks/credential-update
Headers:
  - X-Webhook-Signature: {HMAC-SHA256-signature}
  - X-Webhook-ID: {unique-webhook-id}
  - X-Webhook-Timestamp: {unix-timestamp}

Request Body:
{
  "event": "credential.status.changed",
  "timestamp": "2025-09-22T11:00:00Z",
  "data": {
    "credentialId": "CRED-xxxxx",
    "workerId": "CRX-WORKER-123456",
    "previousStatus": "ACTIVE",
    "newStatus": "SUSPENDED",
    "reason": "LICENSE_EXPIRED",
    "changedBy": "system",
    "effectiveDate": "2025-09-22T00:00:00Z",
    "metadata": {
      "licenseNumber": "HRW-2025-123456",
      "expiryDate": "2025-09-21"
    }
  }
}

Response (200 OK):
{
  "acknowledged": true,
  "actions": [
    {
      "type": "BLOCK_GATE_ACCESS",
      "status": "COMPLETED",
      "affectedGates": ["GATE-NORTH-001", "GATE-SOUTH-001"]
    },
    {
      "type": "NOTIFY_SUPERVISOR",
      "status": "COMPLETED",
      "notifiedUsers": ["supervisor@credenxia.com.au"]
    }
  ]
}
```

## 2. Database Schema Credenxia Needs

### 2.1 Wallet Mapping Table

```sql
CREATE TABLE numbatwallet_mappings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    worker_id VARCHAR(50) NOT NULL REFERENCES workers(id),
    wallet_id VARCHAR(100) NOT NULL,
    wallet_did VARCHAR(200) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',

    CONSTRAINT uk_worker_wallet UNIQUE(worker_id),
    INDEX idx_wallet_id (wallet_id),
    INDEX idx_status (status)
);
```

### 2.2 Credential Registry Table

```sql
CREATE TABLE numbatwallet_credentials (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    credential_id VARCHAR(100) NOT NULL,
    worker_id VARCHAR(50) NOT NULL REFERENCES workers(id),
    credential_type VARCHAR(50) NOT NULL,
    license_id VARCHAR(50) REFERENCES licenses(id),
    training_id VARCHAR(50) REFERENCES trainings(id),
    issued_date TIMESTAMP NOT NULL,
    expiry_date TIMESTAMP,
    status VARCHAR(20) NOT NULL,
    proof_jwt TEXT,
    metadata JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT uk_credential UNIQUE(credential_id),
    INDEX idx_worker_credentials (worker_id, status),
    INDEX idx_expiry (expiry_date),
    INDEX idx_type_status (credential_type, status)
);
```

### 2.3 Access Log Table

```sql
CREATE TABLE gate_access_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    worker_id VARCHAR(50) NOT NULL REFERENCES workers(id),
    gate_id VARCHAR(50) NOT NULL REFERENCES gates(id),
    site_id VARCHAR(50) NOT NULL REFERENCES sites(id),
    access_timestamp TIMESTAMP NOT NULL,
    access_granted BOOLEAN NOT NULL,
    verification_method VARCHAR(20) NOT NULL, -- DIGITAL|PHYSICAL|MANUAL
    credential_ids TEXT[], -- Array of verified credential IDs
    denial_reason VARCHAR(100),
    location_lat DECIMAL(10, 7),
    location_lng DECIMAL(10, 7),
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),

    INDEX idx_worker_access (worker_id, access_timestamp DESC),
    INDEX idx_gate_access (gate_id, access_timestamp DESC),
    INDEX idx_site_daily (site_id, DATE(access_timestamp))
);
```

## 3. Background Jobs Credenxia Must Implement

### 3.1 Credential Expiry Monitor

```python
# Python example using Celery
from celery import Celery
from datetime import datetime, timedelta

app = Celery('credenxia')

@app.task
def check_expiring_credentials():
    """Run daily at 2 AM"""

    # Find credentials expiring in next 30 days
    expiring_soon = db.query("""
        SELECT c.*, w.email, w.phone
        FROM numbatwallet_credentials c
        JOIN workers w ON c.worker_id = w.id
        WHERE c.expiry_date BETWEEN NOW() AND NOW() + INTERVAL '30 days'
        AND c.status = 'ACTIVE'
    """)

    for credential in expiring_soon:
        days_until_expiry = (credential.expiry_date - datetime.now()).days

        if days_until_expiry == 30:
            send_first_warning(credential)
        elif days_until_expiry == 14:
            send_second_warning(credential)
        elif days_until_expiry == 7:
            send_final_warning(credential)
            notify_supervisor(credential)
        elif days_until_expiry == 1:
            prepare_access_suspension(credential)

    return f"Processed {len(expiring_soon)} expiring credentials"

@app.task
def sync_credential_status():
    """Run every hour"""

    # Get all active credentials
    active_credentials = db.query("""
        SELECT credential_id, worker_id
        FROM numbatwallet_credentials
        WHERE status = 'ACTIVE'
    """)

    for credential in active_credentials:
        # Verify with NumbatWallet
        status = numbatwallet_client.verify_credential(credential.credential_id)

        if status != 'ACTIVE':
            update_local_status(credential, status)
            handle_status_change(credential, status)
```

### 3.2 Gate Controller Update Service

```csharp
// C# Background Service
public class GateControllerUpdateService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Update gate controllers every 15 minutes
            var gates = await _gateRepository.GetAllActiveGatesAsync();

            foreach (var gate in gates)
            {
                // Get latest access list from NumbatWallet
                var validCredentials = await _numbatWalletClient
                    .GetSiteValidCredentialsAsync(gate.SiteId);

                // Push to gate controller
                await _gateController.UpdateAccessListAsync(gate.Id, validCredentials);
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
```

## 4. Security Implementation Requirements

### 4.1 Request Signature Verification

```csharp
public class WebhookSignatureValidator
{
    private readonly string _webhookSecret;

    public bool ValidateSignature(string signature, string payload)
    {
        // Expected format: "t=timestamp,v1=signature"
        var parts = signature.Split(',');
        var timestamp = parts[0].Split('=')[1];
        var receivedSignature = parts[1].Split('=')[1];

        // Check timestamp (prevent replay attacks)
        var webhookTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(timestamp));
        if (Math.Abs((DateTimeOffset.UtcNow - webhookTime).TotalMinutes) > 5)
        {
            return false; // Webhook too old or too far in future
        }

        // Calculate expected signature
        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSecret));
        var expectedSignature = Convert.ToBase64String(
            hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))
        );

        // Constant-time comparison
        return CryptographicEquals(receivedSignature, expectedSignature);
    }

    private bool CryptographicEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;

        var result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }
        return result == 0;
    }
}
```

### 4.2 API Authentication Middleware

```csharp
public class NumbaWalletAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _expectedToken;

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply to NumbatWallet endpoints
        if (context.Request.Path.StartsWithSegments("/v1"))
        {
            // Check Bearer token
            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                context.Response.StatusCode = 401;
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (token != _expectedToken)
            {
                context.Response.StatusCode = 401;
                return;
            }

            // Verify request timestamp
            var timestamp = context.Request.Headers["X-Timestamp"].ToString();
            if (!IsValidTimestamp(timestamp))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid or expired timestamp");
                return;
            }
        }

        await _next(context);
    }

    private bool IsValidTimestamp(string timestamp)
    {
        if (!long.TryParse(timestamp, out var unixTime))
            return false;

        var requestTime = DateTimeOffset.FromUnixTimeSeconds(unixTime);
        var timeDiff = Math.Abs((DateTimeOffset.UtcNow - requestTime).TotalMinutes);

        return timeDiff <= 5; // Request must be within 5 minutes
    }
}
```

## 5. Error Handling Requirements

### 5.1 Retry Logic

```csharp
public class ResilientNumbaWalletClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxRetries = 3)
    {
        var retryDelays = new[] { 1000, 2000, 4000 }; // Exponential backoff

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                var delay = retryDelays[Math.Min(attempt, retryDelays.Length - 1)];
                _logger.LogWarning(
                    "Request failed, attempt {Attempt}/{MaxRetries}. Retrying in {Delay}ms",
                    attempt + 1, maxRetries + 1, delay);

                await Task.Delay(delay);
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Request timeout");
                throw;
            }
        }

        throw new Exception($"Operation failed after {maxRetries + 1} attempts");
    }
}
```

### 5.2 Circuit Breaker

```csharp
public class NumbaWalletCircuitBreaker
{
    private int _failureCount = 0;
    private DateTime _lastFailureTime;
    private readonly int _threshold = 5;
    private readonly TimeSpan _timeout = TimeSpan.FromMinutes(1);

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        if (IsOpen())
        {
            throw new CircuitBreakerOpenException(
                $"Circuit breaker is open. Retry after {_lastFailureTime.Add(_timeout)}");
        }

        try
        {
            var result = await operation();
            Reset();
            return result;
        }
        catch (Exception)
        {
            RecordFailure();
            throw;
        }
    }

    private bool IsOpen()
    {
        return _failureCount >= _threshold
            && DateTime.UtcNow < _lastFailureTime.Add(_timeout);
    }

    private void RecordFailure()
    {
        _failureCount++;
        _lastFailureTime = DateTime.UtcNow;
    }

    private void Reset()
    {
        _failureCount = 0;
    }
}
```

## 6. Testing Requirements

### 6.1 Integration Test Suite

```csharp
[TestClass]
public class NumbaWalletIntegrationTests
{
    [TestMethod]
    public async Task Should_CreateWallet_ForNewWorker()
    {
        // Arrange
        var worker = CreateTestWorker();

        // Act
        var wallet = await _client.CreateWalletAsync(worker);

        // Assert
        Assert.IsNotNull(wallet.WalletId);
        Assert.IsNotNull(wallet.Did);
        Assert.AreEqual("ACTIVE", wallet.Status);

        // Verify mapping saved
        var mapping = await _db.GetWalletMappingAsync(worker.Id);
        Assert.AreEqual(wallet.WalletId, mapping.WalletId);
    }

    [TestMethod]
    public async Task Should_RevokeCredential_WhenLicenseExpires()
    {
        // Arrange
        var credential = await CreateActiveCredential();

        // Act - Simulate expiry
        await _client.UpdateCredentialStatusAsync(
            credential.Id,
            "EXPIRED",
            "LICENSE_EXPIRED");

        // Assert
        var gates = await _gateController.GetBlockedWorkersAsync();
        Assert.Contains(credential.WorkerId, gates);
    }
}
```

## 7. Monitoring & Alerting

### 7.1 Metrics to Track

```yaml
Metrics:
  - name: wallet_creation_total
    type: counter
    description: Total wallets created

  - name: credential_issuance_duration_seconds
    type: histogram
    description: Time to issue credential

  - name: gate_verification_duration_seconds
    type: histogram
    description: Time to verify credentials at gate

  - name: webhook_processing_errors_total
    type: counter
    description: Failed webhook processing

  - name: expired_credentials_total
    type: gauge
    description: Currently expired credentials
```

### 7.2 Alerts Configuration

```yaml
Alerts:
  - name: HighWebhookFailureRate
    expr: rate(webhook_processing_errors_total[5m]) > 0.1
    severity: warning
    description: "Webhook failure rate above 10%"

  - name: SlowCredentialVerification
    expr: gate_verification_duration_seconds > 1
    severity: warning
    description: "Gate verification taking > 1 second"

  - name: NumbaWalletAPIDown
    expr: up{job="numbatwallet_api"} == 0
    severity: critical
    description: "Cannot reach NumbatWallet API"
```

---

**This contract defines exactly what Credenxia needs to implement. Once these APIs, webhooks, and background services are in place, the integration will be fully functional.**

Contact: integration@numbatwallet.com.au for clarification.