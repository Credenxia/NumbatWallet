# NumbatWallet — As-Built Architecture (June 2026)

Concise, honest description of what is actually running. Anything stubbed or pending is
flagged. Companion operator guide: `docs/OPERATIONS.md`.

## 1. Layers

Clean Architecture + DDD with a **custom CQRS implementation (no MediatR)**:

| Project | Role |
|---|---|
| `NumbatWallet.Domain` | Aggregates (Wallet, Credential, Person, Issuer, **Presentation**, **PresentationRequest**, …), domain events, specifications. No external deps. |
| `NumbatWallet.Application` | `ICommandHandler<,>` / `IQueryHandler<,>` handlers, DTOs, FluentValidation validators, app services, `IAccessTokenSigner` abstraction. |
| `NumbatWallet.Infrastructure` | EF Core 10 + Npgsql (PostgreSQL 17), repositories, crypto (`AesGcmFieldEncryptor`, `CryptoService`), Key Vault integration, distributed token stores, caching policy, search-token interceptor, admin POA stubs. |
| `NumbatWallet.Web.Api` | REST controllers (+ one Carter module: `PersonEndpoints`), HotChocolate 15 GraphQL at `/graphql`, auth schemes, rate limiter, output cache, forwarded-headers handling. |
| `NumbatWallet.Web.Admin` | Blazor Server admin portal; talks to the API via `AdminGraphQLClient` (`AdminApi:` config). |
| `NumbatWallet.AppHost` | .NET Aspire local orchestration (postgres container, dynamic ports). |

GraphQL rule (learned the hard way): HotChocolate `[ExtendObjectType]` types are
**singletons** — scoped services (handlers, repos, DbContext) must be injected via
`[Service]` resolver parameters, never the constructor (captive-dependency →
"disposed context" failures).

REST vs GraphQL: wallets/credentials/persons/presentations exist on both, except
**presentations are primarily GraphQL** (`presentCredential`, `verifyPresentation`,
`presentationById`, `presentationsByWallet`) with REST only for OID4VP presentation
requests (`/api/v1/presentations/requests`).

## 2. Authentication schemes (all coexist)

Default scheme when Credentry is enabled is **`CredentrySelector`**, a policy scheme that
decodes the token issuer (unvalidated peek) and routes:

1. **`CredentryJwt`** (`CredentryAuthenticationExtensions.cs`, gated on `Credentry:Enabled`)
   — OIDC discovery against the Credentry IdP (OpenIddict, RS256; dev
   `http://localhost:5144`, nonprod `https://tst.portal.credentry.com.au`), audience
   `numbatwallet-api`, product claim gate (`NUMBATWALLET`), roles mapped from `NW.*`
   claims only (Credentry platform roles deliberately NOT mapped), tenant resolved
   **fail-closed** via `Credentry:TenantMap:{tid}={nwTenantGuid}`, person enrichment by
   email claim via search-token lookup (fail-soft; **email is the join key** — Credentry
   `sub` ≠ NumbatWallet person Guid). Normative spec:
   `credentry/docs/integration/06-NUMBATWALLET-FEDERATION-CONTRACT.md`.
2. **Self-issued Bearer JWT** — `POST /api/v1/Authentication/login` (controller-name
   route). Claims contract (test-pinned): `sub`/NameIdentifier = person Guid, roles
   (incl. `Citizen`), `tenant_id`. Refresh-token rotation through `IRefreshTokenStore`
   (distributed; roles persisted across rotation; reuse of a rotated token → 401);
   logout revokes the refresh token. Token blacklist + refresh store are
   Redis-backed (`IDistributedCache`) with fail-safe behaviour when Redis is unreachable.
3. **API key** (service-to-service / SDKs) — `X-API-Key` + `X-Tenant-Id` headers;
   middleware runs **before** authorization; principal gets configurable roles
   (`ApiKey:Roles`), `sub` = `apikey:{keyName}`, and a tenant claim from the header.
4. **ServiceWA OIDC** — scheme exists but is **opt-in (`ServiceWA:OidcEnabled`) and
   unconfigured** (placeholder authority/audience; fails fast if enabled unconfigured).
   Until real ServiceWA config exists, citizens use route (2).
5. Dev/Testing only: `TestAuthenticationHandler` + `TestPasswordValidator` (seeded
   accounts, `Test123!@#`) — environment-gated, not registered in Production.

**JWT signing:** `IAccessTokenSigner` — `HmacAccessTokenSigner` (HS256, `Jwt:SecretKey`,
no fallback secret: throws if unset) or `KeyVaultRsaAccessTokenSigner` (RS256, kid
`nw-jwt-rs256`, PEM from Key Vault). `AccessTokenSignerSelector` makes RS256
**mandatory outside Development/Testing** (fail-fast; explicit Hmac rejected).
Known limitation: the KV secret holds a retrievable private key (test-acceptable);
production should move to KV-side signing (non-exportable key / Managed HSM).

**Authorization:** policy-based (`AdminOnly`, `AdminOrOfficer`, `SuperAdmin`, …).
IDOR ownership checks verified on wallets and credentials (non-owners get 404/403).

## 3. Presentations (W3C VP-JWT + OID4VP subset)

- `presentCredential` issues a **W3C VP-JWT** (VC Data Model v1.1 JWT mapping):
  `iss`/`sub` = `urn:uuid:<walletId>`, `aud` = verifierId, `jti` = presentation id
  (persisted `Presentation` aggregate), `nbf`/`exp` (default 15 min,
  `Presentation:TokenLifetimeMinutes`), optional `nonce`, `vp` claim embedding a
  **JWT-VC** whose `credentialSubject` contains **only the disclosed claims**
  (full-redisclosure selective disclosure; SD-JWT descoped).
- `verifyPresentation` validates VP signature/lifetime/nonce/structure/audience↔record,
  the embedded VC signature/structure/`jti`↔credentialId, and **re-checks credential
  status** (revoked-after-presentation fails verify). Algorithm is pinned
  (no alg-confusion). ~20 failure modes all return `isValid:false` — never 500.
  Verification counts are tracked per presentation.
- **OID4VP subset (experimental):** `createPresentationRequest` produces a DIF
  Presentation Exchange v2 definition (constrains `$.vc.type` +
  `$.vc.credentialSubject.<claim>`) with nonce + request URI; `submitPresentation` is
  anonymous, validates nonce/audience/type/claim satisfaction, and is **one-shot**
  (replay rejected; lazy expiry, default 30 min). REST:
  `POST /api/v1/presentations/requests` (auth), `GET /{id}` + `POST /{id}/submit`
  (anonymous, `NoStore`).
- Not implemented: SD-JWT, multi-credential VPs, ISO 18013-5 mDL transfer.

## 4. PII encryption design

- **Field encryption:** `AesGcmFieldEncryptor` (AES-256-GCM, token format
  `FE1:base64(nonce|tag|cipher)`, plaintext passthrough on decrypt for legacy rows),
  wired through `ProtectedFieldConverter` into `PersonConfiguration` for
  FirstName/LastName/DateOfBirth/Email/Phone. Key from config
  (`FieldEncryption:Key`, dev) or Key Vault secret `field-encryption-key`
  (`FieldEncryption:Source=KeyVault` + `FieldEncryption:KeyVaultUri`).
- **Searchability:** deterministic **HMAC search tokens** in shadow columns
  `email_search_token` / `phone_search_token`, computed by `SearchTokenInterceptor` on
  save using a deployment-wide pepper (`Search:TokenPepper` config or KV secret
  `search-token-pepper`). All email/phone lookups — **including login** — go through the
  tokens. ⚠️ Rotating the pepper (or losing it) invalidates every stored token and
  breaks login until rows are re-tokenised; changing the field key makes existing
  ciphertext unreadable. See OPERATIONS.md.
- API responses return plaintext (decrypted in-process); DB rows contain no plaintext
  for the protected fields (verified by inspection).

## 5. Multi-tenancy

- Tenant context (`ICurrentTenantService`) comes from the **validated JWT claim**;
  the `X-Tenant-Id` header is honoured only for API-key principals (which declare their
  tenant) and unauthenticated dev requests. Spoofed headers are ignored (verified).
- Global EF query filters scope all tenant-owned aggregates; `TenantInterceptor` blocks
  cross-tenant writes; defaults **fail closed** (Guid.Empty) outside Development.
- **Shared database** for now (user decision, June 2026); a per-tenant-DB-on-same-server
  provisioning POC exists for later. Seed tenants differ by environment: local dev
  `…0001`, deployed Testing env `00000000-0000-0000-0000-000000000000`.

## 6. Deployment topology (nonprod)

```
Internet → Azure Front Door (cxaus-test-fd-shared, +~120ms)
        → shared NGINX ingress (AKS aks-shared-nonprod-aue, australiaeast)
        → namespace numbatwallet-test
             ├─ numbatwallet-api    (ASPNETCORE_ENVIRONMENT=Testing, 1 replica)
             ├─ numbatwallet-admin  (ASPNETCORE_ENVIRONMENT=Development, Blazor sticky sessions)
             └─ migrations Job      (EF migrations bundle, Helm pre-install/pre-upgrade hook)
        → shared PostgreSQL flexible server psql-shared-nonprod-aue (B1ms), DB numbatwallet_test
Secrets: kv-numbatwallet-test-aue → k8s secret numbatwallet-secrets (bootstrap-namespace.sh)
Cache:   in-memory (shared Redis pending infra-branch merge + NetworkPolicy)
```

Public URL: `https://tst.numbatwallet.credentry.com.au`. Rate limiter is proxy-aware
(first `X-Forwarded-For` hop; `UseForwardedHeaders` first in pipeline, trust-all
in-cluster — acceptable because only the ingress can reach the pods via NetworkPolicy
and NGINX rewrites client XFF). Output cache never caches authenticated requests.

Performance: see `perf/RESULTS-2026-06-12.md` — post-fix the app meets p95<500 ms on all
measured paths; remaining ≥50 VU tail is capacity of the deliberate 1-replica/300m/B1ms
test footprint.

## 7. Admin portal — real vs stub (honest inventory)

| Surface | State |
|---|---|
| Dashboard, Tenants, Wallets, Credentials pages | **Live data** via GraphQL (`adminWallets`, `tenants` (Relay connection, never selects connectionString), `databaseStats`, `credentialStatistics`) |
| auditLogs, adminUsers (read), metrics, systemHealth | **Real** backend data |
| tenants admin query | Config-backed (`Tenants:` section); CRUD writes are cache-only (don't persist) |
| featureFlags, configurations, backups, reports, key rotation, rate-limit update, clearCache, batchCreateWallets | **In-memory POA stubs** (`Infrastructure/Services/InMemoryAdminServices.cs`) |
| /backup, /keys, master/tenant dashboards, create/issue/revoke modals, placeholder shells (ApiKeys, Webhooks, …) | **Mock** (marked `POA: mock data — no backend endpoint yet`) |
| User management | **Deleted** — managed in Credentry; `/users` links to the Credentry portal (`Credentry:PortalUrl`) |

## 8. Known architectural debt

- Controllers vs Carter duplication resolved for wallets/credentials/bulk (controllers
  kept); `PersonEndpoints` remains the one mapped Carter module.
- `Wallet.PersonId1` shadow FK; mixed table naming (PascalCase vs snake_case).
- Dev schema management uses `EnsureCreated`/`CreateTables` (won't ALTER an existing dev
  DB — reset the volume for new tables); real migrations are non-dev only.
- Key Vault config keys inconsistent across components (`KeyVault:Uri` vs
  `KeyVault:Url` vs `Azure:KeyVault:VaultUri`) — needs unifying.
- No issuer-list API (introspection disabled; `organizations`/`createOrganization`
  broken on AKS) — affects perf write-mix and any issuer-picker UI.
