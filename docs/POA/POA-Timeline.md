# POA Project Timeline & Dependencies

## 📅 Timeline Overview

### Week 1: Foundation (Oct 1-4, 2025)
**Milestone Due: October 4**

#### Day 1-2 (Oct 1-2)
- POA-001: Azure subscription setup → Blocks all infrastructure
- POA-009a: Bicep main template
- POA-010: Database schema design
- POA-012: Backend project structure → Blocks POA-013, POA-014, POA-018

#### Day 2-3 (Oct 2-3)
- POA-002: PostgreSQL setup (depends on POA-001)
- POA-003: Container Registry (depends on POA-001)
- POA-004: VNet setup (depends on POA-001)
- POA-015: Flutter SDK init → Blocks POA-016, POA-017, POA-031

#### Day 3-4 (Oct 3-4)
- POA-005: Key Vault (depends on POA-001)
- POA-009b-d: Bicep modules (depends on POA-009a)
- POA-018: Docker containers (depends on POA-012)
- POA-019: Deploy to Container Apps (depends on POA-018, POA-007)

### Week 2: Integration (Oct 7-11, 2025)
**Milestone Due: October 11**

#### Day 1-2 (Oct 7-8)
- POA-021: OIDC authentication → Blocks POA-022, POA-023, POA-031

#### Day 2-3 (Oct 8-9)
- POA-022: Mock WA IdX (depends on POA-021)
- POA-025: CQRS implementation (depends on POA-024)

#### Day 3-5 (Oct 9-11)
- POA-023: Tenant resolution (depends on POA-021)
- POA-026: Credential API endpoints (depends on POA-025)
- POA-031: Flutter SDK auth (depends on POA-021)

### Week 3: Demo (Oct 14-18, 2025)
**Milestone Due: October 18**
- POA-041: Demo mobile app
- POA-048: Demo presentation

### Week 4: Testing (Oct 21-25, 2025)
**Milestone Due: October 25**
- POA-061: UAT support
- POA-066: Performance testing

### Week 5: Evaluation (Oct 28 - Nov 1, 2025)
**Milestone Due: November 1**
- POA-075: Handover package
- POA-077: Final presentation

## 🔗 Critical Path Dependencies

```
POA-001 (Azure Setup)
├── POA-002 (PostgreSQL)
├── POA-003 (ACR) → POA-009 (CI/CD)
├── POA-004 (VNet) → POA-006 (App Gateway)
├── POA-005 (Key Vault)
├── POA-007 (App Service) → POA-019 (Deploy)
└── POA-008 (Log Analytics)

POA-012 (Backend Structure)
├── POA-013 (Health checks)
├── POA-014 (Swagger/OpenAPI)
└── POA-018 (Docker) → POA-019 (Deploy)

POA-015 (Flutter SDK)
├── POA-016 (Core models)
├── POA-017 (HTTP client)
└── POA-031 (Authentication)

POA-021 (OIDC Auth)
├── POA-022 (Mock IdX)
├── POA-023 (Tenant resolution)
└── POA-031 (Flutter auth)
```

## ✅ Key Success Factors

1. **Week 1 Critical**: POA-001, POA-012, POA-015 must complete early
2. **Parallel Work**: Infrastructure and development can proceed in parallel
3. **Integration Points**: Week 2 depends heavily on Week 1 completion
4. **Demo Ready**: Week 3 requires all core features working
