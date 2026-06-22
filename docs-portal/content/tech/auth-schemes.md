---
tags:
  - tech
---

# Authentication Schemes

> **Audience:** Tech. Source: `docs/ARCHITECTURE-CURRENT.md §2`, `README.md`. All schemes
> **coexist**.

When Credentry is enabled, the default scheme is **`CredentrySelector`** — a policy
scheme that peeks at the token issuer (unvalidated decode) and routes the request to the
right scheme.

## 1. Credentry SSO federation (`CredentryJwt`)

- Gated on `Credentry:Enabled`. OIDC discovery against the Credentry IdP (OpenIddict,
  RS256). Dev issuer `http://localhost:5144`; nonprod `https://tst.portal.credentry.com.au`.
- Audience `numbatwallet-api`; product-claim gate (`NUMBATWALLET`).
- Roles mapped from **`NW.*` claims only** (Credentry platform roles deliberately not
  mapped). `NW.Admin` / `NW.Officer` / `NW.Issuer` / `NW.Citizen`.
- Tenant resolved **fail-closed** via `Credentry:TenantMap:{tid}={nwTenantGuid}` — an
  unmapped `tid` is rejected.
- Person enrichment by **email claim** via search-token lookup (fail-soft). **Email is the
  join key** — the Credentry `sub` is not the NumbatWallet person GUID.
- Live-proven for both M2M (client-credentials) and interactive users. Normative spec:
  `credentry/docs/integration/06-NUMBATWALLET-FEDERATION-CONTRACT.md`.

## 2. Self-issued citizen Bearer JWT

- `POST /api/v1/Authentication/login` (**controller-name route**, not `/auth/login`).
- Claims contract (test-pinned): `sub` / NameIdentifier = **person GUID**, roles (incl.
  `Citizen`), `tenant_id`.
- Refresh-token rotation through a distributed `IRefreshTokenStore` (roles persisted across
  rotation; reusing a rotated token → 401); logout revokes the refresh token.
- Token blacklist + refresh store are Redis-backed (`IDistributedCache`) with **fail-safe**
  behaviour when Redis is unreachable.

## 3. API key (service-to-service / SDKs)

- `X-API-Key` + `X-Tenant-Id` headers; middleware runs **before** authorization.
- Principal gets configurable roles (`ApiKey:Roles`), `sub` = `apikey:{keyName}`, and a
  tenant claim from the header (the service declares its tenant).

## 4. ServiceWA OIDC

- Scheme exists but is **opt-in (`ServiceWA:OidcEnabled`) and unconfigured** (placeholder
  authority/audience; fails fast if enabled unconfigured). Until real config exists,
  citizens use scheme 2.

## Dev/Testing only

`TestAuthenticationHandler` + `TestPasswordValidator` (seeded accounts, `Test123!@#`) —
environment-gated, **not registered in Production**.

## JWT signing

`IAccessTokenSigner`:

- `HmacAccessTokenSigner` — **HS256** (default), `Jwt:SecretKey`, no fallback secret
  (throws if unset).
- `KeyVaultRsaAccessTokenSigner` — **RS256**, kid `nw-jwt-rs256`, PEM from Key Vault.

`AccessTokenSignerSelector` makes RS256 **mandatory outside Development/Testing**
(fail-fast; explicit HMAC rejected). 

!!! warning "Production signing hardening"
    The Key Vault secret holds a **retrievable** private key (test-acceptable). Production
    should move to KV-side signing (non-exportable key / Managed HSM).

## Authorization

Policy-based: `AdminOnly`, `AdminOrOfficer`, `SuperAdmin`, … IDOR ownership checks are
verified on wallets and credentials (non-owners get 404/403).

!!! note "A real bug worth knowing"
    A class-level `[Authorize(Policy=AdminOnly)]` on an `[ExtendObjectType]` admin type
    once applied the policy to the **root** Query/Mutation/Subscription — gating *every*
    GraphQL field (incl. `myWallets`) behind Admin. It went unnoticed because all tests
    used admin principals. Fix: per-field attributes, never class-level on extension types.
