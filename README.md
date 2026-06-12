# NumbatWallet — Digital Wallet & Verifiable Credentials Platform

NumbatWallet is a digital wallet and verifiable-credentials platform (originating from WA
DPC tender DPC2142, continuing as a Credenxia platform product). It issues, holds, presents
and verifies digital credentials (W3C Verifiable Credentials, JWT-VC/VP, OID4VP subset),
with multi-tenant isolation, encrypted PII at rest, and client SDKs for .NET, TypeScript
and Flutter.

This repository contains the backend: REST + GraphQL API, Blazor admin portal, EF Core /
PostgreSQL persistence, Helm chart and CI deployment to AKS.

> **Honesty note:** this README describes what is actually built and verified as of
> June 2026. Items that are stubs, test-stage-only, or pending are called out explicitly.
> See `docs/ARCHITECTURE-CURRENT.md` for the as-built architecture and
> `docs/OPERATIONS.md` for the operator guide.

## Current capabilities (verified working)

- **Credential lifecycle** — issue, fetch, list-by-wallet, verify, share, revoke, bulk
  issue (REST + GraphQL). Revocation is re-checked at verification time.
- **Presentations** — spec-conformant **W3C VP-JWT** tokens (VC Data Model v1.1 JWT
  mapping): selective disclosure of claims, nonce/audience binding, embedded JWT-VC,
  signature + lifetime + revocation re-checks on verify (verification failures return
  `isValid:false`, never 500). Plus a **minimal OID4VP subset** (experimental): DIF
  Presentation Exchange v2 presentation requests with one-shot, replay-rejected
  submission (GraphQL + REST `/api/v1/presentations/requests`). SD-JWT and
  multi-credential VPs are not implemented.
- **Multi-auth** (all schemes coexist):
  - **Citizen Bearer JWT** — `POST /api/v1/Authentication/login` (note: controller-name
    route, not `/auth/login`) issues a JWT with person-GUID subject, roles, and tenant
    claim; refresh-token rotation with distributed store; logout revokes the refresh token.
  - **Credentry SSO federation** — `CredentryJwt` bearer scheme (OIDC discovery, RS256,
    audience `numbatwallet-api`, product gate, `NW.*` role mapping, fail-closed
    `Credentry:TenantMap`) routed by a `CredentrySelector` policy scheme; admin portal
    offers "Sign in with Credentry" (auth-code + PKCE). Live-proven for both M2M and
    interactive users. See `/repo/credentry/docs/integration/06-NUMBATWALLET-FEDERATION-CONTRACT.md`.
  - **API key (service-to-service / SDKs)** — `X-API-Key` + `X-Tenant-Id` headers,
    configurable roles per key (`ApiKey:` config).
  - ServiceWA OIDC scheme exists but is **opt-in and unconfigured** (placeholder values;
    fails fast if enabled without real config).
- **PII encryption at rest** — AES-256-GCM field encryption (`FE1:` token format) for
  person FirstName/LastName/DateOfBirth/Email/Phone, with **deterministic HMAC search
  tokens** (`email_search_token`/`phone_search_token`, deployment-wide pepper) so
  login/lookup by email still works against ciphertext.
- **Multi-tenancy** — tenant resolved from the validated JWT claim (header spoofing
  ignored outside dev), global EF query filters, fail-closed defaults outside
  Development. Shared database for now (per-tenant-DB provisioning is a future option —
  a POC exists).
- **JWT signing** — HS256 by default; RS256 via Azure Key Vault (`Jwt:Signer=KeyVaultRsa`)
  is **required outside Development/Testing** (fail-fast at startup).
- **Admin portal (Blazor)** — Dashboard, Tenants, Wallets, Credentials pages render
  **live backend data** via GraphQL. Still mock/stub: backup, key management, master/tenant
  dashboards, create/issue/revoke action modals, and placeholder shells (ApiKeys,
  Webhooks, etc.). User management was **deleted** in favour of Credentry (the `/users`
  page links to the Credentry portal).
- **Hardened** — IDOR ownership checks on wallets/credentials (404/403 to non-owners),
  output-cache never caches authenticated responses, rate limiter is proxy-aware and
  configurable, token blacklist/refresh stores are distributed (Redis) with fail-safe
  fallbacks, test-password backdoor is environment-gated to Development/Testing.

## Repository structure

```
src/
├── NumbatWallet.Domain/          # Pure domain: aggregates, events, specifications
├── NumbatWallet.Application/     # Custom CQRS (NO MediatR): commands/queries/handlers
├── NumbatWallet.Infrastructure/  # EF Core 10 + PostgreSQL, crypto, Key Vault, caching
├── NumbatWallet.Web.Api/         # REST controllers + HotChocolate GraphQL
├── NumbatWallet.Web.Admin/       # Blazor Server admin portal
├── NumbatWallet.AppHost/         # .NET Aspire local orchestration
└── Tests/                        # Unit + integration test projects
deploy/
├── helm/numbatwallet/            # Helm chart (api, admin, migrations Job, ingress)
└── scripts/bootstrap-namespace.sh # KV secrets -> k8s secret
perf/                             # k6 SLA harness + RESULTS-2026-06-12.md
docs/                             # ARCHITECTURE-CURRENT.md, OPERATIONS.md, standards, ...
```

Stack: **.NET 10 / C# 14**, EF Core 10, PostgreSQL 17, HotChocolate 15 (GraphQL),
Carter (PersonEndpoints), FluentValidation, Serilog, .NET Aspire (dev), Helm/AKS (deploy).

## Running locally

Prerequisites: .NET SDK 10.0.1xx (see `global.json`; `rollForward: latestFeature`),
Docker Desktop running (`open -a Docker`).

```bash
# Aspire DCP needs the docker CLI on PATH (not guaranteed in every shell):
env PATH="/Applications/Docker.app/Contents/Resources/bin:$PATH" \
  ASPIRE_ALLOW_UNSECURED_TRANSPORT=true \
  dotnet run --project src/NumbatWallet.AppHost --launch-profile http
```

Aspire assigns dynamic ports — find the Web.Api/Web.Admin URLs in the Aspire dashboard
output (or `lsof` on the pids). On first run the dev database is schema-created and
seeded automatically (`SKIP_DB_MIGRATION=true` disables seeding).

**Dev logins** (TestPasswordValidator, Development/Testing only):

| Account | Password | Roles |
|---|---|---|
| `admin@example.com` | `Test123!@#` | Admin |
| `citizen@example.com` | `Test123!@#` | Citizen, User |

```bash
# Citizen/officer login (NOTE the controller-name route):
curl -X POST http://127.0.0.1:<port>/api/v1/Authentication/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"citizen@example.com","password":"Test123!@#"}'
```

**Dev service auth (SDK-style)**: header `X-API-Key: test-api-key-development-only` plus
`X-Tenant-Id: 00000000-0000-0000-0000-000000000001` (the dev seed tenant — note the
deployed Testing environment seeds tenant `00000000-0000-0000-0000-000000000000` instead).

GraphQL endpoint: `/graphql`. Dev caching is in-memory (Redis only used when a non-empty
`ConnectionStrings:Redis` is configured; the active backend is logged at startup).

If you change the dev `FieldEncryption:Key` or `Search:TokenPepper`, reset the postgres
data volume (`numbatwallet.apphost-*-postgres-data`) — existing ciphertext/search tokens
become unreadable otherwise.

## Tests

```bash
dotnet build NumbatWallet.sln          # 0 errors (warnings are NOT yet zero — see gaps)
dotnet test                             # all suites
```

Verified suite state (2026-06-13):

| Suite | Result |
|---|---|
| Unit — Domain 202, SharedKernel 52, Application 147, Infrastructure 326, Web.Api 108 | **835 passed / 0 failed** |
| Integration (`NumbatWallet.Integration.Tests`) | **84 passed / 0 failed / 2 skipped** (both skips documented: TestServer can't enforce `MaxRequestBodySize`; output cache excludes authenticated requests) |
| SDK live-contract suites (separate repo) | .NET 65/0, TypeScript 5/5, Flutter 7/7 against a live stack |

CI gate: the deploy workflow runs all 5 unit suites and blocks deployment on any failure.

## Deployment

Deployed to the **shared nonprod AKS cluster** `aks-shared-nonprod-aue`
(subscription "Credenxia AU", namespace `numbatwallet-test`), following the
`credentry-infrastructure` product conventions:

- Public URL: **https://tst.numbatwallet.credentry.com.au** (Front Door → shared NGINX
  ingress; Front Door adds ~120 ms per request).
- Images: `cxausvmcontainerregistry.azurecr.io/numbatwallet-{api,admin,migrations}`
  (built with `az acr build` from a clean `git archive` context).
- Helm chart: `deploy/helm/numbatwallet` — API + Admin deployments, ingress, and an EF
  Core **migrations-bundle Job** as a pre-install/pre-upgrade hook.
- Database: `numbatwallet_test` on shared PostgreSQL flexible server
  `psql-shared-nonprod-aue` (role `numbatwallet_app`; connection string in Key Vault
  `kv-numbatwallet-test-aue`).
- CI: `.github/workflows/deploy-numbatwallet.yml` (manual dispatch + push-to-main on
  deployable paths). **Required GitHub setup: `AZURE_CREDENTIALS` repo secret and a
  `nonprod` environment** — not yet configured.
- Environment contract: API runs `ASPNETCORE_ENVIRONMENT=Testing` (seeded test logins
  enabled — test stage only); Admin runs `Development` (its cookie-login UI only exists
  there; includes a `/auth/test-login` backdoor — an accepted test-stage risk until an
  Entra app registration exists).

Performance (k6, see `perf/RESULTS-2026-06-12.md`): after the 2026-06-13 fixes the
deployed app meets p95 < 500 ms on all measured paths (citizen journey p95 124.8 ms
steady; 0 errors / 0 throttling at 100 VU on health). Remaining tail >3 s at ≥50 VU on
the citizen ramp is **capacity sizing** of the deliberate test footprint (1 replica,
300m CPU, burstable B1ms PostgreSQL), not a config defect.

## SDKs

Client SDKs live in a separate repo: **[NumbatWallet-sdks](https://github.com/Credenxia/NumbatWallet-sdks)**
(.NET — GraphQL transport; TypeScript and Flutter — REST + a GraphQL path for
presentations). Wallet, credential and presentation operations of all three SDKs are
**contract-verified against a live backend**. Distribution is via a **private feed** for
now (public later).

## Known gaps / follow-ups (honest list)

- **AKS seeded-login backfill (operator action):** the searchable-PII change means
  pre-existing seeded persons on AKS lack `email_search_token` values — seeded logins
  like `citizen@example.com` fail there ("person not found") until the test data is
  re-seeded or backfilled. The pepper should move to KV secret `search-token-pepper`
  (currently a deploy-time Helm value). A working test account `john.doe@example.com`
  (`Test123!@#`) was created through the API. See `perf/RESULTS-2026-06-12.md` §8.
- **AKS `Jwt:Signer` config:** RS256 signing is now mandatory outside Development/Testing;
  the nonprod deployment must set `Jwt:Signer=KeyVaultRsa` + `Jwt:SigningKeyVaultUri`
  before its environment moves off `Testing`.
- **Admin portal stubs:** backup/key-management/feature-flag/configuration/report admin
  surfaces are in-memory POA stubs (`InMemoryAdminServices.cs`); admin tenant CRUD writes
  don't persist; several pages remain placeholders (marked `POA: mock data` in code).
- **Capacity sizing:** test footprint intentionally minimal; a sizing pass
  (replicas/CPU/DB tier) is a pending exercise now that requests actually reach the app.
- **Build warnings:** the "zero warnings" goal is not currently met; known-remaining
  advisories are transitive only (MessagePack via Aspire AppHost; DataProtection,
  test-only).
- **Integration-suite skips:** 2 deliberate, documented skips (see Tests above).
- **Pending user/manual steps:** GitHub `AZURE_CREDENTIALS` + `nonprod` environment;
  merge of `credentry-infrastructure` `feature/numbatwallet-onboarding` (then point the
  API at shared Redis); Entra app registration for the admin portal; Credentry nonprod
  client provisioning/secrets handover.

## Related repositories

- [NumbatWallet-sdks](https://github.com/Credenxia/NumbatWallet-sdks) — .NET, TypeScript, Flutter SDKs
- [Project wiki](https://github.com/Credenxia/NumbatWallet/wiki) — tender-era PRD + specs (see the
  "Current state" banner on Home for what supersedes it)
- `credentry-infrastructure` — shared AKS platform (Bicep/Helm/k8s-shared)
- `credentry` — Credentry platform (SSO/IdP); federation contract in `docs/integration/06-*`

## License

Developed under tender agreement DPC2142; continues as a Credenxia platform product.
