---
tags:
  - clients
---

# Authentication Model

> **Audience:** Clients. Source: SDK READMEs, `docs/ARCHITECTURE-CURRENT.md §2`.

There are two authentication modes a client uses. (Credentry SSO and ServiceWA OIDC are
platform/federation concerns — see [Tech → Auth Schemes](../tech/auth-schemes.md).)

## Service-to-service — API key

For agency / back-office integrations and all three SDKs:

- Headers: **`X-API-Key`** + **`X-Tenant-Id`**.
- The backend attaches configured roles to the key's principal and scopes all data to the
  declared tenant.
- Tenant is **never** part of request bodies — it comes from the header.
- For production: store keys in Key Vault and scope roles per key.

```http
X-API-Key: <api-key>
X-Tenant-Id: <tenant-guid>
```

## Citizen — Bearer JWT (Flutter SDK only)

For citizen-facing apps (e.g. ServiceWA integration):

1. Authenticate the user: `POST /api/v1/Authentication/login` (**controller-name route**,
   not `/auth/login`) returns a JWT whose subject is the citizen's **person id**.
2. Hand the token to the SDK. Requests send `Authorization: Bearer <token>` and **never**
   `X-API-Key`; no `tenantId` is needed — the backend derives the tenant from the
   validated `tenant_id` claim.
3. Access is automatically scoped to the signed-in citizen: `wallets.list()` returns only
   their wallet; a foreign wallet id returns not-found (IDOR-checked).

!!! warning "Bearer is Flutter-only today"
    The **.NET and TypeScript SDKs are API-key (service-to-service) clients** — they have
    no Bearer mode. Use the **Flutter SDK** for citizen Bearer flows. The Flutter SDK
    enforces **exactly one** mode (API key XOR Bearer) at client construction.

## What auth applies where

Both modes apply to **all REST calls and the GraphQL path** (presentations). The Flutter
SDK strips the API-key header automatically when in Bearer mode.

## Not verified paths

mTLS, OAuth (client-credentials / auth-code) and offline-mode options appear on some
option objects and in older docs, but they are **not verified paths**. Do not build a
pilot around them.
