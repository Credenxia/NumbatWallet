---
tags:
  - account-management
  - directors
---

# Capabilities Matrix

> **Audience:** Account Management (and Directors). Each row is traceable to
> `README.md` / `docs/ARCHITECTURE-CURRENT.md`. **Legend:**
> :material-check-circle:{ .green } **Live** ·
> :material-flask:{ .amber } **Experimental** ·
> :material-progress-wrench:{ .grey } **Stub / POA** ·
> :material-calendar-clock:{ .grey } **Roadmap**

## Credentials

| Capability | Status | Notes |
|---|---|---|
| Issue credential | :material-check-circle:{ .green } Live | REST + GraphQL. |
| Fetch / list credentials | :material-check-circle:{ .green } Live | List-by-wallet (Relay connection). |
| Verify credential | :material-check-circle:{ .green } Live | Re-checks revocation. |
| Revoke credential | :material-check-circle:{ .green } Live | Revocation surfaces at verify time. |
| Share credential | :material-check-circle:{ .green } Live | Returns share URL/code; dev uses a logging email no-op. |
| Bulk issue | :material-check-circle:{ .green } Live | Returns issued IDs + per-item errors. |

## Presentations

| Capability | Status | Notes |
|---|---|---|
| W3C VP-JWT presentation | :material-check-circle:{ .green } Live | Selective disclosure, nonce/audience binding, signature + revocation re-checks. |
| Selective disclosure | :material-check-circle:{ .green } Live | Full-redisclosure model — **not SD-JWT**. |
| OID4VP presentation requests | :material-flask:{ .amber } Experimental | DIF PE v2; one-shot, replay-rejected submission. **Minimal subset.** |
| SD-JWT | :material-calendar-clock:{ .grey } Roadmap | Descoped. |
| Multi-credential VPs | :material-calendar-clock:{ .grey } Roadmap | Descoped. |
| ISO 18013-5 mDL transfer | :material-calendar-clock:{ .grey } Roadmap | Not implemented. |

## Identity, wallets & tenancy

| Capability | Status | Notes |
|---|---|---|
| Wallet lifecycle (create/list/get/activate) | :material-check-circle:{ .green } Live | One wallet per person rule (duplicate create → 409). |
| Person records | :material-check-circle:{ .green } Live (backend) | Backend REST + GraphQL; **not** contract-validated in the SDKs. |
| Multi-tenancy (shared DB) | :material-check-circle:{ .green } Live | Tenant from validated token claim; spoofed headers ignored. |
| Per-tenant database | :material-progress-wrench:{ .grey } POC | Provisioning POC exists; shared DB is the decision of record. |
| PII encryption at rest | :material-check-circle:{ .green } Live | AES-256-GCM + searchable HMAC tokens. |

## Authentication

| Capability | Status | Notes |
|---|---|---|
| API key (service-to-service) | :material-check-circle:{ .green } Live | `X-API-Key` + `X-Tenant-Id`. |
| Citizen Bearer JWT | :material-check-circle:{ .green } Live | Refresh rotation; logout revokes. |
| Credentry SSO federation | :material-check-circle:{ .green } Live | M2M + interactive sign-in, live-proven. |
| ServiceWA OIDC | :material-progress-wrench:{ .grey } Opt-in / unconfigured | Placeholder config; fails fast if enabled unconfigured. |
| RS256 / Key Vault signing | :material-check-circle:{ .green } Live | Required outside dev/test; key retrievable today (prod hardening pending). |

## Admin portal

| Surface | Status | Notes |
|---|---|---|
| Dashboard / Tenants / Wallets / Credentials | :material-check-circle:{ .green } Live data | Via GraphQL. |
| Audit logs, admin-user read, metrics, system health | :material-check-circle:{ .green } Live data | Real backend data. |
| Tenant CRUD writes | :material-progress-wrench:{ .grey } Stub | Cache-only; do not persist. |
| Backup, key mgmt, feature flags, configuration, reports | :material-progress-wrench:{ .grey } Stub | In-memory POA stubs. |
| User management | :material-calendar-clock:{ .grey } Moved | Deleted in favour of Credentry; `/users` links the Credentry portal. |

## Operations

| Capability | Status | Notes |
|---|---|---|
| Deployed to AKS (nonprod) | :material-check-circle:{ .green } Live | Public URL behind Front Door. |
| Performance SLA (p95 < 500 ms) | :material-check-circle:{ .green } Pass | On all measured paths; capacity sizing tail noted. |
| CI deploy pipeline | :material-progress-wrench:{ .grey } Built, gated on secrets | Needs GitHub `AZURE_CREDENTIALS` + `nonprod` environment. |
