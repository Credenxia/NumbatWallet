# CRITICAL IMPLEMENTATION PLAN - NumbatWallet POA
Generated: September 23, 2025
Status: CRITICAL - 80% of work remains with 4 weeks deadline

## 🔴 CURRENT REALITY
- **Structure**: 80% complete
- **Functionality**: 5% complete
- **Working Features**: 0
- **Time Remaining**: 4 weeks
- **Required Completion Rate**: 20% per week

## 📋 PHASE 1: FOUNDATION FIXES (Days 1-2)
**Goal**: Get basic infrastructure working

### Day 1 Morning: Database & Migrations
```bash
# Tasks:
1. Create manual migration SQL scripts
2. Setup database schema directly
3. Create seed data
4. Test database connectivity
```

**Implementation Steps:**
1. Create SQL script for all tables
2. Run script directly on PostgreSQL
3. Update MigrationHelper to skip EF migrations
4. Implement DatabaseSeeder with test data
5. Verify all tables created correctly

### Day 1 Afternoon: Fix API Controllers
```csharp
// Fix controller registration in Program.cs
builder.Services.AddControllers()
    .AddApplicationPart(typeof(TenantController).Assembly);
```

**Implementation Steps:**
1. Create TenantController with CRUD operations
2. Create WalletController with basic operations
3. Create CredentialController with issuance
4. Register all controllers properly
5. Verify Swagger UI loads

### Day 2 Morning: Implement First Real Handler
```csharp
// CreateTenantCommandHandler - COMPLETE IMPLEMENTATION
public async Task<Guid> Handle(CreateTenantCommand command)
{
    // Real implementation, not mock
    var tenant = new Tenant(...);
    await _repository.AddAsync(tenant);
    await _unitOfWork.CommitAsync();
    return tenant.Id;
}
```

**Implementation Steps:**
1. Implement CreateTenantCommandHandler
2. Implement GetTenantByIdQueryHandler
3. Implement UpdateTenantCommandHandler
4. Add proper validation
5. Test with Postman/curl

### Day 2 Afternoon: Connect Admin Portal
```typescript
// Configure API client properly
services.AddHttpClient<IApiClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5000");
});
```

**Implementation Steps:**
1. Fix API client configuration
2. Implement real dashboard data fetch
3. Create tenant management page
4. Test data flow end-to-end
5. Verify real data displays

## 📋 PHASE 2: CORE FEATURES (Days 3-7)
**Goal**: Implement primary business features

### Day 3: Tenant Management Complete
- [ ] Multi-tenant isolation middleware
- [ ] Tenant switching in Admin portal
- [ ] Certificate management per tenant
- [ ] Tenant-specific configurations
- [ ] Audit logging for tenant actions

### Day 4-5: Wallet Implementation
```csharp
public class WalletService
{
    // Day 4: Core wallet operations
    Task<Wallet> CreateWalletAsync(Person person, WalletTemplate template);
    Task<Wallet> GetWalletAsync(Guid walletId);
    Task UpdateWalletAsync(Wallet wallet);

    // Day 5: Template system
    Task<WalletTemplate> CreateTemplateAsync(WalletTemplateDto dto);
    Task<AppleWalletPass> GenerateApplePassAsync(Wallet wallet);
    Task<GoogleWalletPass> GenerateGooglePassAsync(Wallet wallet);
}
```

### Day 6-7: Credential Issuance
```csharp
public class CredentialService
{
    // Day 6: Basic issuance
    Task<Credential> IssueCredentialAsync(IssueCredentialCommand command);
    Task<bool> VerifyCredentialAsync(Guid credentialId);
    Task RevokeCredentialAsync(Guid credentialId, string reason);

    // Day 7: Bulk operations
    Task<BulkIssueResult> BulkIssueAsync(List<IssueRequest> requests);
    Task<BulkRevokeResult> BulkRevokeAsync(List<Guid> credentialIds);
}
```

## 📋 PHASE 3: STANDARDS & COMPLIANCE (Days 8-14)
**Goal**: Implement required standards

### Day 8-9: W3C Verifiable Credentials
```csharp
public class VCService
{
    Task<JwtVc> CreateJwtVcAsync(CredentialSubject subject, Issuer issuer);
    Task<JsonLdVc> CreateJsonLdVcAsync(CredentialSubject subject, Context context);
    Task<VerificationResult> VerifyVcAsync(string vcJwt);
}
```

### Day 10-11: OpenID4VCI Implementation
```csharp
public class OpenIdVciService
{
    Task<TokenResponse> HandleTokenRequestAsync(TokenRequest request);
    Task<CredentialResponse> HandleCredentialRequestAsync(CredentialRequest request);
    Task<MetadataResponse> GetIssuerMetadataAsync();
}
```

### Day 12-13: ISO 18013-5 mDL
```csharp
public class MdlService
{
    Task<MobileDriverLicense> CreateMdlAsync(PersonInfo person, LicenseInfo license);
    Task<DeviceEngagement> InitiateDeviceEngagementAsync();
    Task<bool> ValidateMdlAsync(byte[] mdlData);
}
```

### Day 14: PKI & Certificate Management
```csharp
public class PkiService
{
    Task<X509Certificate2> IssueCertificateAsync(CertificateRequest request);
    Task<bool> ValidateCertificateChainAsync(X509Certificate2 cert);
    Task<CrlResponse> CheckRevocationStatusAsync(X509Certificate2 cert);
}
```

## 📋 PHASE 4: INTEGRATION & POLISH (Days 15-21)
**Goal**: Connect everything and ensure quality

### Day 15-16: Admin Portal Full Integration
- [ ] Real-time dashboard with SignalR
- [ ] Tenant management UI working
- [ ] Wallet builder functional
- [ ] Certificate management UI
- [ ] Batch operations interface

### Day 17-18: API Completion
- [ ] All REST endpoints implemented
- [ ] GraphQL resolvers working
- [ ] Authentication/authorization active
- [ ] Rate limiting configured
- [ ] API documentation complete

### Day 19: Security & Performance
- [ ] Security audit
- [ ] Performance optimization
- [ ] Caching implementation
- [ ] Load testing (100+ users)
- [ ] Vulnerability scanning

### Day 20-21: Testing & Documentation
- [ ] Integration tests for all features
- [ ] End-to-end test scenarios
- [ ] API documentation
- [ ] Deployment guide
- [ ] User manual

## 📋 PHASE 5: DEPLOYMENT (Days 22-28)
**Goal**: Production readiness

### Day 22-23: Azure Deployment
```bicep
// Complete Bicep templates
- Azure SQL Database
- App Services
- Key Vault
- Redis Cache
- Application Insights
```

### Day 24-25: CI/CD Pipeline
```yaml
# GitHub Actions
- Build pipeline
- Test automation
- Security scanning
- Deployment stages
- Rollback procedures
```

### Day 26-27: Final Testing
- [ ] UAT testing
- [ ] Performance validation
- [ ] Security verification
- [ ] Compliance check
- [ ] Documentation review

### Day 28: Handover
- [ ] Deployment to production
- [ ] Knowledge transfer
- [ ] Support documentation
- [ ] Monitoring setup
- [ ] Project closure

## 🎯 CRITICAL SUCCESS FACTORS

### Must Have (Week 1)
1. ✅ Database working
2. ✅ One complete CRUD flow
3. ✅ Admin portal shows real data
4. ✅ API endpoints accessible

### Should Have (Week 2)
1. ✅ Tenant management
2. ✅ Wallet operations
3. ✅ Basic credential issuance
4. ✅ Template system

### Nice to Have (Week 3)
1. ✅ Full standards compliance
2. ✅ Bulk operations
3. ✅ Advanced PKI features
4. ✅ Performance optimization

### Final Week
1. ✅ Azure deployment
2. ✅ CI/CD pipeline
3. ✅ Documentation
4. ✅ Handover ready

## 🚨 RISK MITIGATION

### High Risks
1. **Database migrations broken** → Use SQL scripts directly
2. **EF Tools incompatible** → Bypass EF migrations
3. **Time constraint** → Focus on MVP features only
4. **Integration complexity** → Test as you build

### Mitigation Strategies
1. **Daily progress checks** - Must complete planned tasks
2. **Fail fast** - If blocked >2 hours, find workaround
3. **Scope reduction** - Cut nice-to-haves if behind
4. **Parallel work** - Multiple features simultaneously

## 📊 DAILY CHECKLIST

### Every Morning
- [ ] Review daily goals
- [ ] Check blockers from yesterday
- [ ] Plan implementation order
- [ ] Set success criteria

### Every Evening
- [ ] Test implemented features
- [ ] Commit working code
- [ ] Update GitHub issues
- [ ] Document progress
- [ ] Plan next day

## 🔥 IMPLEMENTATION PRIORITIES

### Week 1 Priority: Make It Work
Focus: Get one complete flow working end-to-end

### Week 2 Priority: Core Features
Focus: Implement all primary business logic

### Week 3 Priority: Standards
Focus: Add compliance and standards

### Week 4 Priority: Production
Focus: Deploy and document

---

## NEXT IMMEDIATE ACTIONS (START NOW)

1. **Fix Database (1 hour)**
   ```sql
   -- Create tables manually
   CREATE TABLE tenants (id UUID PRIMARY KEY, ...);
   CREATE TABLE wallets (id UUID PRIMARY KEY, ...);
   ```

2. **Register Controllers (30 min)**
   ```csharp
   // In Program.cs
   builder.Services.AddControllers();
   app.MapControllers();
   ```

3. **Implement One Handler (2 hours)**
   ```csharp
   // Pick CreateTenantCommandHandler
   // Write complete implementation
   // Test with Postman
   ```

4. **Connect Admin Portal (1 hour)**
   ```csharp
   // Fix API client
   // Fetch real data
   // Display in dashboard
   ```

5. **Test End-to-End (30 min)**
   - Create tenant via API
   - View in Admin portal
   - Verify in database

**START TIME**: NOW
**FIRST MILESTONE**: Working tenant CRUD by end of day
**SUCCESS CRITERIA**: Can create and view real tenants

---
*This plan represents the MINIMUM required to deliver a working POA. Any deviation or delay will result in project failure.*