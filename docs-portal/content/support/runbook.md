---
tags:
  - support
---

# Runbook

> **Audience:** Support / operators. Source: `docs/OPERATIONS.md` (authoritative — see it
> for the full environment-variable contract), `README.md`.

## Run locally

Prerequisites: .NET SDK 10.0.1xx (`global.json`, `rollForward: latestFeature`), Docker
Desktop running.

```bash
# Aspire DCP needs the docker CLI on PATH (not guaranteed in every shell):
env PATH="/Applications/Docker.app/Contents/Resources/bin:$PATH" \
  ASPIRE_ALLOW_UNSECURED_TRANSPORT=true \
  dotnet run --project src/NumbatWallet.AppHost --launch-profile http
```

Aspire assigns dynamic ports (find the Web.Api/Web.Admin URLs in the Aspire dashboard).
On first run the dev database is schema-created and seeded automatically
(`SKIP_DB_MIGRATION=true` disables seeding). Dev caching is in-memory; the active backend
is logged at startup.

```bash
# Citizen login (note the controller-name route):
curl -X POST http://127.0.0.1:<port>/api/v1/Authentication/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"citizen@example.com","password":"Test123!@#"}'
```

!!! warning "Resetting dev crypto config"
    If you change `FieldEncryption:Key` or `Search:TokenPepper` in dev, **reset the
    postgres data volume** (`numbatwallet.apphost-*-postgres-data`) — existing ciphertext
    and search tokens become unreadable otherwise.

## Environment-variable contract (summary)

The full table is in `docs/OPERATIONS.md §1`. The load-bearing ones:

| Variable | Meaning |
|---|---|
| `ConnectionStrings__numbatwallet` | PostgreSQL connection string (from `numbatwallet-secrets`). |
| `ConnectionStrings__Redis` | **Optional.** Empty/unset ⇒ in-memory cache. Non-empty ⇒ Redis (bounded fail-open timeouts). No `localhost:6379` default any more. |
| `Jwt__SecretKey` | HS256 secret (app throws if unset when HMAC signer active). |
| `Jwt__Signer` / `Jwt__SigningKeyVaultUri` | `KeyVaultRsa` + URI enables RS256. **Mandatory outside Development/Testing.** |
| `FieldEncryption__Key` / `__Source=KeyVault` + `__KeyVaultUri` | AES-256-GCM field key. Changing/losing it makes existing ciphertext unreadable. |
| `Search__TokenPepper` | HMAC pepper for email/phone search tokens. **Required** — without it the app falls back to a random per-pod pepper and login breaks after a restart. |
| `RateLimiting__PermitLimit` / `__Window` / `__QueueLimit` | Global limiter. Defaults 100/min/queue 10; the test namespace runs 100000/min. **Production must not copy the test values.** |
| `Credentry__Enabled`, `Credentry__TenantMap__{tid}` | SSO federation (fail-closed tenant map). |

## Rotation implications

| Secret | Rotation consequence |
|---|---|
| `Jwt:SecretKey` / RS256 key | Outstanding tokens become invalid (users re-login). Otherwise safe. |
| `FieldEncryption:Key` | ⚠️ Existing ciphertext becomes **unreadable** — no re-encryption job yet; needs a decrypt-old/encrypt-new migration. In dev, reset the volume. |
| `Search:TokenPepper` | ⚠️ All stored search tokens stop matching ⇒ **email/phone lookup and login break** until rows are re-tokenised. Treat as immutable per deployment. |
| API keys | Update SDK/portal consumers in lockstep. |

## Open operator follow-ups

From `docs/OPERATIONS.md §4` / `perf/RESULTS-2026-06-12.md §8`:

1. **Seeded-login backfill on AKS** — re-seed the disposable `numbatwallet_test` data or
   run an authorized backfill so pre-existing persons get search tokens. A valid account
   exists: `john.doe@example.com` / `Test123!@#`.
2. **Pepper provisioning** — move `Search__TokenPepper` into Key Vault secret
   `search-token-pepper` and extend `bootstrap-namespace.sh` to project it.
3. **Rate limiter** — production must use strict defaults (omit `api.rateLimiting`).
4. **Shared Redis** — merge the infra onboarding branch (namespace + network policies),
   then set `api.redisConnectionString`. Until then in-memory cache is used.
5. **`Jwt:Signer`** — set `KeyVaultRsa` + vault URI before any environment runs as
   Production.
6. **Admin portal auth** — Entra app registration + `AzureAd__*`, switch admin off
   `Development` (removes the `/auth/test-login` backdoor).
7. **Capacity sizing** — schedule a sizing pass (replicas / CPU / DB tier).

## Useful commands

```bash
# Cluster access
export PATH="/Applications/Docker.app/Contents/Resources/bin:$PATH"
az aks get-credentials -g rg-shared-nonprod-aue -n aks-shared-nonprod-aue
kubectl get pods -n numbatwallet-test
kubectl top pods -n numbatwallet-test

# Admin API key
kubectl get secret numbatwallet-secrets -n numbatwallet-test \
  -o jsonpath='{.data.admin-api-key}' | base64 -d

# Smoke (public edge) / direct ingress
curl https://tst.numbatwallet.credentry.com.au/health
curl -k --resolve tst.numbatwallet.credentry.com.au:443:20.92.192.89 \
  https://tst.numbatwallet.credentry.com.au/health

# Confirm active cache backend + limiter (logged at startup)
kubectl logs deploy/numbatwallet-api -n numbatwallet-test \
  | grep -E "Distributed cache backend|Global rate limiter"
```
