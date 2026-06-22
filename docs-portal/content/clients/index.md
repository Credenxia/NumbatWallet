---
tags:
  - clients
---

# Clients

> **Audience:** External integrators / client developers. Source: the three SDK READMEs
> and `README.md`.

Everything you need to integrate against NumbatWallet:

- **[Getting Started](getting-started.md)** — the test URL, what you need, first call.
- **[Authentication Model](auth-model.md)** — API-key vs citizen Bearer.
- **[SDK Quickstarts](sdks.md)** — .NET, TypeScript and Flutter, with honest coverage
  notes per SDK.

!!! info "Public test environment"
    **<https://tst.numbatwallet.credentry.com.au>** — GraphQL at `/graphql`, citizen login
    at `POST /api/v1/Authentication/login`.

!!! warning "Read the honesty notes"
    Wallet, credential and presentation operations are **contract-verified** in all three
    SDKs. **Persons / Consent / Passes are not** — treat them as experimental. **Citizen
    Bearer auth is Flutter-only** today. **Presentations are GraphQL-only**. See
    [SDK Quickstarts](sdks.md) for the per-SDK detail.
