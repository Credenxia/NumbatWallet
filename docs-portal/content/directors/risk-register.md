---
tags:
  - directors
  - support
---

# Risk & Pending Items

> **Audience:** Directors (and Support — the operator steps live here too). Source:
> `README.md` "Known gaps", `docs/OPERATIONS.md` §4, `perf/RESULTS-2026-06-12.md` §8.

## Risk register (honest)

| # | Item | Severity | Status / mitigation |
|---|---|---|---|
| R1 | **AKS seeded-login backfill.** The searchable-PII change means pre-existing seeded persons on AKS lack search tokens, so seeded logins (e.g. `citizen@example.com`) fail there ("person not found"). | Medium | A working test account (`john.doe@example.com`) was created through the API. Proper fix: re-seed or backfill the **disposable** test data. Production unaffected (no legacy rows). |
| R2 | **Search-token pepper provisioning.** The HMAC pepper is currently a deploy-time Helm value. Without a stable pepper, a pod restart can break login. | Medium | Move to Key Vault secret `search-token-pepper`. Treat the pepper as **immutable per deployment** — rotating it breaks all logins until rows are re-tokenised. |
| R3 | **`Jwt:Signer` for production.** RS256 signing is mandatory outside dev/test; the nonprod namespace runs as `Testing` and has not yet set it. | Medium | Set `Jwt:Signer=KeyVaultRsa` + vault URI **before** any environment moves to Production (startup fail-fasts otherwise). |
| R4 | **Admin portal stubs.** Backup, key management, feature flags, configuration and reports are in-memory POA stubs; tenant CRUD writes don't persist. | Low (scope) | Documented and flagged in code. Not represented as production governance controls. |
| R5 | **Capacity sizing.** The deliberate test footprint (1 replica / 300m CPU / burstable DB) shows a >3 s p95 tail at 50+ concurrent users — **0 errors**. | Low | A sizing pass (replicas / CPU / DB tier) is a planned exercise; the app core is fast. |
| R6 | **Build warnings.** "Zero warnings" goal not met. | Low | Remaining advisories are **transitive-only** (a serialization dependency of the local dev orchestrator; a test-only data-protection package). |
| R7 | **ServiceWA OIDC** unconfigured. | Low | Opt-in scheme; fails fast if enabled without real config. Citizens use the self-issued Bearer login until ServiceWA config exists. |
| R8 | **Admin portal auth on AKS.** Admin runs in `Development` mode (cookie login only exists there) and exposes a `/auth/test-login` backdoor. | Medium (test-stage) | Accepted **test-stage** risk. Requires an Entra app registration before going to a real environment. |

## Pending manual / operator steps (not yet done)

These are owned by the operator or the client, not the build:

1. **GitHub CI secrets** — add the `AZURE_CREDENTIALS` repository secret and a `nonprod`
   environment so the deploy workflow can run.
2. **Shared Redis** — merge the `credentry-infrastructure` onboarding branch (namespace +
   network policies), then point the API at shared Redis. Until then the API uses an
   in-memory cache (fine for a single replica).
3. **AKS pepper / reseed** — see R1/R2: move the pepper into Key Vault and backfill or
   re-seed the disposable test data.
4. **`Jwt:Signer=KeyVaultRsa`** on any non-test environment (R3).
5. **Admin Entra app registration** (R8) to move the admin portal off `Development`.
6. **Front Door / DNS** — finalise the public route for the host (the edge is live but
   adds ~120 ms per request).
7. **Credentry nonprod client provisioning** — provision the `numbatwallet-admin` and
   `numbatwallet-m2m` OIDC clients and hand over secrets for the nonprod IdP.

The full operator detail for items 1–7 is in the
[Support runbook](../support/runbook.md).
