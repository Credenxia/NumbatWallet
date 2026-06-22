---
tags:
  - tech
---

# Architecture (As-Built)

> **Audience:** Tech. This page **summarises** `docs/ARCHITECTURE-CURRENT.md` — read that
> file for the authoritative, full detail.

## Layers (Clean Architecture + DDD, custom CQRS)

| Project | Role |
|---|---|
| `NumbatWallet.Domain` | Aggregates (Wallet, Credential, Person, Issuer, **Presentation**, **PresentationRequest**, …), domain events, specifications. No external deps. |
| `NumbatWallet.Application` | `ICommandHandler<,>` / `IQueryHandler<,>` handlers, DTOs, FluentValidation validators, app services, `IAccessTokenSigner` abstraction. |
| `NumbatWallet.Infrastructure` | EF Core 10 + Npgsql (PostgreSQL 17), repositories, crypto (`AesGcmFieldEncryptor`, `CryptoService`), Key Vault, distributed token stores, caching policy, search-token interceptor, admin POA stubs. |
| `NumbatWallet.Web.Api` | REST controllers (+ one Carter module: `PersonEndpoints`), HotChocolate GraphQL at `/graphql`, auth schemes, rate limiter, output cache, forwarded-headers handling. |
| `NumbatWallet.Web.Admin` | Blazor Server admin portal; talks to the API via `AdminGraphQLClient`. |
| `NumbatWallet.AppHost` | .NET Aspire local orchestration. |

Dependency flow: Domain ← Application ← Infrastructure ← Web. Domain has no external
dependencies.

## REST vs GraphQL

Wallets / credentials / persons / presentations exist on both surfaces, **except
presentations are primarily GraphQL** (`presentCredential`, `verifyPresentation`,
`presentationById`, `presentationsByWallet`), with REST only for the OID4VP presentation
requests (`/api/v1/presentations/requests`). See [API Surface](api-surface.md).

## Presentations (W3C VP-JWT + OID4VP subset)

- `presentCredential` issues a **W3C VP-JWT** (VC Data Model v1.1 JWT mapping): `iss`/`sub`
  = `urn:uuid:<walletId>`, `aud` = verifierId, `jti` = presentation id (persisted
  `Presentation` aggregate), `nbf`/`exp` (default 15 min), optional `nonce`, and a `vp`
  claim embedding a **JWT-VC** whose `credentialSubject` contains **only disclosed claims**
  (full-redisclosure; **SD-JWT descoped**).
- `verifyPresentation` validates VP signature/lifetime/nonce/structure/audience↔record,
  the embedded VC signature/structure/`jti`↔credentialId, and **re-checks credential
  status** (revoked-after-presentation fails verify). Algorithm pinned (no
  alg-confusion). ~20 failure modes all return `isValid:false` — never 500.
- **OID4VP subset (experimental):** `createPresentationRequest` produces a DIF
  Presentation Exchange v2 definition; `submitPresentation` is anonymous, one-shot
  (replay rejected, lazy expiry default 30 min).
- **Not implemented:** SD-JWT, multi-credential VPs, ISO 18013-5 mDL transfer.

## Multi-tenancy

- Tenant context comes from the **validated JWT claim**; `X-Tenant-Id` is honoured only
  for API-key principals (which declare their tenant) and unauthenticated dev requests.
  Spoofed headers are ignored.
- Global EF query filters scope tenant-owned aggregates; `TenantInterceptor` blocks
  cross-tenant writes; defaults **fail closed** (`Guid.Empty`) outside Development.
- **Shared database** (decision of record, June 2026); per-tenant-DB provisioning is a POC
  for later. Seed tenant differs by environment (dev `…0001`, AKS Testing all-zeros).

## Admin portal — real vs stub

See [Support → Known Issues §5](../support/known-issues.md) for the honest inventory.
Dashboard/Tenants/Wallets/Credentials render live GraphQL data; backup/key-mgmt/
feature-flag/config/report surfaces are in-memory POA stubs; user management was deleted
in favour of Credentry.

## Known architectural debt

`Wallet.PersonId1` shadow FK; mixed table naming; dev `EnsureCreated`/`CreateTables`
won't ALTER an existing dev DB; inconsistent Key Vault config keys; no issuer-list API.
Full list in `docs/ARCHITECTURE-CURRENT.md §8`.
