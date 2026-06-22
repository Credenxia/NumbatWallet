# TODO Tracking - NumbatWallet Backend

**Last Updated:** October 1, 2025
**Total TODOs:** 49
**Status:** Organized by priority and implementation timeline

## Quick Reference

- ✅ **Completed (Recent):** 12 tasks (WalletBuilder TODOs resolved, CS0414 warnings fixed)
- 🟡 **Deferred:** 18 tasks (external dependencies)
- 🟢 **Actionable:** 22 tasks (ready to implement) - includes 9 new Certificate Management TODOs
- 📝 **Documentation:** All TODOs categorized in this tracking document

---

## Recently Completed (October 1, 2025 Session)

1. ✅ WalletBuilder TODOs (5 items) - Replaced hardcoded values with IConfiguration
   - Apple TeamIdentifier → Configuration["WalletProviders:Apple:TeamIdentifier"]
   - Google IssuerId → Configuration["WalletProviders:Google:IssuerId"]
   - Web wallet options (Theme, PrimaryColor, EnableQrCode, etc.) → Configuration

2. ✅ CS0414 Warning Suppressions (3 Blazor pages) - Zero warnings achieved
   - Reports.razor - Added helper methods for field access
   - BackupRestore.razor - Added 6 helper methods (56 fields organized)
   - KeyRotation.razor - Added 6 helper methods (36 fields organized)

3. ✅ Dashboard.razor - Migrated from mock data to IDashboardService integration

4. ✅ TODO Tracking Documentation - Updated with all 49 TODOs categorized

### Previous Session Completions:
1. ✅ RevocationRegistry user context tracking
2. ✅ Credential schema DTO property
3. ✅ Organization contact info fields
4. ✅ GetCredentialsByIssuerQuery implementation
5. ✅ TenantCreated & TenantDeleted events
6. ✅ Credential expiry reminders
7. ✅ Database schema varchar(20) fix

---

## Deferred TODOs (External Dependencies - 18 items)

### Strawberry Shake / GraphQL Client (7 TODOs)
**Reason:** Waiting for Strawberry Shake configuration and schema generation
**Files:**
- `Web.Admin/Services/GraphQLAuditLogService.cs:28, 58, 76, 96`
- `Web.Admin/Services/GraphQLTenantService.cs:58, 74, 90`

**Action:** Implement after GraphQL schema is finalized and Strawberry Shake is configured.

---

### HotChocolate API Stability (4 TODOs)
**Reason:** Waiting for stable HotChocolate APIs
**Files:**
- `Web.Api/Extensions/GraphQLExtensions.cs:89` - DiagnosticEventListener API
- `Web.Api/Extensions/GraphQLExtensions.cs:126` - Interceptor API
- `Web.Api/Extensions/GraphQLExtensions.cs:201` - Type converter API
- `Web.Api/Extensions/GraphQLExtensions.cs:75` - GraphQL Voyager package

**Action:** Monitor HotChocolate releases for stable APIs.

---

### External Service Integration (3 TODOs)
**Reason:** Requires external service accounts and configuration

#### Identity Verification Service
- **File:** `Application/Services/PersonService.cs:145`
- **Description:** Integrate with external identity verification provider
- **Dependencies:** Service provider selection, API keys, compliance review

#### Push Notifications (Firebase/APNS)
- **File:** `Infrastructure/Services/NotificationService.cs:25`
- **Description:** Integrate Firebase and APNS for mobile push notifications
- **Dependencies:** Firebase/APNS setup, certificates, service accounts

#### Background Job Scheduler (Hangfire)
- **File:** `Application/EventHandlers/CredentialIssuedEventHandler.cs:93`
- **Description:** Integrate Hangfire for scheduled jobs
- **Dependencies:** Package selection (Hangfire vs Quartz), Redis/SQL Server setup

---

### Package Updates (2 TODOs)
**Reason:** Waiting for newer versions of dependencies

#### Npgsql DataSource Builder
- **File:** `Infrastructure/Azure/AzurePostgreSQLConfiguration.cs:46`
- **Description:** Migrate to NpgsqlDataSourceBuilder when upgrading Npgsql
- **Current Version:** Check Directory.Packages.props
- **Action:** Upgrade Npgsql to latest version

#### Seq Logging
- **File:** `AppHost/Program.cs:53`
- **Description:** Enable Seq when package is added to .NET Aspire
- **Dependencies:** .NET Aspire Seq package availability

---

### Architecture Decisions Needed (2 TODOs)

#### GraphQL Types Implementation
- **Files:**
  - `Web.Api/DependencyInjection/ServiceCollectionExtensions.cs:7, 123`
  - `Web.Api/GraphQL/Admin/AdminQuery.cs:55, 121`
- **Description:** Implement GraphQL types for AuditLog and User entities
- **Action:** Define GraphQL schema strategy, implement type mappings

---

## Actionable TODOs (Ready to Implement - 22 items)

### High Priority - Certificate Management (9 items) **NEW**

#### Certificate Management UI Integration
- **File:** `Web.Admin/Components/Pages/Certificates/CertificateManagement.razor`
- **Lines:** 363, 375, 403, 429, 451, 456, 461, 466, 471
- **Effort:** 8-12 hours total
- **Description:** Complete certificate management page implementation
  1. Line 363: Load certificates from service
  2. Line 375: Load statistics from service
  3. Line 403: Parse X.509 certificate and create preview
  4. Line 429: Upload certificate to service
  5. Line 451: Apply filters
  6. Line 456: Show certificate details modal
  7. Line 461: Revoke certificate
  8. Line 466: Validate certificate chain and revocation status
  9. Line 471: Delete certificate
- **Blockers:** Needs ICertificateManagementService implementation
- **Priority:** **HIGH** - Admin portal feature is incomplete without this

---

### High Priority - Security (3 items)

#### 1. PIN Verification
- **File:** `Application/Commands/Wallets/Handlers/ActivateWalletCommandHandler.cs:43`
- **Effort:** 2-3 hours
- **Description:** Implement PIN verification when activating wallets
- **Blockers:** None - security layer exists

#### 2. Signature Verification
- **File:** `Application/Commands/Credentials/Handlers/VerifyCredentialCommandHandler.cs:75`
- **Effort:** 3-4 hours
- **Description:** Implement cryptographic signature verification for credentials
- **Blockers:** None - HSM service ready

#### 3. Biometric Verification
- **File:** `Application/Commands/Credentials/Handlers/VerifyCredentialCommandHandler.cs:88`
- **Effort:** 3-4 hours
- **Description:** Check biometric verification status during credential verification
- **Blockers:** Platform-specific biometric APIs

---

### Medium Priority - Audit & Compliance (4 items)

#### 4-7. Audit Service Persistence
- **Files:**
  - `Infrastructure/Services/AuditService.cs:18` - Add persistent storage
  - `Infrastructure/Services/AuditService.cs:47` - Query from storage
  - `Infrastructure/Services/AuditService.cs:57` - Query unmask operations
  - `Infrastructure/Services/AuditService.cs:66` - Calculate statistics
- **Effort:** 4-6 hours total
- **Description:** Implement persistent audit log storage (Azure Table Storage or dedicated DB)
- **Blockers:** None - architecture decision needed

---

### Medium Priority - Notifications (3 items)

#### 8. Priority Queue Processing
- **File:** `Infrastructure/Services/NotificationService.cs:36`
- **Effort:** 2-3 hours
- **Description:** Add priority queue for urgent notifications
- **Blockers:** None

#### 9. Organization Batch Notifications
- **File:** `Infrastructure/Services/NotificationService.cs:47`
- **Effort:** 2-3 hours
- **Description:** Implement organization member lookup and batch notifications
- **Blockers:** None

#### 10. Scheduled Reminders
- **File:** `Infrastructure/Services/NotificationService.cs:60`
- **Effort:** 3-4 hours (if using Hangfire)
- **Description:** Integrate with background job scheduler for scheduled reminders
- **Dependencies:** Hangfire/Quartz decision

---

### Low Priority - Architecture Improvements (4 items)

#### 11-12. Specification Pattern
- **Files:**
  - `Application/Services/PersonService.cs:29`
  - `Application/Services/CredentialService.cs:39`
- **Effort:** 3-4 hours total
- **Description:** Implement specification pattern for complex queries
- **Blockers:** None - good refactoring opportunity

#### 13. Person Update Logic
- **File:** `Application/Services/PersonService.cs:84`
- **Effort:** 1-2 hours
- **Description:** Implement person update logic in domain entity
- **Blockers:** None

#### 14. Statistics Service
- **Files:**
  - `Application/EventHandlers/CredentialIssuedEventHandler.cs:14, 46`
- **Effort:** 4-6 hours
- **Description:** Extend IStatisticsService for credential tracking
- **Blockers:** Service design needed

---

### Low Priority - Service Registration (2 items)

#### 15-16. Background Jobs Registration
- **Files:**
  - `Application/DependencyInjection/ServiceCollectionExtensions.cs:81, 87`
  - `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs:229, 231`
- **Effort:** 1-2 hours
- **Description:** Register background jobs and notification channels in DI
- **Dependencies:** Hangfire implementation

---

### Documentation Fixes (1 item)

#### 17. API Documentation DTO Fix
- **File:** `Web.Api/Documentation/ApiDocumentation.cs:129`
- **Effort:** 15 minutes
- **Description:** Fix API documentation to match actual DTO structure
- **Blockers:** None

---

### Low Priority - HSM (1 item)

#### 18. HSM Key Update
- **File:** `Infrastructure/Services/HsmService.cs:685`
- **Effort:** 2-3 hours
- **Description:** Implement key update mechanism - IHsmProvider doesn't have UpdateKeyAsync
- **Blockers:** HSM provider interface extension needed

---

## Implementation Roadmap

### Phase 1: Certificate Management & Security (Week 1-2) - 16-22 hours **UPDATED**
- **Certificate Management (9 TODOs):** ICertificateManagementService + Admin UI integration
- PIN verification
- Signature verification
- Biometric verification

### Phase 2: Audit & Compliance (Week 2-3) - 4-6 hours
- Audit service persistence
- Query implementations

### Phase 3: Notifications (Week 3-4) - 7-10 hours
- Priority queue
- Batch notifications
- Scheduled reminders (if Hangfire ready)

### Phase 4: Architecture Improvements (Week 4-5) - 8-12 hours
- Specification pattern
- Statistics service
- Person update logic

### Phase 5: External Dependencies (TBD)
- Monitor and implement when dependencies are ready

---

## Metrics

- **Total TODOs:** 49 (up from 41 - added 9 Certificate Management, reduced 1 by fixing WalletBuilder)
- **Total Effort Estimate (Actionable):** 35-52 hours (includes 8-12 hours for Certificate Management)
- **Total Effort Estimate (Deferred):** 20-30 hours (when dependencies ready)
- **High Priority:** 16-22 hours (Certificate Management + Security)
- **Medium Priority:** 13-20 hours (Audit + Notifications)
- **Low Priority:** 6-10 hours (Architecture + Misc)

---

## Notes

1. **Security items** should be prioritized as they're blocking wallet and credential operations
2. **Audit persistence** is critical for compliance requirements
3. **GraphQL/Strawberry Shake** items can wait until admin portal UI development
4. **Background jobs** decision should be made soon (Hangfire vs Quartz)
5. **External services** (Firebase, APNS, identity verification) need architecture review

---

*Generated: September 2025 | Maintained by: Development Team*