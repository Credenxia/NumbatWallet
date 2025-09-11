# POA Dependency Matrix

**Generated:** September 10, 2025  
**Purpose:** Visual representation of task dependencies and parallelization opportunities

## 🚦 Parallelization Guide

### Day 1 - Can Start Immediately (Parallel)
These tasks have NO dependencies and can all start simultaneously:

| Task | GitHub | Team | Description |
|------|--------|------|-------------|
| POA-001 | #1 | DevOps | Azure subscription (CRITICAL - blocks 7 others) |
| POA-010 | #31 | Backend | Database schema design |
| POA-012 | #10 | Backend | Backend project structure |
| POA-015 | #12 | Mobile | Flutter SDK initialization |
| POA-033 | #39 | Backend | .NET SDK setup |
| POA-035 | #41 | Frontend | TypeScript SDK setup |
| POA-081 | #42 | DevOps | **TEST: Set up test framework and CI pipeline** |

**Key Insight:** 3 teams can work independently from Day 1!

### Critical Path Items
These tasks block the most other tasks and should be prioritized:

1. **POA-001** (#1) → Blocks 7 infrastructure tasks
2. **POA-012** (#10) → Blocks backend development 
3. **POA-021** (#13) → Blocks ALL authentication work (including Flutter SDK auth)
4. **POA-025** (#16) → Blocks API endpoints

## 📊 Full Dependency Matrix

### Legend
- ✅ = Can start immediately
- ⏸️ = Blocked by another task
- 🚫 = Blocks other tasks
- ⚠️ = Cross-team dependency

### Week 1 Dependencies

| Task | Status | Blocks | Blocked By | Notes |
|------|--------|--------|-------------|-------|
| POA-001 (#1) | ✅ | 🚫 #2,3,4,5,26,28,29 | None | **CRITICAL PATH** |
| POA-002 (#2) | ⏸️ | None | #1 | PostgreSQL |
| POA-003 (#3) | ⏸️ | 🚫 #30 | #1 | Container Registry |
| POA-004 (#26) | ⏸️ | 🚫 #27 | #1 | VNet |
| POA-005 (#4) | ⏸️ | None | #1 | Key Vault |
| POA-006 (#27) | ⏸️ | None | #26 | App Gateway |
| POA-007 (#28) | ⏸️ | 🚫 #37 | #1 | App Service |
| POA-008 (#29) | ⏸️ | None | #1 | Log Analytics |
| POA-009 (#30) | ⏸️ | None | #3 | CI/CD |
| POA-009a (#5) | ⏸️ | 🚫 #6,7,8 | #1 | Bicep main |
| POA-009b (#6) | ⏸️ | 🚫 #9 | #5 | Bicep network |
| POA-009c (#7) | ⏸️ | 🚫 #9 | #5 | Bicep database |
| POA-009d (#8) | ⏸️ | 🚫 #9 | #5 | Bicep containers |
| POA-009e (#9) | ⏸️ | None | #6,7,8 | Bicep deploy |
| POA-010 (#31) | ✅ | 🚫 #32 | None | DB schema |
| POA-011 (#32) | ⏸️ | POA-024 | #31 | Migrations |
| POA-012 (#10) | ✅ | 🚫 #11,33,36 | None | **Backend structure** |
| POA-013 (#11) | ⏸️ | 🚫 #13 | #10 | Health checks |
| POA-014 (#33) | ⏸️ | None | #10 | Swagger |
| POA-015 (#12) | ✅ | 🚫 #34,35 | None | **Flutter SDK** |
| POA-016 (#34) | ⏸️ | POA-032 | #12 | Flutter models |
| POA-017 (#35) | ⏸️ | None | #12 | Flutter HTTP |
| POA-018 (#36) | ⏸️ | 🚫 #37 | #10 | Docker |
| POA-019 (#37) | ⏸️ | None | #28,36 | Deploy |
| **TESTING** | | | | |
| POA-081 (#42) | ✅ | 🚫 All tests | None | **Test framework** |
| POA-082 (#43) | ⏸️ | Unit tests | #10 | Domain tests |
| POA-083 (#44) | ⏸️ | Integration | #10 | Test harness |
| POA-084 (#45) | ⏸️ | DB tests | #2 | Test database |
| POA-085 (#46) | ⏸️ | All tests | #43 | Test builders |
| POA-086 (#47) | ⏸️ | Flutter tests | #12 | Flutter tests |
| POA-087 (#48) | ⏸️ | Infra tests | #10 | Infrastructure tests |

### Week 2 Dependencies

| Task | Status | Blocks | Blocked By | Notes |
|------|--------|--------|-------------|-------|
| POA-021 (#13) | ⏸️ | 🚫 #14,15,18 | #11 | **⚠️ CRITICAL: Blocks Flutter auth** |
| POA-022 (#14) | ⏸️ | None | #13 | Mock IdX |
| POA-023 (#15) | ⏸️ | None | #13 | Tenant resolution |
| POA-024 | ⏸️ | POA-025,030 | #32 | Domain model (no GitHub issue) |
| POA-025 (#16) | ⏸️ | 🚫 #17 | POA-024 | CQRS |
| POA-026 (#17) | ⏸️ | Testing, Demo | #16 | **API endpoints** |
| POA-031 (#18) | ⏸️ | None | #13 | **⚠️ Flutter auth (cross-team)** |
| POA-033 (#39) | ✅ | 🚫 #40 | None | .NET SDK |
| POA-034 (#40) | ⏸️ | None | #39 | .NET client |
| POA-035 (#41) | ✅ | None | None | TypeScript SDK |
| **TESTING** | | | | |
| POA-088 (#49) | ⏸️ | None | #16 | Credential tests |
| POA-089 (#50) | ⏸️ | None | #13 | Auth tests |
| POA-090 (#51) | ⏸️ | E2E tests | #17 | API tests |
| POA-091 (#52) | ⏸️ | None | #15 | Tenant tests |
| POA-092 (#53) | ⏸️ | None | POA-029 | Security tests |
| POA-093 (#54) | ⏸️ | None | #40 | SDK tests |
| POA-094 (#55) | ⏸️ | Load tests | #51 | Performance baseline |

## 🎯 Optimal Execution Order

### Phase 1: Parallel Starts (Day 1)
```
Parallel:
├── Team DevOps: POA-001 (Azure) [PRIORITY] + POA-081 (Test Framework)
├── Team Backend: POA-012 (Structure) + POA-010 (Schema)
├── Team Mobile: POA-015 (Flutter SDK)
└── Team SDK: POA-033 (.NET) + POA-035 (TypeScript)

CRITICAL: POA-081 (Test Framework) MUST complete Day 1 for TDD compliance
```

### Phase 2: Infrastructure Sprint (Day 2-3)
```
After POA-001:
├── POA-002 (PostgreSQL)
├── POA-003 (Container Registry) → POA-009 (CI/CD)
├── POA-004 (VNet) → POA-006 (App Gateway)
├── POA-005 (Key Vault)
├── POA-007 (App Service)
├── POA-008 (Log Analytics)
└── POA-009a (Bicep) → POA-009b,c,d → POA-009e
```

### Phase 3: Backend Development (Day 2-4)
```
After POA-012:
├── POA-013 (Health) → POA-021 (OIDC) [CRITICAL]
├── POA-014 (Swagger)
└── POA-018 (Docker) → POA-019 (Deploy)

After POA-010:
└── POA-011 (Migrations) → POA-024 (Domain) → POA-025 (CQRS) → POA-026 (APIs)
```

### Phase 4: SDK Development (Day 2-4)
```
After POA-015:
├── POA-016 (Models) → POA-032 (Storage)
└── POA-017 (HTTP client)

After POA-033:
└── POA-034 (.NET client)

POA-035 continues independently
```

### Phase 5: Integration (Week 2)
```
After POA-021:
├── POA-022 (Mock IdX)
├── POA-023 (Tenant)
└── POA-031 (Flutter Auth) [Cross-team handoff]
```

## ⚠️ Critical Cross-Team Dependencies

1. **Backend → Mobile**: POA-021 (#13) must complete before POA-031 (#18)
   - **Impact**: Flutter team blocked on auth module
   - **Mitigation**: Flutter team should mock auth interface

2. **Infrastructure → Backend**: POA-007 (#28) must complete before POA-019 (#37)
   - **Impact**: Cannot deploy without App Service
   - **Mitigation**: Use local Docker for testing

3. **Backend → Demo**: POA-026 (#17) must complete before demo preparation
   - **Impact**: No real data for demo
   - **Mitigation**: Create mock data scenarios

## 📈 Parallelization Metrics

- **Maximum parallel tasks (Day 1)**: 6 tasks across 3 teams
- **Critical path length**: 8 tasks (POA-001 → ... → POA-026)
- **Cross-team dependencies**: 1 critical (Backend OIDC → Flutter Auth)
- **Independent workstreams**: 3 (Infrastructure, Backend, SDKs)

## 🚀 Recommendations

1. **Start immediately**: POA-001, POA-010, POA-012, POA-015, POA-033, POA-035
2. **Prioritize**: POA-001 (blocks 7 tasks), POA-012 (blocks backend)
3. **Coordinate**: Backend and Mobile teams on POA-021 → POA-031 handoff
4. **Mock early**: Flutter team should mock auth while waiting for POA-021
5. **Parallelize SDKs**: All three SDKs can develop independently

## 📋 Team Assignment Guide

### DevOps Team (Infrastructure)
- **Day 1**: Start POA-001 (CRITICAL)
- **Day 2-3**: Execute POA-002 through POA-008 in parallel
- **Day 3-4**: Bicep templates and deployment

### Backend Team
- **Day 1**: Start POA-012 (structure) AND POA-010 (schema) 
- **Day 2**: POA-013, POA-014, POA-011
- **Day 3-4**: POA-018, POA-024, POA-025
- **Week 2**: POA-021 (CRITICAL for Mobile team)

### Mobile Team (Flutter)
- **Day 1-2**: POA-015 (init)
- **Day 2-3**: POA-016, POA-017 in parallel
- **Day 3-4**: POA-032 (can mock auth)
- **Week 2**: POA-031 (wait for POA-021)

### SDK Teams
- **.NET**: POA-033 → POA-034 (independent)
- **TypeScript**: POA-035 (completely independent)

---

**Note**: This matrix is based on the GitHub issues created. Some POA tasks without GitHub issues are noted but not fully tracked.