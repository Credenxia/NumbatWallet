# Credenxia-NumbatWallet Integration Package

## Delivery Date: September 22, 2025

### Package Contents

This package contains the complete technical documentation required for Credenxia to integrate with NumbatWallet's digital identity platform.

## 📦 Documents Included

### 1. CREDENXIA_INTEGRATION_GUIDE.md
**Purpose**: High-level integration guide for developers
**Sections Covered**:
- Authentication & API Key Setup
- Worker Identity Management
- Credential Operations (Issue, Verify, Suspend, Revoke)
- Webhook Integration for Real-time Updates
- Testing Strategies (Unit, Integration, E2E)
- Security Best Practices
- Production Deployment Checklist

### 2. CREDENXIA_API_CONTRACT.md
**Purpose**: Detailed technical specification
**Specifications Included**:
- Complete REST API Endpoints (11 endpoints)
- Request/Response JSON Schemas
- Database Schema Requirements (4 tables)
- Security Implementation Details
- Error Code Reference
- Migration Strategies
- Monitoring & SLA Requirements

## 🚀 Quick Start for Credenxia Team

### Step 1: Review Prerequisites
- .NET 8.0+ or Node.js 18+ development environment
- PostgreSQL 14+ database
- HTTPS-enabled development environment
- Azure/AWS account for production deployment

### Step 2: Implementation Order
1. **Week 1**: Database schema setup & API authentication
2. **Week 2**: Worker management endpoints (GET /workers/{id})
3. **Week 3**: Webhook receiver implementation
4. **Week 4**: Integration testing with NumbatWallet sandbox

### Step 3: Key Integration Points

#### API Endpoints to Implement (Priority Order)
1. `GET /v1/workers/{workerId}` - Worker information retrieval
2. `GET /v1/workers/{workerId}/licenses` - License information
3. `GET /v1/workers/{workerId}/trainings` - Training records
4. `POST /v1/webhooks/numbatwallet` - Webhook receiver
5. `GET /v1/health` - Health check endpoint

#### NumbatWallet APIs You'll Call
- `POST /api/v1/credentials/issue` - Issue new credentials
- `POST /api/v1/credentials/verify` - Verify credentials
- `POST /api/v1/credentials/revoke` - Revoke credentials
- `POST /api/v1/webhooks/register` - Register webhook endpoints

## 🔐 Security Implementation Checklist

- [ ] Generate and securely store API keys
- [ ] Implement HMAC-SHA256 request signing
- [ ] Set up TLS 1.3 for all API communications
- [ ] Configure IP whitelisting for production
- [ ] Implement rate limiting (100 req/min)
- [ ] Set up audit logging for all API calls

## 📊 Testing Requirements

### Minimum Test Coverage
- Unit Tests: 80% coverage
- Integration Tests: All API endpoints
- Security Tests: Authentication & authorization
- Performance Tests: 100 concurrent requests

### Test Environments
1. **Development**: Use mock NumbatWallet responses
2. **Staging**: Connect to NumbatWallet sandbox
3. **Production**: Gradual rollout with monitoring

## 📞 Support & Communication

### Technical Questions
- Primary: NumbatWallet Integration Team
- Email: integration@numbatwallet.com.au
- Slack: #numbatwallet-integration

### Weekly Sync Meetings
- When: Thursdays 2:00 PM AWST
- Where: Teams/Zoom (link to be provided)
- Agenda: Progress review, blocker resolution

### Escalation Path
1. Technical Lead: Rodrigo Miranda
2. Product Owner: [To be assigned]
3. Security Team: security@numbatwallet.com.au

## 📅 Integration Timeline

### Phase 1: Foundation (Weeks 1-2)
- Database setup
- API authentication implementation
- Basic worker endpoint

### Phase 2: Core Integration (Weeks 3-4)
- Complete worker information APIs
- Webhook implementation
- Initial testing with sandbox

### Phase 3: Security & Performance (Weeks 5-6)
- Security hardening
- Performance optimization
- Load testing

### Phase 4: Production Readiness (Weeks 7-8)
- UAT with selected workers
- Production deployment
- Monitoring setup

## ⚠️ Important Assumptions

These documents assume:
1. Credenxia manages workforce data for high-risk industries (mining, construction)
2. Workers require digital credentials for site access
3. Real-time verification is critical for gate control
4. Existing worker data needs migration support
5. Multi-tenant architecture with data isolation

## 🔄 Version Control

| Document | Version | Last Updated | Changes |
|----------|---------|--------------|---------|
| INTEGRATION_GUIDE | 1.0 | Sep 22, 2025 | Initial release |
| API_CONTRACT | 1.0 | Sep 22, 2025 | Initial release |

## ✅ Delivery Checklist

- [x] Integration Guide Document
- [x] API Contract Specification
- [x] Security Implementation Details
- [x] Database Schema Definitions
- [x] Testing Requirements
- [x] Migration Strategies
- [ ] Sandbox Environment Access (pending)
- [ ] API Keys Generation (pending)

## 🎯 Success Criteria

Integration is considered successful when:
1. All API endpoints are implemented and tested
2. Webhook communication is bidirectional and reliable
3. Security audit passes with no critical findings
4. Performance meets SLA (< 500ms response time)
5. 100 workers successfully using digital credentials

## 📝 Notes for Credenxia Developers

1. **Start Simple**: Begin with read-only worker information endpoints
2. **Use Mocks**: Develop against mock NumbatWallet responses initially
3. **Security First**: Implement authentication before any functional code
4. **Log Everything**: Comprehensive logging will help during integration
5. **Ask Questions**: Better to clarify assumptions early than refactor later

---

## Appendix: Quick Reference

### NumbatWallet Sandbox Environment
```
Base URL: https://sandbox.numbatwallet.com.au/api
API Version: v1
Request Timeout: 30 seconds
Rate Limit: 100 requests/minute
```

### Required Headers for All API Calls
```
Content-Type: application/json
X-API-Key: {YOUR_API_KEY}
X-Signature: {HMAC_SIGNATURE}
X-Request-ID: {UUID}
```

### Sample HMAC Signature Generation (C#)
```csharp
var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
var nonce = Guid.NewGuid().ToString("N");
var message = $"{method}\n{path}\n{timestamp}\n{nonce}\n{body}";
using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret));
var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(message)));
```

---

*This package prepared for Credenxia Development Team*
*Delivery Date: September 22, 2025*
*Contact: Rodrigo Miranda - NumbatWallet Integration Lead*