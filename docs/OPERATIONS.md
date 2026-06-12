# NumbatWallet — Operations Guide (June 2026)

Operator-facing contract for running NumbatWallet in containers/AKS. As-built
architecture: `docs/ARCHITECTURE-CURRENT.md`. Performance baseline + post-fix results:
`perf/RESULTS-2026-06-12.md`.

## 1. Container environment-variable contract

### API container (`numbatwallet-api`)

| Variable | Meaning / notes |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Testing` on the nonprod namespace (enables seeded test accounts + test auth handler — **test stage only**). Production must use `Production`. |
| `ConnectionStrings__numbatwallet` | PostgreSQL connection string (from k8s secret `numbatwallet-secrets`, sourced from KV `kv-numbatwallet-test-aue` secret `ConnectionStrings--numbatwallet`). |
| `ConnectionStrings__Redis` | OPTIONAL. Empty/unset ⇒ **in-memory distributed cache** (logged at startup: "Distributed cache backend: …"). Non-empty ⇒ Redis with bounded fail-open timeouts forced by `CacheRegistrationPolicy` (`abortConnect=false`, connect 1000 ms, sync 500 ms) unless the string overrides them. There is **no** `localhost:6379` default any more. |
| `Jwt__SecretKey` | HS256 signing/validation secret (required when HMAC signer active; app throws if unset). |
| `Jwt__Signer` / `Jwt__SigningKeyVaultUri` | `KeyVaultRsa` + vault URI enables RS256 via Key Vault. **Mandatory outside Development/Testing** (startup fail-fast). ⚠️ The AKS namespace currently runs `Testing` and has NOT set this — it must be configured before the environment moves to Production. |
| `FieldEncryption__Key` or `FieldEncryption__Source=KeyVault` + `FieldEncryption__KeyVaultUri` | AES-256-GCM field-encryption key (base64, 256-bit) or KV secret `field-encryption-key`. Changing/losing the key makes existing `FE1:` ciphertext unreadable (typed converters then fail ⇒ 500s). |
| `Search__TokenPepper` | Deployment-wide HMAC pepper for email/phone search tokens (base64 ≥16 bytes). **Required** — without it the app falls back to the in-memory mock Key Vault, i.e. a RANDOM pepper per pod start, making every stored token unmatchable after a restart (login breaks). Follow-up: source from KV secret `search-token-pepper` via the pipeline secret. |
| `ApiKey__*` | API-key auth: `ApiKey:Keys` entries + `ApiKey:Roles` (CSV roles attached to key principals). Admin key in secret `numbatwallet-secrets/admin-api-key`. |
| `RateLimiting__PermitLimit` / `__Window` / `__QueueLimit` | Global limiter, partitioned per client (first X-Forwarded-For hop). Code defaults **100/min, queue 10**; the test namespace deliberately runs `100000`/min via Helm so load tests measure the app. **Production should NOT copy the test values.** |
| `Credentry__Enabled`, `Credentry__Issuer`, `Credentry__TenantMap__{tid}` | Credentry SSO federation (fail-closed tenant map: unmapped `tid` ⇒ rejected). |
| `ServiceWA__OidcEnabled` (+ authority/audience) | Opt-in; fails fast if enabled with placeholder config. |
| Forwarded headers | `UseForwardedHeaders` is explicit in code (first in pipeline, trust-all in-cluster); the old `ASPNETCORE_FORWARDEDHEADERS_ENABLED` env is no longer used. |

### Admin container (`numbatwallet-admin`)

| Variable | Meaning |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | Currently `Development` — the cookie/login UI only exists there; **includes the `/auth/test-login` backdoor (accepted test-stage risk)**. Moving off Development requires an Entra app registration + `AzureAd__*` config (pending user step). |
| `services__webapi__http__0` | API base URL for service discovery, e.g. `http://numbatwallet-api`. |
| `AdminApi__*` | API key / tenant the portal's `AdminGraphQLClient` uses. |
| Tenant | Admin must address the deployed seed tenant — `00000000-0000-0000-0000-000000000000` on AKS Testing (local dev uses `…0001`). |

### Migrations image

Runs the EF Core **migrations bundle** (baked at image build) against
`ConnectionStrings__numbatwallet`. Executed as a Helm **pre-install/pre-upgrade hook
Job** — a failed migration blocks the rollout. Non-Development environments use real
migrations only (`MigrateAsync`, fail-fast); the dev EnsureCreated path is never used in
containers. Note: stale 2025 migrations were deleted — history starts at `InitialSchema`
(22 tables) + `AddPersonSearchTokenColumns` + `AddPresentationRequests`.

## 2. Helm chart (`deploy/helm/numbatwallet`)

Templates: `deployment-api`, `deployment-admin`, `migrate-job` (hook), `services`,
`ingress` (class `nginx`, host `tst.numbatwallet.credentry.com.au`, Blazor sticky
sessions). Key values (see comments in `values.yaml` — they are authoritative):

- `image.registry/tag` — `cxausvmcontainerregistry.azurecr.io`, tags `test-{shortSha}`.
- `secretName: numbatwallet-secrets` — created by `deploy/scripts/bootstrap-namespace.sh`
  from KV `kv-numbatwallet-test-aue` (keys: connection-string, jwt-secret-key,
  field-encryption-key, admin-api-key).
- `api.environment`, `api.redisConnectionString` (empty until the shared-services
  NetworkPolicy admits this namespace — then set
  `redis-master.shared-services.svc.cluster.local:6379`), `api.rateLimiting.*`,
  `api.searchTokenPepper` (deploy-time only, never committed), `api.extraEnv`.
- `admin.environment`, `admin.tenantId`.
- Resources: deliberately small burstable footprint (API 25m/300m CPU, 256–512Mi).

Deploy: CI workflow `.github/workflows/deploy-numbatwallet.yml` (dispatch or
push-to-main on deployable paths): 5 unit suites gate → `az acr build` 3 images →
`helm upgrade` → smoke. Requires GitHub secret `AZURE_CREDENTIALS` + a `nonprod`
environment (NOT yet configured — pending user step). Manual image builds must use
`az acr build --platform linux/amd64` from a clean `git archive` context (building from
the working tree uploads ~1 GB and hangs).

## 3. Keys, peppers and rotation implications

| Secret | Where | Rotation consequence |
|---|---|---|
| `Jwt:SecretKey` (HS256) | KV → k8s secret | All outstanding access tokens become invalid (users re-login). Safe otherwise. |
| RS256 signing key (`jwt-signing-key-pem`, KV `kv-numbatwallet-test-aue`) | Key Vault | Same as above for RS256 tokens. Prod hardening: move to KV-side signing / non-exportable key. |
| `FieldEncryption:Key` / KV `field-encryption-key` | config / KV | ⚠️ **Existing ciphertext becomes unreadable** — there is no re-encryption job yet. Rotation requires a decrypt-with-old/encrypt-with-new data migration. In dev, reset the postgres volume instead. |
| `Search:TokenPepper` / KV `search-token-pepper` | Helm value (today) / KV (target) | ⚠️ **All stored search tokens stop matching ⇒ email/phone lookup AND login break** until rows are re-tokenised (re-save persons or run a backfill). Treat the pepper as immutable per deployment. |
| API keys (`admin-api-key`, …) | KV → k8s secret | Update SDK/portal consumers (`AdminApi:` config) in lockstep. |

## 4. Operator runbook (current open items — from perf RESULTS §8)

1. **Seeded-login backfill on AKS (open):** the searchable-PII change landed after the
   original seed, so pre-existing seeded persons have **no** `email_search_token` —
   seeded logins (e.g. `citizen@example.com`) fail with "person not found" on AKS.
   Fix: re-seed the disposable `numbatwallet_test` data or run an authorized backfill
   job; ensure the pepper is stable first. A valid test account exists:
   `john.doe@example.com` / `Test123!@#` (created through the API post-change, so its
   tokens are correct).
2. **Pepper provisioning (open):** `Search__TokenPepper` is currently a deploy-time Helm
   value (set on Helm rev 5/6). Move it into KV `kv-numbatwallet-test-aue` as secret
   `search-token-pepper` and extend `bootstrap-namespace.sh` to project it into
   `numbatwallet-secrets`.
3. **Rate limiter config:** test namespace runs 100k/min deliberately; production must
   use the strict defaults (omit `api.rateLimiting`) and size `PermitLimit` to the SLA
   concurrency per client.
4. **Shared Redis (open):** merge `credentry-infrastructure`
   `feature/numbatwallet-onboarding` (namespace + NetworkPolicies), then set
   `api.redisConnectionString`. Until then the API uses in-memory cache (fine
   single-replica; token blacklist/refresh stores are then per-pod).
5. **`Jwt:Signer` for non-test environments (open):** set `Jwt__Signer=KeyVaultRsa` +
   `Jwt__SigningKeyVaultUri` before any environment runs as Production — startup
   fail-fasts otherwise.
6. **Admin portal auth (open):** Entra app registration, `AzureAd__*`, switch
   `admin.environment` off `Development` (removes the `/auth/test-login` backdoor).
7. **Capacity sizing (deferred, now measurable):** citizen ramp shows a >3 s p95 tail at
   ≥50 VU on the 1-replica/300m/B1ms footprint with 0 errors — schedule a sizing pass
   (replicas/CPU + DB tier) as its own exercise.
8. **Health probe quirk:** admin `/health` returns 302→/login (auth fallback) — probes
   accept it (<400). API `/health` is a plain 200.

## 5. Useful commands

```bash
# Cluster access (docker bin provides kubelogin on dev machines)
export PATH="/Applications/Docker.app/Contents/Resources/bin:$PATH"
az aks get-credentials -g rg-shared-nonprod-aue -n aks-shared-nonprod-aue
kubectl get pods -n numbatwallet-test
kubectl top pods -n numbatwallet-test

# Admin API key
kubectl get secret numbatwallet-secrets -n numbatwallet-test \
  -o jsonpath='{.data.admin-api-key}' | base64 -d

# Smoke (public edge)
curl https://tst.numbatwallet.credentry.com.au/health
# Direct ingress (bypassing Front Door)
curl -k --resolve tst.numbatwallet.credentry.com.au:443:20.92.192.89 \
  https://tst.numbatwallet.credentry.com.au/health

# Startup config confirmation (cache backend + limiter are logged)
kubectl logs deploy/numbatwallet-api -n numbatwallet-test | grep -E "Distributed cache backend|Global rate limiter"
```
