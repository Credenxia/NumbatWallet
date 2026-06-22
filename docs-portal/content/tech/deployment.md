---
tags:
  - tech
  - support
---

# Deployment & CI

> **Audience:** Tech (and Support). Source: `docs/ARCHITECTURE-CURRENT.md §6`,
> `docs/OPERATIONS.md §2`, `README.md`, `perf/RESULTS-2026-06-12.md`.

## Topology (nonprod)

```
Internet → Azure Front Door (cxaus-test-fd-shared, +~120 ms)
        → shared NGINX ingress (AKS aks-shared-nonprod-aue, australiaeast)
        → namespace numbatwallet-test
             ├─ numbatwallet-api    (ASPNETCORE_ENVIRONMENT=Testing, 1 replica)
             ├─ numbatwallet-admin  (ASPNETCORE_ENVIRONMENT=Development, Blazor sticky sessions)
             └─ migrations Job      (EF migrations bundle, Helm pre-install/pre-upgrade hook)
        → shared PostgreSQL flexible server psql-shared-nonprod-aue (B1ms), DB numbatwallet_test
Secrets: kv-numbatwallet-test-aue → k8s secret numbatwallet-secrets (bootstrap-namespace.sh)
Cache:   in-memory (shared Redis pending infra-branch merge + NetworkPolicy)
```

Public URL: `https://tst.numbatwallet.credentry.com.au`. This **supersedes** the older
Container-Apps / Bicep direction.

## Helm chart (`deploy/helm/numbatwallet`)

Templates: `deployment-api`, `deployment-admin`, `migrate-job` (hook), `services`,
`ingress` (class `nginx`, host `tst.numbatwallet.credentry.com.au`, Blazor sticky
sessions). Key values (`values.yaml` comments are authoritative):

- `image.registry/tag` — `cxausvmcontainerregistry.azurecr.io`, tags `test-{shortSha}`.
- `secretName: numbatwallet-secrets` — created by `bootstrap-namespace.sh` from
  `kv-numbatwallet-test-aue`.
- `api.redisConnectionString` (empty until the shared-services NetworkPolicy admits the
  namespace), `api.rateLimiting.*`, `api.searchTokenPepper` (deploy-time only), `api.extraEnv`.
- Resources: deliberately small burstable footprint (API 25m/300m CPU, 256–512Mi).

## Migrations

The migrations image runs the EF Core **migrations bundle** (baked at image build) as a
Helm **pre-install/pre-upgrade hook Job** — a failed migration blocks the rollout.
Non-Development environments use real migrations only (`MigrateAsync`, fail-fast); the dev
`EnsureCreated` path is never used in containers. History starts at `InitialSchema`
(22 tables) + `AddPersonSearchTokenColumns` + `AddPresentationRequests`.

!!! note "Image build gotcha"
    Build with `az acr build --platform linux/amd64` from a clean **`git archive`**
    context — building from the working tree uploads ~1 GB and hangs.

## CI

`.github/workflows/deploy-numbatwallet.yml` (workflow dispatch + push-to-main on
deployable paths):

```
build → 5 unit suites (gate) → az acr build (api/admin/migrations) → helm upgrade → smoke
```

!!! warning "Pending GitHub setup"
    The workflow needs a repo secret **`AZURE_CREDENTIALS`** and a **`nonprod`**
    environment — **not yet configured** (a pending user step).

## Performance

After the 2026-06-13 fixes (see [Directors → Status](../directors/status-readiness.md)
and `perf/RESULTS-2026-06-12.md`): p95 < 500 ms on all measured paths (citizen journey
124.8 ms steady, 0 errors / 0 throttling at 100 VU on health). The remaining tail at 50+
VU on the citizen ramp is **capacity sizing** of the deliberate test footprint, not a
config defect.

Two SLA-blocking config defects were found and fixed:

- **A — Redis 5 s stall:** every Bearer request blocked the full `syncTimeout` on a token
  blacklist lookup against a non-existent `localhost:6379`. Fixed by the
  `CacheRegistrationPolicy` (Redis only with a non-empty connection string; bounded
  fail-open timeouts; `localhost` default removed; backend logged at startup).
- **B — global rate limiter:** 100/min keyed per remote IP behind NGINX collapsed all
  clients into one bucket. Fixed by `RateLimitPartitionKeyResolver` (first X-Forwarded-For
  hop) + explicit `UseForwardedHeaders` first in the pipeline + configurable limits.
