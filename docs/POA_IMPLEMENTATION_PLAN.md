# POA Implementation Plan - Complete Details

## 🔴 CRITICAL FINDINGS FROM ASSESSMENT

### 1. API Endpoints Not Working
**CLAIMED**: "Fixed API endpoints so they appear in Swagger"
**REALITY**: Swagger shows endpoints but they return empty data
**ROOT CAUSE**: Many query handlers are MISSING:
- GetWalletsQueryHandler - NOT IMPLEMENTED
- GetWalletByIdQueryHandler - NOT IMPLEMENTED
- Only GetCredentialsQueryHandler exists but returns empty

### 2. Admin Portal Shows Default Template
**CLAIMED**: "Admin portal showing actual admin functionality"
**REALITY**: Still shows "Hello, world!" template
**ROOT CAUSE**:
- Changes were made to NavMenu and Home page
- BUT application won't rebuild/recompile
- Navigation changes not reflected in running app

### 3. Multiple Container Duplicates
**ISSUE**: Multiple instances of same containers running
**CAUSE**: Multiple Aspire instances launched (2 AppHost processes found)
- Each Aspire instance creates its own set of containers

## 📋 COMPREHENSIVE IMPLEMENTATION PLAN

### Phase 1: API Definitions for SDK (COMPLETED ✅)
- Created `/docs/API_DEFINITIONS.md` with complete specifications
- GraphQL schema definitions
- REST endpoint mappings
- Data models and contracts
- Workflow examples with Mermaid diagrams
- Error handling patterns

### Phase 2: Fix Missing Backend API Components

#### 2.1 Missing Query Handlers to Implement
```csharp
// Application Layer - Query Handlers needed:
- GetWalletsQueryHandler
- GetWalletByIdQueryHandler
- GetPersonByIdQueryHandler
- GetAllPersonsQueryHandler
- GetCredentialByIdQueryHandler
- GetPersonWalletsQueryHandler
- GetWalletCredentialsQueryHandler
- GetActiveCredentialsQueryHandler
- GetRevokedCredentialsQueryHandler
- GetPresentationsByWalletQueryHandler
- GetDevicesByWalletQueryHandler
- GetAuditLogsQueryHandler
- GetTenantStatisticsQueryHandler
- GetSystemMetricsQueryHandler
- VerifyCredentialQueryHandler
```

#### 2.2 Missing Command Handlers to Implement
```csharp
// Application Layer - Command Handlers needed:
- CreateWalletCommandHandler
- UpdateWalletCommandHandler
- DeleteWalletCommandHandler
- IssueCredentialCommandHandler
- RevokeCredentialCommandHandler
- CreatePresentationCommandHandler
- RegisterDeviceCommandHandler
- SuspendWalletCommandHandler
- ReactivateWalletCommandHandler
- RotateKeysCommandHandler
- BatchIssueCredentialsCommandHandler
```

#### 2.3 GraphQL Resolvers to Fix
```csharp
// GraphQL Layer - Resolvers needed:
- WalletResolver (connect to query handlers)
- CredentialResolver (connect to command handlers)
- PersonResolver (implement data fetching)
- PresentationResolver (implement verification logic)
- MetricsResolver (real-time dashboard data)
```

### Phase 3: Admin Portal Full Implementation

#### 3.1 Master Tenant Features
**Tenant Management**
- `/Components/Pages/Tenants.razor` - Create/invite/manage tenants
- `/Components/Pages/TenantDetails.razor` - Individual tenant configuration
- `/Services/TenantService.cs` - Tenant API integration

**Analytics & KPIs Dashboard**
- `/Components/Dashboard/SystemMetrics.razor` - System-wide metrics
- `/Components/Dashboard/TenantUsage.razor` - Per-tenant statistics
- `/Components/Charts/PerformanceChart.razor` - Real-time performance
- `/Components/Charts/CostAnalytics.razor` - Cost breakdown

**Audit & Compliance**
- `/Components/Pages/AuditLogs.razor` - Enhanced with filters
- `/Components/Pages/ComplianceReports.razor` - GDPR/Privacy reports
- `/Components/Pages/SecurityEvents.razor` - Security monitoring

#### 3.2 Tenant-Level Features
**Wallet Configuration**
- `/Components/Pages/WalletDesigner.razor` - Visual wallet editor
- `/Components/Pages/AppleWalletConfig.razor` - Apple Wallet pass designer
- `/Components/Pages/GoogleWalletConfig.razor` - Google Wallet card designer
- `/Components/Pages/BrandCustomization.razor` - Colors, logos, themes

**Certificate Management**
- `/Components/Pages/Certificates.razor` - PKI certificate management
- `/Components/Pages/CertificateGeneration.razor` - Generate new certs
- `/Components/Pages/RevocationLists.razor` - CRL management
- `/Services/PKIService.cs` - Certificate operations

**Credential Templates**
- `/Components/Pages/CredentialSchemas.razor` - Schema designer
- `/Components/Pages/MDLConfiguration.razor` - Driver's license config
- `/Components/Pages/ValidationRules.razor` - Rule builder
- `/Components/Pages/DisclosurePolicies.razor` - Privacy settings

**Integration Management**
- `/Components/Pages/ApiKeys.razor` - API key generation/management
- `/Components/Pages/Webhooks.razor` - Webhook configuration
- `/Components/Pages/SDKDownloads.razor` - SDK distribution
- `/Components/Pages/RateLimits.razor` - Rate limit configuration

#### 3.3 UI Implementation Requirements

**Dark Mode Theme**
```css
/* /wwwroot/css/dark-theme.css */
:root[data-theme="dark"] {
  --bg-primary: #1a1a2e;
  --bg-secondary: #16213e;
  --bg-card: #0f3460;
  --text-primary: #e8e8e8;
  --text-secondary: #a8a8a8;
  --accent: #e94560;
  --success: #22c55e;
  --warning: #f59e0b;
  --error: #ef4444;
  --border: #2a2a3e;
}

/* Card styles */
.dashboard-card {
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.3);
}

/* Sidebar navigation */
.sidebar {
  background: var(--bg-secondary);
  border-right: 1px solid var(--border);
  width: 280px;
  height: 100vh;
}

/* Data tables */
.data-table {
  background: var(--bg-card);
  border-radius: 8px;
  overflow: hidden;
}
```

**Dashboard Components**
- Real-time metrics cards with animations
- Interactive charts using Chart.js
- Activity feed with WebSocket updates
- Alert notifications with toast messages
- Data grids with sorting/filtering/pagination

### Phase 4: Fix Infrastructure Issues

#### 4.1 Docker/Aspire Orchestration Fix
```bash
#!/bin/bash
# /scripts/start-clean.sh

# Kill any existing Aspire processes
echo "Stopping existing processes..."
pkill -f "NumbatWallet.AppHost"
sleep 2

# Clean up Docker containers
echo "Cleaning up containers..."
docker container stop $(docker container ls -q --filter name=numbatwallet) 2>/dev/null
docker container rm $(docker container ls -aq --filter name=numbatwallet) 2>/dev/null

# Clean up volumes (optional)
# docker volume prune -f

# Start fresh
echo "Starting fresh instance..."
dotnet run --project src/NumbatWallet.AppHost
```

#### 4.2 Health Check Implementation
```csharp
// Health check endpoints
- /health/live - Liveness probe
- /health/ready - Readiness probe
- /health/startup - Startup probe

// Database health check
services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "database")
    .AddRedis(redisConnection, name: "cache")
    .AddAzureBlobStorage(storageConnection, name: "storage");
```

### Phase 5: Multi-tenancy Implementation

#### 5.1 Database Isolation
```sql
-- Per-tenant database schema
CREATE DATABASE tenant_{tenant_id};

-- Tenant configuration table
CREATE TABLE tenants (
    tenant_id UUID PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    domain VARCHAR(255) UNIQUE,
    database_name VARCHAR(255) NOT NULL,
    connection_string TEXT,
    configuration JSONB,
    status VARCHAR(50),
    created_at TIMESTAMP,
    updated_at TIMESTAMP
);
```

#### 5.2 Tenant Resolution Middleware
```csharp
public class TenantResolutionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Extract tenant from header/subdomain/API key
        var tenantId = ExtractTenantId(context);

        // Set tenant context
        context.Items["TenantId"] = tenantId;

        // Configure DbContext with tenant connection
        var connectionString = await GetTenantConnectionString(tenantId);
        context.RequestServices.GetService<DbContextOptions>()
            .UseSqlServer(connectionString);
    }
}
```

### Phase 6: Security & Compliance

#### 6.1 Authentication Implementation
- Azure AD/Entra ID for officers
- ServiceWA mock for citizens
- API key authentication for SDKs
- JWT token validation

#### 6.2 Certificate Management
- Generate IACA certificates
- Configure trust chains
- Implement revocation checking
- Key rotation automation

#### 6.3 Audit Logging
- All API operations logged
- User actions tracked
- Data access audited
- Compliance reports generated

## 🎯 IMPLEMENTATION PRIORITIES & TIMELINE

### Week 1: Core Backend (Days 1-5)
**Day 1-2: Fix API**
- Implement all missing query handlers (15+ handlers)
- Implement all missing command handlers (10+ handlers)
- Fix GraphQL resolvers
- Add seed data for testing

**Day 3: Admin Portal Base**
- Implement dark mode theme
- Create layout components
- Add navigation structure
- Set up routing

**Day 4-5: Admin Features**
- Dashboard with real metrics
- Tenant management pages
- Wallet management pages
- Credential management pages

### Week 2: Integration (Days 6-10)
**Day 6-7: Multi-tenancy**
- Per-tenant database isolation
- Tenant resolution middleware
- Tenant switching UI
- Configuration per tenant

**Day 8-9: Certificate & Security**
- PKI infrastructure setup
- Certificate generation UI
- API key management
- Authentication flows

**Day 10: Wallet Designer**
- Visual wallet editor
- Apple Wallet configuration
- Google Wallet configuration
- Brand customization

### Week 3: Polish & Testing (Days 11-15)
**Day 11-12: Performance**
- Database indexing
- Query optimization
- Caching implementation
- Load testing

**Day 13-14: Security**
- Security scanning
- Penetration testing
- Vulnerability fixes
- Compliance checks

**Day 15: Documentation**
- API documentation
- Admin user guide
- SDK integration guide
- Deployment guide

### Week 4-5: POA Demonstration
**Week 4: Demo Preparation**
- Demo environment setup
- Test data preparation
- Scenario walkthroughs
- Backup systems

**Week 5: Live Demo**
- ServiceWA integration demo
- Credential issuance demo
- Offline verification demo
- Admin portal demo

## 📦 DELIVERABLES CHECKLIST

### Backend API
- [ ] All query handlers implemented (15+)
- [ ] All command handlers implemented (10+)
- [ ] GraphQL resolvers working
- [ ] REST endpoints functional
- [ ] Database migrations working
- [ ] Seed data available

### Admin Portal
- [ ] Dark mode theme implemented
- [ ] Dashboard with real-time data
- [ ] Tenant management complete
- [ ] Wallet management functional
- [ ] Credential management working
- [ ] Certificate management ready
- [ ] Wallet designer operational
- [ ] Integration management done

### Infrastructure
- [ ] Single Aspire instance running
- [ ] No duplicate containers
- [ ] Health checks implemented
- [ ] Service discovery working
- [ ] Monitoring configured

### Security
- [ ] Authentication working
- [ ] Authorization implemented
- [ ] API key management
- [ ] Certificate infrastructure
- [ ] Audit logging active
- [ ] Encryption configured

### Documentation
- [ ] API definitions complete
- [ ] GraphQL schema documented
- [ ] REST endpoints documented
- [ ] Admin portal guide
- [ ] SDK integration guide
- [ ] Deployment instructions

## 🚨 RISK MITIGATION

| Risk | Impact | Mitigation |
|------|--------|------------|
| Query handlers incomplete | HIGH | Implement with TDD, test each handler |
| Admin portal not loading | HIGH | Fix build issues, ensure proper compilation |
| Container duplicates | MEDIUM | Cleanup script, proper orchestration |
| Performance issues | MEDIUM | Caching, indexing, query optimization |
| Security vulnerabilities | HIGH | OWASP scanning, penetration testing |
| Integration failures | HIGH | Mock services, fallback mechanisms |
| Demo environment issues | HIGH | Backup environment, rehearsal runs |

## 🎯 SUCCESS CRITERIA

### Technical Requirements
- ✅ All API endpoints return real data
- ✅ Admin portal fully functional with dark mode
- ✅ No duplicate containers or processes
- ✅ Multi-tenant isolation working
- ✅ Certificate management operational
- ✅ Performance <500ms response time

### Business Requirements
- ✅ SDK integration documented
- ✅ Wallet lifecycle management
- ✅ Credential issuance/revocation
- ✅ Offline verification
- ✅ Audit compliance
- ✅ Security standards met

### POA Demo Requirements
- ✅ Live demonstration ready
- ✅ All features working
- ✅ Performance benchmarks met
- ✅ Security validated
- ✅ Documentation complete
- ✅ Handover package ready

## 📝 NOTES & REMINDERS

1. **API First**: SDK team is waiting for API definitions - COMPLETED
2. **Dark Mode**: Use the provided screenshot as reference
3. **Real Data**: Ensure all endpoints return actual data, not empty arrays
4. **Clean Start**: Always kill existing processes before starting new ones
5. **Test Coverage**: Minimum 85% coverage required
6. **Documentation**: Keep updating as we implement

---

*This plan ensures delivery of a production-ready POA solution for the Western Australia Digital Wallet tender (DPC2142)*