# Appendix H – Project Plan, Team & Effort Estimates

[← Back to Master PRD](PRD_Master.md#team--effort-estimates) | [← View Pricing](Appendix_I_Pricing_and_Assumptions.md#team-composition--ai-augmentation)

## Phase overview & timeline

The project comprises **three phases**: Proof‑of‑Operation (PoO), Pilot and Production/Expansion.  The timeline below assumes contract award in **Q1 2026** (aligned with the addendum) and uses weeks as the unit of measure.  Actual dates will be confirmed with DGov.

| Phase | Duration | Key activities | Exit criteria |
| --- | --- | --- | --- |
| **Phase 0 – Proof‑of‑Operation** | 5 weeks | Deliver SDK and documentation; set up demonstration environment; execute issuance, presentation, revocation and selective disclosure scenarios; provide security and interoperability evidence. | Successful demonstration and acceptance by DGov; sign‑off on capability and integration. |
| **Phase 1 – Pilot (Stages 1‑4)** | 12 months | Implementation & integration; restricted pilot (gov users); preview pilot (controlled citizens); evaluation & iteration. | Functional completeness; satisfaction of KPIs (user adoption, performance, security); readiness for production. |
| **Phase 2 – Production & Expansion** | 2–6 years (extensions) | Scale to additional credentials and agencies; optimise multi‑tenancy; add optional modules (biometrics, offline features); continuous improvement. | Achieve or exceed SLA targets; maintain certifications; positive stakeholder feedback. |

A simplified Gantt chart is presented below.

| Week | Activity | Phase |
| --- | --- | --- |
| **0–1** | Project kick‑off, confirm requirements, set up contract & governance | PoO |
| **1–3** | Develop and deliver SDKs, integrate with test environment | PoO |
| **3–5** | Demonstrate PoO scenarios; DGov evaluation | PoO |
| **6–14** | Production environment setup, integration with DTP/IdX | Pilot Stage 1 |
| **15–26** | Restricted pilot: onboard government users, gather feedback | Pilot Stage 2 |
| **27–40** | Preview pilot: controlled citizen rollout, monitor performance | Pilot Stage 3 |
| **41–52** | Evaluate pilot results, refine solution, prepare production plan | Pilot Stage 4 |
| **Year 2+** | Scale to new credentials/agencies; R&D for optional modules | Production |

## Team composition & effort estimates

### AI-Augmented Team Structure

Our team leverages AI tools (GitHub Copilot, Claude, GPT-4) to achieve 40-50% productivity gains, allowing a smaller team with higher output. The table below shows our **lean, AI-augmented team** structure:

| Role | PoO FTE | Pilot FTE | Production FTE | Base Salary | Key Responsibilities |
| --- | --- | --- | --- | --- | --- |
| **Project Manager** | 0.5 | 1.0 | 0.0 | $180,000 | Stakeholder management, reporting, risk mitigation, coordination |
| **Account Manager** | 0.0 | 0.0 | 1.0 | $110,000 | Client relationships, PM duties, stakeholder coordination |
| **Business Analyst** | 0.5 | 1.0 | 0.2 | $100,000 | Requirements gathering, documentation, process mapping |
| **Tester/QA** | 0.5 | 1.0 | 0.1 | $100,000 | Test planning, UAT coordination, quality assurance |
| **Architect/Developer** | 1.0 | 1.0 | 0.5 | $180,000 | 50% architecture, 50% development, L3 support |
| **Full-Stack Developer** | 1.0 | 1.0 | 0.5 | $160,000 | Backend + mobile SDK, highly productive with AI assistance |
| **DevOps/Security Engineer** | 0.5 | 1.0 | 0.5 | $180,000 | Infrastructure, security, can assist with development |
| **Technical Support** | 0.0 | 0.5 | 1.0 | $100,000 | L1/L2 support, documentation, knowledge base |
| **Total Team Size** | **4.0** | **6.5** | **3.6** | **-** | **15% contingency added for super, leave, replacements** |

### Traditional vs AI-Augmented Comparison

| Metric | Traditional Team | AI-Augmented (Pilot) | AI-Augmented (Prod) |
| --- | --- | --- | --- |
| Team Size (FTE) | 10-12 | 6.5 | 3.6 |
| Annual Cost (base) | $1,500,000 | $950,000 | $500,000 |
| Annual Cost (with contingency) | $1,725,000 | $1,092,500 | $575,000 |
| Lines of Code/Day | 100-150 | 180-250 | 150-200 |
| Documentation Time | 30% of dev time | 12% of dev time | 10% of dev time |
| Bug Rate | 15-20 per KLOC | 8-10 per KLOC | 5-8 per KLOC |
| Time to Market | 6 months | 3.5 months | 2 months |

### Key Team Advantages

1. **Optimized Structure**: 6.5 FTE pilot (separate BA and Tester), 3.6 FTE production through maximum efficiency
2. **AI Productivity**: 40-50% productivity boost through AI pair programming and code generation
3. **Cross-functional Capability**: DevOps/Security engineer can assist with development when needed
4. **Dedicated BA and Tester**: Separate roles ensure thorough requirements and quality (1.0 + 1.0 in pilot)
5. **Account Manager Evolution**: Replaces PM in production, maintains relationships while reducing costs
6. **15% Contingency Buffer**: Covers superannuation, annual leave, and potential replacements

## RACI matrix

The RACI (Responsible, Accountable, Consulted, Informed) matrix below maps key activities to stakeholders, including the critical Business Analyst role.

| Activity | DGov | ServiceWA Dev | Wallet Provider | BA/Tester | Account Mgr | Issuer Agencies | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| **Requirements & governance** | A | C | R | R | I | C | BA/Tester leads requirements with DGov |
| **SDK development** | I | C | A | C | I | I | Our team develops; BA documents |
| **Integration testing** | C | R | R | A | I | I | BA/Tester coordinates all testing |
| **Training & documentation** | C | I | R | A | C | C | BA/Tester develops materials |
| **User acceptance testing** | A | R | C | R | I | C | BA/Tester manages test scenarios |
| **PKI/security configuration** | A | I | R | I | I | I | DevOps/Security leads |
| **Credential issuance setup** | C | I | R | C | A | R | Account Mgr facilitates in production |
| **Production rollout** | A | C | R | C | R | C | Account Mgr coordinates |
| **Support & maintenance** | I | I | A | I | C | I | Tech support with Account Mgr |
| **Incident management** | C | C | A | I | R | I | Account Mgr escalates critical issues |

### Key for RACI:
- **R** (Responsible): Does the work
- **A** (Accountable): Ultimately answerable
- **C** (Consulted): Provides input
- **I** (Informed): Kept updated

## Risk register

The following table identifies key project risks, their mitigation strategies, and ownership. The Business Analyst plays a crucial role in risk identification and mitigation planning.

| Risk | Likelihood | Impact | Mitigation | Owner |
| --- | --- | --- | --- | --- |
| **Integration delays with ServiceWA** | Medium | High | Early SDK delivery; regular integration workshops; BA/Tester facilitates | BA/Tester |
| **Requirements ambiguity** | Medium | Medium | BA/Tester conducts detailed sessions; prototypes for validation | BA/Tester |
| **PKI/certificate complexity** | Low | High | DevOps/Security expert handles; thorough testing | DevOps/Sec |
| **User adoption resistance** | Medium | Medium | BA/Tester develops training; Account Mgr manages relationships | BA/Account Mgr |
| **AI tool unavailability** | Low | Medium | Maintain fallback to traditional development; local AI models | Architect |
| **Resource availability** | Low | Medium | 15% contingency buffer; cross-training; documentation | PM/Account Mgr |
| **Compliance certification delays** | Medium | High | Start certification early; 4 penetration tests/year required | DevOps/Sec |
| **Performance issues at scale** | Low | High | Load testing from pilot start; auto-scaling design | DevOps/Sec |

## Success metrics

### Phase-specific KPIs

| Metric | PoO Target | Pilot Target | Production Target |
| --- | --- | --- | --- |
| **Functional completeness** | 80% | 95% | 99% |
| **System availability** | 95% | 99.5% | 99.95% |
| **API response time** | < 500ms | < 200ms | < 100ms |
| **User satisfaction** | N/A | > 70% | > 85% |
| **Support ticket resolution** | N/A | < 24 hours | < 4 hours |
| **Security incidents** | 0 | < 2 minor | 0 critical |
| **Documentation completeness** | 70% | 90% | 100% |
| **Test coverage** | 60% | 80% | 90% |

### Team productivity metrics (AI-augmented)

| Metric | Traditional | AI-Augmented | Improvement |
| --- | --- | --- | --- |
| **Features per sprint** | 3-4 | 5-7 | 75% |
| **Code review time** | 4 hours | 2 hours | 50% |
| **Bug fix time** | 8 hours | 4 hours | 50% |
| **Documentation per feature** | 6 hours | 2 hours | 67% |
| **Test creation** | 40% of dev time | 15% of dev time | 63% |
| **Team cost efficiency** | $125K/FTE | $178K/FTE (pilot) | Better output/$ |

## Communication plan

### Stakeholder engagement model

| Stakeholder | Frequency | Format | Lead |
| --- | --- | --- | --- |
| DGov Leadership | Monthly | Status report + meeting | PM (pilot) / Account Mgr (prod) |
| ServiceWA Team | Weekly | Sprint reviews | BA/Tester & Architect |
| Technical Working Group | Bi-weekly | Technical deep-dives | Architect |
| User Representatives | Monthly | Feedback sessions | BA/Tester |
| Security Board | Quarterly | Compliance reviews | DevOps/Security |
| Support Team | Daily | Stand-ups | Tech Support & Account Mgr |

### Deliverable schedule

| Deliverable | PoO | Pilot Month 3 | Pilot Month 6 | Pilot Month 12 |
| --- | --- | --- | --- | --- |
| SDK Releases | v0.1 | v0.5 | v0.8 | v1.0 |
| API Documentation | Draft | v1.0 | v1.5 | v2.0 |
| Training Materials | Outline | Basic | Intermediate | Complete |
| Security Assessments | Initial | Penetration Test 1 (required) | Mid-pilot review | Penetration Test 2 (required) |
| User Documentation | N/A | Draft | Review | Final |
| Operations Runbooks | N/A | Basic | Enhanced | Production-ready |

---

[← Back to Master PRD](PRD_Master.md#team--effort-estimates) | [View Operations Details →](Appendix_G_Operations_SRE_DR.md) | [View Testing Plan →](Appendix_F_Testing_QA_POA_Pilot.md)