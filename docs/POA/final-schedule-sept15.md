# POA Final Schedule - September 15 Start

**Start Date:** September 15, 2025 (Monday)  
**End Date:** October 31, 2025 (Friday)  
**Duration:** 33 working days (no weekends)  
**Resources:** 2 people (R1: Architect/Dev, R2: Developer)  
**Total Capacity:** 66 person-days

## Critical Points
✅ POA-001 on Day 1 (Sept 15) as requested  
✅ No weekend work - all tasks Mon-Fri only  
✅ No date overlaps (inclusive dates considered)  
✅ Dependencies properly sequenced  
✅ Resources clearly allocated  

## Manual Updates Required in GitHub Project

### Priority 1: Fix Weekend Conflicts
These issues currently have weekend dates that need fixing:

| Issue | Current | Change To | Description |
|-------|---------|-----------|-------------|
| #18 | Oct 11 (Sat) | Oct 10 (Fri) | Flutter SDK auth |
| #19 | Oct 18 (Sat) | Oct 17 (Fri) | Demo presentation |
| #22 | Oct 25 (Sat) | Oct 24 (Fri) | Performance testing |
| #23 | Nov 1 (Sat) | Oct 31 (Fri) | Final presentation |
| #27 | Oct 4 (Sat) | Sept 26 (Fri) | App Gateway |
| #30 | Oct 4 (Sat) | Sept 29 (Mon) | CI/CD |
| #37 | Oct 4 (Sat) | Sept 30 (Tue) | Deploy |
| #38 | Oct 4 (Sat) | Oct 1 (Wed) | Checkpoint |
| #41 | Oct 18 (Sat) | Oct 17 (Fri) | TypeScript SDK |
| #55 | Oct 11 (Sat) | Oct 23 (Thu) | Perf baseline |
| #61 | Oct 18 (Sat) | Oct 29 (Wed) | Coverage |

### Priority 2: Update All Dates
Use the schedule below to update Start Date and Target Date fields in the project.

## Complete Schedule by Week

### Week 0: September 15-19 (Foundation)
| Day | R1 (Architect/Dev) | R2 (Developer) |
|-----|-------------------|----------------|
| Sept 15 Mon | #1 POA-001 Azure subscription | #10 POA-012 Backend structure |
| Sept 16 Tue | #2 POA-002 PostgreSQL | #31 POA-010 Database schema |
| Sept 17 Wed | #3 POA-003 ACR, #26 POA-004 VNet | #12 POA-015 Flutter SDK init |
| Sept 18 Thu | #4 POA-005 Key Vault | #39 POA-033 .NET SDK setup |
| Sept 19 Fri | #28 POA-007 App Service, #29 POA-008 Logs | #11 POA-013 Health checks |

### Week 1: September 22-26 (Infrastructure)
| Day | R1 (Architect/Dev) | R2 (Developer) |
|-----|-------------------|----------------|
| Sept 22 Mon | #5 POA-009a Bicep main | #33 POA-014 Swagger |
| Sept 23 Tue | #6 POA-009b Network, #7 POA-009c DB | #34 POA-016 Flutter models |
| Sept 24 Wed | #8 POA-009d Containers | #35 POA-017 Flutter HTTP |
| Sept 25 Thu | #9 POA-009e Deploy script | #32 POA-011 Migrations |
| Sept 26 Fri | #27 POA-006 App Gateway | #36 POA-018 Docker |

### Week 2: Sept 29-30, Oct 1-3 (Integration)
| Day | R1 (Architect/Dev) | R2 (Developer) |
|-----|-------------------|----------------|
| Sept 29 Mon | #30 POA-009 CI/CD | #40 POA-034 .NET client |
| Sept 30 Tue | #37 POA-019 Deploy to Azure | #43,58 POA-081 Test framework |
| Oct 1 Wed | #38 POA-020 Checkpoint | #44 POA-082 Domain tests |
| Oct 2 Thu | ServiceWA workshop | #45 POA-083 Integration harness |
| Oct 3 Fri | #13 POA-021 OIDC auth (start 4-day) | #46 POA-084 Test database |

### Week 3: October 6-10 (Core Development)
| Day | R1 (Architect/Dev) | R2 (Developer) |
|-----|-------------------|----------------|
| Oct 6 Mon | #13 POA-021 OIDC (end) | #47 POA-086 Flutter tests |
| Oct 7 Tue | #14 POA-022 Mock IdX | #16 POA-025 CQRS (start 2-day) |
| Oct 8 Wed | #15 POA-023 Tenant | #16 POA-025 CQRS (end) |
| Oct 9 Thu | Security review | #17 POA-026 APIs (start 2-day) |
| Oct 10 Fri | #18 POA-031 Flutter auth | #17 POA-026 APIs (end) |

### Week 4: October 13-17 (Demo Preparation)
| Day | R1 (Architect/Dev) | R2 (Developer) |
|-----|-------------------|----------------|
| Oct 13 Mon | #24 POA-041 Demo app (start 2-day) | #48 POA-087 Infra tests |
| Oct 14 Tue | #24 POA-041 Demo app (end) | #49 POA-088 Credential tests |
| Oct 15 Wed | #57,59 POA-097 Security (start 2-day) | #50 POA-089 Auth tests |
| Oct 16 Thu | #57,59 POA-097 Security (end) | #51 POA-090 API tests |
| Oct 17 Fri | #19 POA-048 Demo prep | #41 POA-035 TypeScript SDK |

### Week 5: October 20-24 (Testing)
| Day | R1 & R2 Together | Individual |
|-----|------------------|------------|
| Oct 20-21 | #25 POA-061 UAT support (both) | - |
| Oct 22 Wed | - | R1: #52 Multi-tenant, R2: #53 Security |
| Oct 23 Thu | - | R1: #54 Cross-SDK, R2: #55 Perf baseline |
| Oct 24 Fri | #22 POA-066 Performance (both) | - |

### Week 6: October 27-31 (Evaluation)
| Day | R1 (Architect/Dev) | R2 (Developer) |
|-----|-------------------|----------------|
| Oct 27 Mon | #21 POA-075 Handover (start 2-day) | #56 POA-096 Load testing |
| Oct 28 Tue | #21 POA-075 Handover (end) | Documentation |
| Oct 29 Wed | #60 POA-098 Compliance | #61 POA-099 Coverage |
| Oct 30 Thu | Final fixes | Final fixes |
| Oct 31 Fri | #23 POA-077 Final presentation (both) | - |

## Critical Dependencies
1. **POA-001 (#1)** must complete Sept 15 - blocks all infrastructure
2. **POA-021 (#13)** Oct 3-6 - blocks POA-031 Flutter auth
3. **POA-025 (#16)** Oct 7-8 - blocks POA-026 APIs
4. **POA-041 (#24)** Oct 13-14 - needed for demo

## Resource Summary
- **R1 (Architect/Dev):** 33 days - Infrastructure, Auth, Security, Architecture
- **R2 (Developer):** 33 days - Backend, SDKs, Testing, Implementation
- **Both:** Key milestones, UAT support, presentations

## Success Factors
1. Aggressive AI adoption (50% efficiency required)
2. No scope creep - defer P2 tasks if needed
3. Daily coordination between R1 and R2
4. Immediate escalation of blockers
5. Continuous testing from Day 1

---
**Next Action:** Manually update all dates in GitHub Project view using this schedule.