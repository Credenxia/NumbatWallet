# NumbatWallet performance harness (k6)

SLA targets exercised by this harness: **p95 < 500 ms, 100+ concurrent users,
error rate ~0**.

## Layout

```
perf/
├── lib/common.js            shared helpers (env, headers, login, GraphQL, thresholds, profiles)
├── health.js                baseline floor — GET /health (no auth, no DB)
├── citizen-journey.js       citizen read journey — login(once) + GraphQL myWallets + REST GET /wallets
├── credential-ops.js        admin/API-key — credentialsByWallet + credential(id) reads (90%) + issue write-mix (<=1/s)
├── presentation-verify.js   verifier hot path — present once (setup), then hammer verifyPresentation
├── results/                 JSON summaries written by handleSummary (and the captured baseline run)
└── RESULTS-2026-06-12.md     nonprod AKS baseline + analysis + SLA verdict
```

Every script encodes the SLA as k6 thresholds (`lib/common.js → SLA_THRESHOLDS`):

```
http_req_duration: p(95) < 500ms        (SLA)  + p(95) < 3000ms abortOnFail  (shared-cluster guard)
http_req_failed:   rate < 1%            (SLA)  + rate  < 5%      abortOnFail  (shared-cluster guard)
throttled_429:     rate < 1%            (reporting-only; share rejected by the global rate limiter)
```

HTTP 429 (rate-limit rejections) are marked *expected* via `http.setResponseCallback`,
so they are tracked in `throttled_429` but kept **out** of `http_req_failed` — the
cluster-protective `abortOnFail` then trips only on genuine 5xx/timeout stress, not
on cheap throttling.

## Parameters (env vars)

| var | meaning | local default |
|-----|---------|---------------|
| `BASE_URL` | API origin | `http://127.0.0.1:61299` |
| `HOST_HEADER` | `Host:` override when hitting the ingress IP directly | *(empty)* |
| `API_KEY` | `X-API-Key` (admin/service paths) | `test-api-key-development-only` |
| `TENANT_ID` | `X-Tenant-Id` | `00000000-0000-0000-0000-000000000001` |
| `CITIZEN_EMAIL` / `CITIZEN_PASSWORD` | seeded citizen login | `citizen@example.com` / `Test123!@#` |
| `ISSUER_ID` | issuer-org GUID enabling the credential-ops issue write-mix | *(empty → write-mix skipped)* |
| `PROFILE` | `local` \| `steady` \| `ramp` | `local` |

### Profiles

- **`local`** — 10 VUs / 30 s. Quick harness validation.
- **`steady`** — constant 40 iterations/min (≤ ~80 req/min, **below** the deployed
  100/min global limiter). Measures **true app latency** (p50/p95/p99) without the
  limiter distorting the numbers.
- **`ramp`** — ramping VUs 10 → 50 → 100, < 3 min active load per scenario
  (shared-cluster blast-radius rule). Demonstrates the throughput ceiling.

## Running

```bash
# 1. Local validation (Aspire stack URL in /tmp/nw-api.txt)
BASE_URL=http://127.0.0.1:61299 \
API_KEY=test-api-key-development-only TENANT_ID=00000000-0000-0000-0000-000000000001 \
ISSUER_ID=<local-issuer-guid> PROFILE=steady \
k6 run --summary-trend-stats "avg,min,med,p(90),p(95),p(99),max" perf/citizen-journey.js

# 2. AKS nonprod — hit the ingress IP directly with a Host override
#    (public DNS currently points at Front Door, which has no route yet → 404).
#    Read the admin API key:  kubectl get secret numbatwallet-secrets -n numbatwallet-test \
#      -o jsonpath='{.data.admin-api-key}' | base64 -d
export BASE_URL=http://20.92.192.89
export HOST_HEADER=tst.numbatwallet.credentry.com.au
export API_KEY=<admin-api-key> TENANT_ID=00000000-0000-0000-0000-000000000000
PROFILE=steady k6 run perf/health.js          # true app latency, under the limiter
PROFILE=ramp   k6 run perf/health.js          # throughput ceiling (expect heavy 429)
```

kubectl/kubelogin need Docker's bin on PATH:
`export PATH="/Applications/Docker.app/Contents/Resources/bin:$PATH"`.
Watch resources during a run: `kubectl top pods -n numbatwallet-test`.

### Edge re-run (once the Front Door route is live)

The public hostname will route through Front Door to the ingress. Re-run the **same
scripts** with no Host override:

```bash
export BASE_URL=https://tst.numbatwallet.credentry.com.au
unset HOST_HEADER
export API_KEY=<admin-api-key> TENANT_ID=00000000-0000-0000-0000-000000000000
PROFILE=ramp k6 run perf/citizen-journey.js
```

## Known constraints baked into the harness

- **Global rate limiter (100 req/min per remote IP).** The deployed API enforces a
  hardcoded fixed-window limiter and does **not** call `UseForwardedHeaders`, so behind
  the NGINX ingress all clients share one bucket keyed on the ingress pod IP. The
  `steady` profile stays under this cap to measure app latency; the `ramp` profile
  demonstrates the ceiling. See `RESULTS-2026-06-12.md`.
- **Issuer ids are not exposed by any API** (introspection disabled; no issuers query;
  `createOrganization`/`organizations` are broken). So the credential-ops **issue
  write-mix** and the **real presentation happy-path** are only exercisable where the
  seeded issuer GUID is known (local). Pass `ISSUER_ID` to enable the write-mix; on AKS
  credential-ops runs the read path and presentation-verify uses a synthetic token
  (still exercises JWT parse + signature + DB lookup; returns `isValid:false`, HTTP 200).
