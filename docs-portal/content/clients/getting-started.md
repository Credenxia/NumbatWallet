---
tags:
  - clients
---

# Getting Started

> **Audience:** Clients. Source: SDK READMEs, `README.md`.

## What you need

- An **API key** + **tenant id** (service-to-service), **or** a citizen **Bearer token**
  for citizen flows (Flutter SDK only — see [Auth Model](auth-model.md)).
- The API origin. Public test environment:
  **`https://tst.numbatwallet.credentry.com.au`**.
- One of the SDKs (recommended) or a plain HTTP/GraphQL client.

## First call (no SDK)

```bash
# Citizen login (controller-name route, NOT /auth/login):
curl -X POST https://tst.numbatwallet.credentry.com.au/api/v1/Authentication/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"<email>","password":"<password>"}'

# Service-to-service GraphQL call with an API key:
curl -X POST https://tst.numbatwallet.credentry.com.au/graphql \
  -H 'X-API-Key: <api-key>' \
  -H 'X-Tenant-Id: <tenant-guid>' \
  -H 'Content-Type: application/json' \
  -d '{"query":"{ myWallets { id name status } }"}'
```

!!! note "`myWallets` is empty for API-key callers"
    An API-key principal is **not a person**, so `myWallets` returns an empty list. List
    wallets per person instead, or use a citizen Bearer token whose subject is a person.

## The shape of things

- **GraphQL** lives at `/graphql`. Wallets, credentials and **presentations** all work
  over GraphQL.
- **REST** mirrors wallets/credentials/persons; presentations are GraphQL-only (except the
  OID4VP request flow at `/api/v1/presentations/requests`).
- Lists like `myWallets` are **plain arrays** (no Relay pagination); credential lists by
  wallet are **Relay connections**.
- In GraphQL responses, dictionary fields (`credentialSubject`, `claims`, `metadata`)
  come back as a **list of `{key, value}` pairs** — the SDKs convert these to dictionaries
  for you. REST returns a normal JSON object.

## Recommended path

Use a [SDK](sdks.md) — they handle the auth headers, the GraphQL transport, retries, the
dictionary-pair quirk, and the error contract for you. Each ships an env-gated
live-contract test suite you can run against a running backend to confirm your setup.
