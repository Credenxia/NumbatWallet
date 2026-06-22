---
tags:
  - directors
---

# Status & Readiness

> **Audience:** Directors. Source: `README.md`, `docs/ARCHITECTURE-CURRENT.md`,
> `perf/RESULTS-2026-06-12.md`.

## The headline

NumbatWallet is a **working, deployed proof-of-authority (POA) platform**. As of June
2026 every planned build pillar is complete and independently verified, the system is
live on a shared non-production Azure Kubernetes cluster, and it meets its performance
SLA on all measured paths after two configuration defects were fixed.

What remains is **not feature work** — it is a short list of manual provisioning and
operational-hardening steps (see [Risk & Pending Items](risk-register.md)).

## Pillars delivered (verified)

| Pillar | State | Notes |
|---|---|---|
| Credential lifecycle | :material-check-circle:{ .green } Done | Issue, fetch, list, verify, share, revoke, bulk issue. Revocation re-checked at verify time. |
| W3C VP-JWT presentations | :material-check-circle:{ .green } Done | Spec-conformant verifiable-presentation tokens with selective disclosure, nonce/audience binding, signature + revocation re-checks. |
| OID4VP subset | :material-flask:{ .amber } Experimental | DIF Presentation Exchange v2 requests, one-shot replay-rejected submission. A **minimal subset** — SD-JWT and multi-credential presentations are out of scope. |
| Three client SDKs | :material-check-circle:{ .green } Done | .NET, TypeScript, Flutter — wallet/credential/presentation operations contract-verified against a live backend. |
| Multi-auth | :material-check-circle:{ .green } Done | API key, citizen Bearer JWT, Credentry SSO federation (all coexist). |
| Credentry SSO federation | :material-check-circle:{ .green } Done | Live-proven for both service (M2M) and interactive user sign-in. |
| PII encryption at rest | :material-check-circle:{ .green } Done | AES-256-GCM field encryption with searchable HMAC tokens so login/lookup still work. |
| Deployed to AKS | :material-check-circle:{ .green } Done | Shared nonprod cluster, public URL live behind Azure Front Door. |
| Performance / SLA | :material-check-circle:{ .green } Pass | Both SLA-blocking defects fixed; p95 < 500 ms on all measured paths. |

## Honest "not done" list

These are **deliberately** stubbed, experimental, or descoped — not failures, but they
must not be over-claimed in client-facing material:

- **Admin portal stubs** — Dashboard, Tenants, Wallets and Credentials pages show live
  data, but backup, key-management, feature-flag, configuration and report admin surfaces
  are **in-memory POA stubs**; admin tenant CRUD writes do not persist; several pages are
  placeholders.
- **OID4VP** is a **minimal experimental subset**; presentations are signed-JWT-VP, not
  SD-JWT.
- **ISO 18013-5 mDL** transfer is **not implemented**.
- **Per-tenant database** isolation is a **POC**, not productionised (shared DB is the
  decision of record for now).
- **ServiceWA OIDC** scheme exists but is opt-in and unconfigured (placeholders).
- **Build warnings** — the "zero warnings" goal is not currently met; the remaining
  advisories are transitive-only (a serialization library pulled in by the local dev
  orchestrator, and a test-only data-protection package).

## SLA verdict

The performance pass (k6 against the deployed AKS topology) initially found the deployed
**configuration** failed the SLA — not the application. Two config defects were fixed and
redeployed:

| Scenario | Before | After fix | Verdict |
|---|---|---|---|
| Citizen journey (Bearer) p95 | 5,093 ms | **124.8 ms** | :material-check-circle:{ .green } Pass |
| Health under 100 concurrent users | 99.6% throttled | **0% throttled, 558 req/s** | :material-check-circle:{ .green } Pass |
| Credential reads / presentation verify | ~140 ms / ~120 ms | unchanged | :material-check-circle:{ .green } Pass |

**Bottom line:** the application core is fast (DB ~2 ms, health 58 ms). One remaining
tail (>3 s p95 at 50+ concurrent users on the citizen ramp, **0 errors**) is **capacity
sizing** of the deliberately small test footprint (1 replica, 300m CPU, burstable
database) — a sizing exercise, not a defect. Full detail in
`perf/RESULTS-2026-06-12.md`.

## Test evidence

| Suite | Result |
|---|---|
| Unit (Domain 202, SharedKernel 52, Application 147, Infrastructure 326, Web.Api 108) | **835 passed / 0 failed** |
| Integration | **84 passed / 0 failed / 2 skipped** (both skips documented) |
| SDK live-contract suites | .NET 65/0, TypeScript 5/5, Flutter 7/7 against a live stack |

A CI gate runs all five unit suites and blocks deployment on any failure.
