---
tags:
  - support
---

# Known Issues

> **Audience:** Support. Source: `README.md` "Known gaps", `docs/OPERATIONS.md §4`,
> `perf/RESULTS-2026-06-12.md §8`, `docs/ARCHITECTURE-CURRENT.md §8`.

## 1. AKS seeded-login backfill (RESOLVED 2026-06)

**Was:** logging in on AKS with a pre-existing seeded account (e.g. `citizen@example.com`)
failed with **"person not found"** — the searchable-PII change made login resolve persons
via an `email_search_token`, and persons seeded **before** that change had no token.

**Resolved:** an in-namespace Job truncated `Persons` (CASCADE) in the disposable
`numbatwallet_test` DB and the API was restarted, so the seeder repopulated persons with
tokens under the stable pepper. `citizen@example.com` / `Test123!@#` now logs in and has a
wallet on AKS. To repeat for a fresh seed, re-run that Job + `kubectl rollout restart`.

## 2. Search-token pepper not yet in Key Vault (open)

The HMAC pepper is currently a deploy-time Helm value (`api.searchTokenPepper`). If the
pepper is missing the app falls back to the in-memory mock Key Vault — a **random pepper
per pod start** — so every stored token stops matching after a restart and login breaks.

**Fix:** move it into Key Vault secret `search-token-pepper` and extend
`bootstrap-namespace.sh`. Treat the pepper as **immutable per deployment**.

## 3. Field-encryption key changes break PII reads

Changing or losing `FieldEncryption:Key` makes existing `FE1:` ciphertext unreadable;
typed converters (e.g. date parsing) then throw and you see 500s. There is no
re-encryption job yet. In dev, reset the postgres volume; in production this needs a
decrypt-old/encrypt-new migration.

## 4. Capacity sizing tail (low)

On the deliberate test footprint (1 replica, 300m CPU, burstable B1ms PostgreSQL) the
citizen-journey ramp shows a **>3 s p95 tail at 50+ concurrent users with 0 errors**.
This is **capacity**, not a config defect — the app core is fast (DB ~2 ms). A sizing pass
(replicas / CPU / DB tier) is the planned follow-up.

## 5. Admin portal stubs (by design)

| Surface | State |
|---|---|
| Dashboard, Tenants, Wallets, Credentials, audit logs, metrics, system health | **Live data** |
| Tenant CRUD writes | Cache-only — **do not persist** |
| Backup, key management, feature flags, configuration, reports, key rotation, clear-cache, batch wallet create | **In-memory POA stubs** (`InMemoryAdminServices.cs`) |
| Master/tenant dashboards, create/issue/revoke modals, placeholder shells (ApiKeys, Webhooks, …) | **Mock** (marked `POA: mock data` in code) |
| User management | **Deleted** — managed in Credentry; `/users` links the Credentry portal |

These six stub areas are intentional POA scope. They are flagged in code and must not be
represented as production governance controls.

## 6. Admin portal runs in Development on AKS

The admin container runs `ASPNETCORE_ENVIRONMENT=Development` because its cookie-login UI
only exists there — which includes a `/auth/test-login` backdoor. This is an **accepted
test-stage risk**. Moving off Development requires an Entra app registration.

## 7. Health probe quirk

Admin `/health` returns **302 → /login** (auth fallback); probes accept it because the
status is `<400`. The API `/health` is a plain 200.

## 8. Other architectural debt (low)

- `Wallet.PersonId1` shadow FK; mixed table naming (PascalCase vs snake_case).
- Dev schema management uses `EnsureCreated`/`CreateTables` and **won't ALTER** an existing
  dev DB — reset the volume for new tables (real migrations are non-dev only).
- Key Vault config keys are inconsistent across components (`KeyVault:Uri` vs `:Url` vs
  `Azure:KeyVault:VaultUri`).
- No issuer-list API (introspection disabled; `organizations`/`createOrganization` broken
  on AKS) — affects perf write-mix and any issuer-picker UI.
