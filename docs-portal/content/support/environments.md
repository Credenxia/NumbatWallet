---
tags:
  - support
  - tech
---

# Environments & URLs

> **Audience:** Support (and Tech). Source: `docs/OPERATIONS.md`, `README.md`,
> `perf/RESULTS-2026-06-12.md`.

## Environment reference

| | Local (dev) | Nonprod (AKS) |
|---|---|---|
| Orchestration | .NET Aspire (Postgres container, dynamic ports) | Shared AKS `aks-shared-nonprod-aue`, namespace `numbatwallet-test` |
| `ASPNETCORE_ENVIRONMENT` (API) | `Development` | `Testing` |
| `ASPNETCORE_ENVIRONMENT` (Admin) | `Development` | `Development` |
| Public URL | dynamic localhost port (Aspire dashboard) | **<https://tst.numbatwallet.credentry.com.au>** |
| Seed tenant | `00000000-0000-0000-0000-000000000001` | `00000000-0000-0000-0000-000000000000` |
| Cache | in-memory | in-memory (shared Redis pending infra merge) |
| Database | Postgres container (Aspire) | shared `psql-shared-nonprod-aue` (PG17, B1ms), DB `numbatwallet_test` |
| Subscription | — | "Credenxia AU" (Key Vault `kv-numbatwallet-test-aue` is in "Credenxia AU LAB") |

## Public edge

```
Internet → Azure Front Door (cxaus-test-fd-shared, +~120 ms)
        → shared NGINX ingress (AKS, australiaeast)
        → namespace numbatwallet-test (api, admin, migrations Job)
        → shared PostgreSQL (numbatwallet_test)
```

The Front Door edge adds roughly **+120 ms** per request (TLS termination + backhaul). Fast
paths stay under the 500 ms SLA through the edge.

## Key URLs & endpoints

| What | Where |
|---|---|
| Public API/admin host | `https://tst.numbatwallet.credentry.com.au` |
| Ingress IP (direct, bypassing Front Door) | `20.92.192.89` (use `curl -k --resolve host:443:20.92.192.89`) |
| GraphQL endpoint | `/graphql` |
| Citizen/officer login | `POST /api/v1/Authentication/login` (**controller-name route**, not `/auth/login`) |
| API health | `/health` → 200 (admin `/health` returns 302→/login; probes accept <400) |

## Dev logins

`TestPasswordValidator` — **Development / Testing only**, password `Test123!@#`:

| Account | Roles |
|---|---|
| `admin@example.com` | Admin |
| `citizen@example.com` | Citizen, User |
| `john.doe@example.com` | (created on AKS through the API so its search tokens are valid) |

!!! warning "Seeded logins on AKS"
    On AKS, the **pre-existing** seeded persons lack search tokens, so logins like
    `citizen@example.com` fail there with "person not found". Use `john.doe@example.com`
    (`Test123!@#`) on AKS, or re-seed/backfill — see [Known Issues](known-issues.md).

## Dev service (SDK-style) auth

```
X-API-Key:   test-api-key-development-only
X-Tenant-Id: 00000000-0000-0000-0000-000000000001   # local dev seed tenant
```

On the AKS Testing environment use tenant `00000000-0000-0000-0000-000000000000` instead.
