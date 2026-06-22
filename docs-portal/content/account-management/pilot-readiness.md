---
tags:
  - account-management
  - clients
---

# Pilot Readiness (SDKs)

> **Audience:** Account Management (and Clients). Source: the three SDK READMEs in the
> `NumbatWallet-sdks` repository.

All three SDKs are **contract-verified against a live backend** for their core surface
(wallets, credentials, presentations). Distribution is via a **private feed for now**
(public later). The table below is what you can promise for a pilot.

## Per-SDK readiness

| SDK | Language / package | Transport | Auth | Pilot-ready surface | Not ready / experimental |
|---|---|---|---|---|---|
| **.NET** | C# `net10.0` · `NumbatWallet.Sdk` | GraphQL | API key | Wallets, credentials, presentations (1304 unit + 65 contract tests; live suite green) | Persons/Consent/Passes not contract-validated; **no Bearer mode** (API-key only); OAuth/mTLS options exist but unverified; offline mode best-effort |
| **TypeScript** | TS/JS · `@numbatwallet/sdk` | REST (+ GraphQL for presentations) | API key | Wallets, credentials, presentations (live contract 5/5) | Persons/Consent/Passes not contract-validated; API-key only (no Bearer) |
| **Flutter** | Dart · `numbatwallet_sdk` | REST (+ GraphQL for presentations) | API key **or Bearer JWT** | Wallets, credentials, presentations **incl. citizen flows + IDOR checks** (live 7/7) | Persons/Consent/Passes not contract-validated |

## What "pilot-ready" means here

- **Verified end-to-end** against a running backend: wallet create/list/get, credential
  issue/get/list/revoke, and present → verify (including selective disclosure and
  revoke-then-verify-fails).
- Each SDK ships an **env-gated live-contract test suite** that runs the real SDK against
  a real backend.

## Honest caveats to communicate

- **Citizen (Bearer) flows are Flutter-only today.** The .NET and TypeScript SDKs are
  service-to-service (API-key) clients. For a citizen-facing app pilot, use Flutter.
- **Presentations are GraphQL-only** on the backend. The TS and Flutter SDKs route
  presentation calls through a small GraphQL call on the same auth headers — this is
  handled for you, but it is not a REST endpoint.
- **Persons / Consent / Passes services are experimental** in every SDK — the client code
  exists but the backend contract for those is not validated; some endpoints may not exist
  backend-side yet. Do not scope a pilot around them.
- **mTLS / OAuth / offline-mode** options that appear in older docs or option objects are
  **not verified paths**.

For the actual quickstarts to hand a client developer, see the
[Clients → SDK Quickstarts](../clients/sdks.md) page.
