---
tags:
  - tech
  - clients
---

# API Surface

> **Audience:** Tech (and Clients). Source: `docs/ARCHITECTURE-CURRENT.md`, the SDK
> READMEs, `perf/RESULTS-2026-06-12.md`. GraphQL endpoint: **`/graphql`**.

!!! note "REST vs GraphQL"
    Wallets / credentials / persons exist on **both** surfaces. **Presentations are
    primarily GraphQL**; REST exists only for the OID4VP presentation-request flow.

## Authentication routes

| Method | Route | Notes |
|---|---|---|
| POST | `/api/v1/Authentication/login` | Citizen/officer login (**controller-name route**). Returns access + refresh tokens. |
| POST | refresh / logout | Refresh-token rotation; logout revokes the refresh token. |

## REST (selected)

| Area | Examples |
|---|---|
| Wallets | `GET/POST /api/v1/wallets…`, get by id, activate. IDOR-checked (404/403 to non-owners). |
| Credentials | `POST /api/v1/credentials/issue`, `GET /api/v1/credentials/wallet/{id}` (plain array), get by id, revoke, share. |
| Persons | `PersonEndpoints` (the one mapped Carter module) + GraphQL. |
| Presentation requests (OID4VP) | `POST /api/v1/presentations/requests` (auth), `GET /{id}` + `POST /{id}/submit` (anonymous, `NoStore`). |
| Health | `GET /health` → 200. |

In REST responses, `credentialSubject` serializes as a normal JSON **object**.

## GraphQL operations

### Wallets

| Operation | Shape |
|---|---|
| `myWallets` | **plain array** (no pagination); empty for API-key principals (not persons). |
| `walletById(id: UUID!)` | single wallet. |
| `createWallet(input:{ personId, name })` | create (one wallet per person → 409 on duplicate). |

### Credentials

| Operation | Notes |
|---|---|
| `credentialById(id: UUID!)` | single. |
| `credentialsByWallet(walletId, first, after)` | Relay connection. |
| `issueCredential(input:{ walletId, credentialType, subject, claimsJson, validFrom, validUntil, issuerOrganizationId })` | enum is `SCREAMING_SNAKE` on the wire. |
| `revokeCredential(input:{ credentialId, reason })` | returns bool. |
| `verifyCredential(...)` | a **mutation**. |
| `shareCredential(...)` | returns share URL/code/expiry. |
| `bulkIssueCredentials(...)` | returns issued **IDs** + per-item errors. |

!!! warning "HotChocolate dictionary quirk"
    In **GraphQL** responses, dictionaries (`credentialSubject`, `metadata`, `claims`)
    serialize as a **list of `{key, value}` pairs**, not a JSON object. The SDKs map these
    back to dictionaries for you. (This is GraphQL-only; REST returns a normal object.)
    Also: a credential's `holderId` is the **wallet** id.

### Presentations (W3C VP-JWT)

| Operation | Notes |
|---|---|
| `presentCredential(input:{ credentialId, verifierId, purpose, selectiveClaims, nonce? })` | → presentation token (VP-JWT) + verification URL + disclosed claims. |
| `verifyPresentation(token: String!)` | `[AllowAnonymous]`; invalid token ⇒ `isValid:false` (never 500). |
| `presentationById(id: UUID!)` / `presentationsByWallet(walletId: UUID!)` | history (plain list). |

### OID4VP subset (experimental)

| Operation | Notes |
|---|---|
| `createPresentationRequest(...)` | DIF PE v2 definition constraining `$.vc.type` + claims, nonce, request URI. |
| `submitPresentation(...)` | anonymous, one-shot (replay rejected, lazy expiry). |

### Admin surface (GraphQL)

| Operation | Backing |
|---|---|
| `systemHealth`, `databaseStats`, `credentialStatistics`, `metrics`, `auditLogs`, `adminUsers` (read), `adminWallets(search, first)`, `tenants` (Relay connection — never selects `connectionString`) | **Real** backend data |
| `featureFlags`, `configurations`, `backups`, `reports`, `toggleFeatureFlag`, `updateRateLimits`, key rotation, `clearCache`, `batchCreateWallets` | **In-memory POA stubs** |

`SuperAdmin`-gated fields enforce their own policy (clean 403 for non-super-admins).
