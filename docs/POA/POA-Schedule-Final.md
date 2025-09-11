# POA Schedule - Final Version
**Start:** September 15, 2025 (Monday)  
**End:** October 31, 2025 (Friday)  
**Resources:** 2 people (R1: Architect/Dev, R2: Developer)

## Critical Rules Applied
1. ✅ POA-001 starts Day 1 (Sept 15)
2. ✅ NO weekend work (skip Sept 20-21, 27-28, Oct 4-5, 11-12, 18-19, 25-26)
3. ✅ Start and End dates are INCLUSIVE
4. ✅ No overlapping dependencies
5. ✅ 2 resources working in parallel

## Complete Task Schedule

### Week 0: September 15-19 (5 days)

| Date | Issue # | Task | Resource | Start | End | Notes |
|------|---------|------|----------|-------|-----|-------|
| Sept 15 Mon | #1 | POA-001: Azure subscription | R1 | Sept 15 | Sept 15 | DAY 1 - Foundation |
| Sept 15 Mon | #10 | POA-012: Backend structure | R2 | Sept 15 | Sept 15 | Blocks #11, #33, #36 |
| Sept 16 Tue | #31 | POA-010: Database schema | R1 | Sept 16 | Sept 16 | Blocks #32 |
| Sept 16 Tue | #12 | POA-015: Flutter SDK init | R2 | Sept 16 | Sept 16 | Blocks #34, #35 |
| Sept 17 Wed | #2 | POA-002: PostgreSQL | R1 | Sept 17 | Sept 17 | Depends on #1 |
| Sept 17 Wed | #39 | POA-033: .NET SDK setup | R2 | Sept 17 | Sept 17 | Blocks #40 |
| Sept 18 Thu | #3 | POA-003: Container Registry | R1 | Sept 18 | Sept 18 | Depends on #1 |
| Sept 18 Thu | #26 | POA-004: VNet setup | R1 | Sept 18 | Sept 18 | Depends on #1 |
| Sept 18 Thu | #11 | POA-013: Health checks | R2 | Sept 18 | Sept 18 | Depends on #10 |
| Sept 19 Fri | #4 | POA-005: Key Vault | R1 | Sept 19 | Sept 19 | Depends on #1 |
| Sept 19 Fri | #28 | POA-007: App Service | R1 | Sept 19 | Sept 19 | Depends on #1 |
| Sept 19 Fri | #33 | POA-014: Swagger/OpenAPI | R2 | Sept 19 | Sept 19 | Depends on #10 |

### Week 1: September 22-26 (5 days)

| Date | Issue # | Task | Resource | Start | End | Notes |
|------|---------|------|----------|-------|-----|-------|
| Sept 22 Mon | #29 | POA-008: Log Analytics | R1 | Sept 22 | Sept 22 | Depends on #1 |
| Sept 22 Mon | #32 | POA-011: Database migrations | R2 | Sept 22 | Sept 22 | Depends on #31 |
| Sept 23 Tue | #5 | POA-009a: Bicep main | R1 | Sept 23 | Sept 23 | Depends on #1 |
| Sept 23 Tue | #34 | POA-016: Flutter models | R2 | Sept 23 | Sept 23 | Depends on #12 |
| Sept 24 Wed | #6 | POA-009b: Bicep networking | R1 | Sept 24 | Sept 24 | Depends on #5 |
| Sept 24 Wed | #35 | POA-017: Flutter HTTP | R2 | Sept 24 | Sept 24 | Depends on #12 |
| Sept 25 Thu | #7 | POA-009c: Bicep database | R1 | Sept 25 | Sept 25 | Depends on #5 |
| Sept 25 Thu | #8 | POA-009d: Bicep containers | R1 | Sept 25 | Sept 25 | Depends on #5 |
| Sept 25 Thu | #40 | POA-034: .NET SDK client | R2 | Sept 25 | Sept 25 | Depends on #39 |
| Sept 26 Fri | #9 | POA-009e: Bicep deployment | R1 | Sept 26 | Sept 26 | Depends on #6,7,8 |
| Sept 26 Fri | #36 | POA-018: Docker containers | R2 | Sept 26 | Sept 26 | Depends on #10 |

### Week 2: Sept 29-30, Oct 1-3 (5 days)

| Date | Issue # | Task | Resource | Start | End | Notes |
|------|---------|------|----------|-------|-----|-------|
| Sept 29 Mon | #27 | POA-006: App Gateway WAF | R1 | Sept 29 | Sept 29 | Depends on #26 |
| Sept 29 Mon | #43 | POA-081: Test framework | R2 | Sept 29 | Sept 29 | |
| Sept 29 Mon | #58 | POA-081: Test framework (dup) | R2 | Sept 29 | Sept 29 | |
| Sept 30 Tue | #30 | POA-009: CI/CD pipelines | R1 | Sept 30 | Sept 30 | Depends on #3 |
| Sept 30 Tue | #44 | POA-082: Domain test structure | R2 | Sept 30 | Sept 30 | Depends on #10 |
| Oct 1 Wed | #37 | POA-019: Deploy to Container Apps | R1 | Oct 1 | Oct 1 | Depends on #28, #36 |
| Oct 1 Wed | #45 | POA-083: Integration test harness | R2 | Oct 1 | Oct 1 | |
| Oct 2 Thu | #38 | POA-020: Week 1 checkpoint | R1 | Oct 2 | Oct 2 | Milestone |
| Oct 2 Thu | #46 | POA-084: Test database | R2 | Oct 2 | Oct 2 | Depends on #2 |
| Oct 3 Fri | #13 | POA-021: OIDC authentication | R1 | Oct 3 | Oct 6 | 2 days - CRITICAL |
| Oct 3 Fri | #47 | POA-086: Flutter SDK tests | R2 | Oct 3 | Oct 3 | Depends on #12 |

### Week 3: October 6-10 (5 days)

| Date | Issue # | Task | Resource | Start | End | Notes |
|------|---------|------|----------|-------|-----|-------|
| Oct 6 Mon | #13 | POA-021: OIDC (continues) | R1 | - | Oct 6 | End of 2-day task |
| Oct 6 Mon | #48 | POA-087: Infrastructure tests | R2 | Oct 6 | Oct 6 | |
| Oct 7 Tue | #14 | POA-022: Mock WA IdX | R1 | Oct 7 | Oct 7 | Depends on #13 |
| Oct 7 Tue | #16 | POA-025: CQRS implementation | R2 | Oct 7 | Oct 8 | 2 days |
| Oct 8 Wed | #15 | POA-023: Tenant resolution | R1 | Oct 8 | Oct 8 | Depends on #13 |
| Oct 8 Wed | #16 | POA-025: CQRS (continues) | R2 | - | Oct 8 | End of 2-day task |
| Oct 9 Thu | #18 | POA-031: Flutter SDK auth | R1 | Oct 9 | Oct 9 | Depends on #13 |
| Oct 9 Thu | #17 | POA-026: Credential APIs | R2 | Oct 9 | Oct 10 | 2 days, depends on #16 |
| Oct 10 Fri | #55 | POA-094: Performance baseline | R1 | Oct 10 | Oct 10 | |
| Oct 10 Fri | #17 | POA-026: APIs (continues) | R2 | - | Oct 10 | End of 2-day task |

### Week 4: October 13-17 (5 days)

| Date | Issue # | Task | Resource | Start | End | Notes |
|------|---------|------|----------|-------|-----|-------|
| Oct 13 Mon | #24 | POA-041: Demo mobile app | R1 | Oct 13 | Oct 14 | 2 days |
| Oct 13 Mon | #49 | POA-088: Credential tests | R2 | Oct 13 | Oct 13 | |
| Oct 14 Tue | #24 | POA-041: Demo (continues) | R1 | - | Oct 14 | End of 2-day task |
| Oct 14 Tue | #50 | POA-089: Auth integration tests | R2 | Oct 14 | Oct 14 | |
| Oct 15 Wed | #57 | POA-097: Security testing | R1 | Oct 15 | Oct 16 | 2 days |
| Oct 15 Wed | #51 | POA-090: API endpoint tests | R2 | Oct 15 | Oct 15 | |
| Oct 16 Thu | #57 | POA-097: Security (continues) | R1 | - | Oct 16 | End of 2-day task |
| Oct 16 Thu | #59 | POA-097: Security testing (dup) | R1 | Oct 16 | Oct 16 | |
| Oct 16 Thu | #52 | POA-091: Multi-tenant tests | R2 | Oct 16 | Oct 16 | |
| Oct 17 Fri | #19 | POA-048: Demo presentation | R1 | Oct 17 | Oct 17 | |
| Oct 17 Fri | #41 | POA-035: TypeScript SDK | R2 | Oct 17 | Oct 17 | |
| Oct 17 Fri | #61 | POA-099: Test coverage | R2 | Oct 17 | Oct 17 | |

### Week 5: October 20-24 (5 days)

| Date | Issue # | Task | Resource | Start | End | Notes |
|------|---------|------|----------|-------|-----|-------|
| Oct 20 Mon | #25 | POA-061: DGov UAT support | Both | Oct 20 | Oct 21 | 2 days |
| Oct 21 Tue | #25 | POA-061: UAT (continues) | Both | - | Oct 21 | End of 2-day task |
| Oct 22 Wed | #53 | POA-092: Security validation | R1 | Oct 22 | Oct 22 | |
| Oct 22 Wed | #54 | POA-093: Cross-SDK tests | R2 | Oct 22 | Oct 22 | |
| Oct 23 Thu | #56 | POA-096: Load testing | R1 | Oct 23 | Oct 23 | |
| Oct 23 Thu | #60 | POA-098: Compliance tests | R2 | Oct 23 | Oct 23 | |
| Oct 24 Fri | #22 | POA-066: Performance testing | Both | Oct 24 | Oct 24 | |

### Week 6: October 27-31 (5 days)

| Date | Issue # | Task | Resource | Start | End | Notes |
|------|---------|------|----------|-------|-----|-------|
| Oct 27 Mon | #21 | POA-075: Handover package | R1 | Oct 27 | Oct 28 | 2 days |
| Oct 28 Tue | #21 | POA-075: Handover (continues) | R1 | - | Oct 28 | End of 2-day task |
| Oct 29 Wed | | Documentation updates | Both | Oct 29 | Oct 29 | |
| Oct 30 Thu | | Final fixes and prep | Both | Oct 30 | Oct 30 | |
| Oct 31 Fri | #23 | POA-077: Final presentation | Both | Oct 31 | Oct 31 | |

## Dependencies Summary

### Critical Path
1. POA-001 (#1) → All infrastructure tasks
2. POA-010 (#10) → POA-011 (#32), POA-013 (#11), POA-014 (#33), POA-018 (#36)
3. POA-021 (#13) → POA-022 (#14), POA-023 (#15), POA-031 (#18)
4. POA-025 (#16) → POA-026 (#17)

### Key Milestones
- Sept 15: Project start with POA-001
- Oct 1: Official POA start, deployment complete
- Oct 2: Week 1 checkpoint (#38)
- Oct 17: Demo presentation (#19)
- Oct 24: Performance testing complete (#22)
- Oct 31: Final presentation (#23)

## Resource Allocation Summary
- **R1 (Architect/Dev)**: 30 tasks - Infrastructure, Auth, Security, Architecture
- **R2 (Developer)**: 31 tasks - Backend, SDKs, Testing
- **Both**: 3 tasks - UAT support, Performance testing, Final presentation