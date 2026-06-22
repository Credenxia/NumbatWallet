// Shared helpers for the NumbatWallet k6 performance harness.
//
// All scenarios are parameterised by environment variables so the same scripts
// run unchanged against the local Aspire stack, the AKS nonprod ingress (with a
// Host-header override), or — once the Front Door route is live — the public
// edge URL with no override.
//
//   BASE_URL     required, e.g. http://20.92.192.89  or  http://127.0.0.1:61299
//   HOST_HEADER  optional, e.g. tst.numbatwallet.credentry.com.au
//                (set when hitting the AKS ingress IP directly; omit at the edge)
//   API_KEY      admin/service API key (X-API-Key) for the credential/admin paths
//   TENANT_ID    tenant GUID (X-Tenant-Id) for API-key requests
//   CITIZEN_EMAIL / CITIZEN_PASSWORD  seeded citizen login (defaults below)
//   ISSUER_ID    optional issuer-organisation GUID enabling the issue write-mix
//                (credential-ops). When unset, the write-mix is skipped.
//   PROFILE      "local" (default; 10 VUs / 30s) or "ramp" (10->50->100, blast-safe)

import http from 'k6/http';
import { check } from 'k6';
import { Rate } from 'k6/metrics';
import { textSummary } from 'https://jslib.k6.io/k6-summary/0.0.1/index.js';

// The deployed API enforces a HARDCODED global rate limiter: a fixed window of
// 100 requests / minute, partitioned by the TCP remote IP (Program.cs). The app
// does NOT call UseForwardedHeaders, so behind the NGINX ingress that remote IP
// is the ingress-controller pod — i.e. ALL clients share ONE 100/min bucket.
//
// Above that ceiling the API replies 429. A 429 is a deliberate, cheap rejection
// that does NOT stress the shared cluster, so we mark it "expected" to keep it
// out of http_req_failed — otherwise the cluster-protective abortOnFail guard
// would trip on harmless throttling rather than on real 5xx/timeout stress.
// We track the throttle separately so the SLA verdict can account for it.
http.setResponseCallback(http.expectedStatuses({ min: 200, max: 399 }, 429));
export const throttled = new Rate('throttled_429');

export function recordThrottle(res) {
  throttled.add(res.status === 429);
  return res;
}

export const BASE_URL = (__ENV.BASE_URL || 'http://127.0.0.1:61299').replace(/\/$/, '');
export const HOST_HEADER = __ENV.HOST_HEADER || '';
export const API_KEY = __ENV.API_KEY || 'test-api-key-development-only';
export const TENANT_ID = __ENV.TENANT_ID || '00000000-0000-0000-0000-000000000001';
export const CITIZEN_EMAIL = __ENV.CITIZEN_EMAIL || 'citizen@example.com';
export const CITIZEN_PASSWORD = __ENV.CITIZEN_PASSWORD || 'Test123!@#';
export const ISSUER_ID = __ENV.ISSUER_ID || '';
export const PROFILE = __ENV.PROFILE || 'local';

// GraphQL lives at the API root, NOT under /api/v1.
export const GRAPHQL_URL = `${BASE_URL}/graphql`;

// SLA thresholds (shared by every scenario):
//   p95 < 500 ms (the SLA), error rate < 1%.
// Blast-radius guards (AKS shared cluster): abort the run if the error rate
// exceeds 5% or p95 stays above 3 s — protecting the other products on the
// cluster from a misbehaving load test.
export const SLA_THRESHOLDS = {
  http_req_duration: [
    'p(95)<500',
    { threshold: 'p(95)<3000', abortOnFail: true, delayAbortEval: '30s' },
  ],
  http_req_failed: [
    'rate<0.01',
    { threshold: 'rate<0.05', abortOnFail: true, delayAbortEval: '10s' },
  ],
  // Reporting-only (non-aborting): the share of requests rejected by the global
  // 100/min limiter. RED here at high VU is expected and is the headline finding.
  throttled_429: ['rate<0.01'],
};

// Ramp profiles. "ramp" stays under 3 minutes of active load per scenario
// (10 -> 50 -> 100 VUs) per the shared-cluster blast-radius rules.
const PROFILES = {
  local: {
    executor: 'ramping-vus',
    startVUs: 0,
    stages: [
      { duration: '5s', target: 10 },
      { duration: '30s', target: 10 },
      { duration: '5s', target: 0 },
    ],
    gracefulRampDown: '5s',
  },
  ramp: {
    executor: 'ramping-vus',
    startVUs: 0,
    stages: [
      { duration: '20s', target: 10 },
      { duration: '30s', target: 10 },
      { duration: '20s', target: 50 },
      { duration: '30s', target: 50 },
      { duration: '20s', target: 100 },
      { duration: '40s', target: 100 },
      { duration: '10s', target: 0 },
    ],
    gracefulRampDown: '10s',
  },
  // "edge": a bounded 50-VU run (<= 2 min) for one-shot checks through the live
  // Front Door edge (HTTPS, no Host override). Shared FD — keep it short.
  edge: {
    executor: 'ramping-vus',
    startVUs: 0,
    stages: [
      { duration: '20s', target: 50 },
      { duration: '80s', target: 50 },
      { duration: '10s', target: 0 },
    ],
    gracefulRampDown: '10s',
  },
  // "steady": a fixed low arrival rate that stays UNDER the global 100/min cap
  // (90/min) so we can measure TRUE app latency (p50/p95/p99) without the limiter
  // distorting the numbers. Answers "does the app itself meet p95<500ms?".
  steady: {
    executor: 'constant-arrival-rate',
    // 40 iterations/min. Scenarios issue up to ~2 requests/iteration, so total
    // request rate stays under ~80/min — beneath the global 100/min limiter, so
    // no throttling/queueing distorts the measured app latency.
    rate: 40,
    timeUnit: '60s',
    duration: '60s',
    preAllocatedVUs: 5,
    maxVUs: 10,
  },
};

// Percentiles surfaced in the summary (pass via --summary-trend-stats too).
export const TREND_STATS = ['avg', 'min', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'];

// Approx active duration of each profile (used to size the rate-limited write
// scenario so it co-runs with the read ramp).
const PROFILE_DURATION = { local: '40s', ramp: '170s' };

export function scenario(name, exec) {
  const p = PROFILES[PROFILE] || PROFILES.local;
  const cfg = Object.assign({ tags: { scenario: name } }, p);
  if (exec) cfg.exec = exec;
  return { [name]: cfg };
}

// A rate-limited writer (default 1 op/s across ALL VUs) that co-runs with a
// read ramp. Used for the credential-ops issue write-mix so writes stay <= 1/s.
export function rateScenario(name, exec, ratePerSec) {
  return {
    [name]: {
      executor: 'constant-arrival-rate',
      rate: ratePerSec || 1,
      timeUnit: '1s',
      duration: PROFILE_DURATION[PROFILE] || PROFILE_DURATION.local,
      preAllocatedVUs: 3,
      maxVUs: 5,
      exec: exec,
      tags: { scenario: name },
    },
  };
}

// Base headers including the Host override when hitting the ingress IP directly.
export function baseHeaders(extra) {
  const h = Object.assign({ 'Content-Type': 'application/json' }, extra || {});
  if (HOST_HEADER) h['Host'] = HOST_HEADER;
  return h;
}

export function apiKeyHeaders(extra) {
  return baseHeaders(Object.assign({ 'X-API-Key': API_KEY, 'X-Tenant-Id': TENANT_ID }, extra || {}));
}

export function bearerHeaders(token, extra) {
  return baseHeaders(Object.assign({ Authorization: `Bearer ${token}` }, extra || {}));
}

// Citizen login. Called from setup() so VUs reuse one token rather than
// hammering the login endpoint on the hot path.
export function login(email, password) {
  const res = http.post(
    `${BASE_URL}/api/v1/Authentication/login`,
    JSON.stringify({ email: email || CITIZEN_EMAIL, password: password || CITIZEN_PASSWORD }),
    { headers: baseHeaders(), tags: { name: 'login' } },
  );
  const ok = check(res, { 'login 200': (r) => r.status === 200 });
  if (!ok) {
    throw new Error(`login failed: status=${res.status} body=${String(res.body).slice(0, 200)}`);
  }
  return JSON.parse(res.body).accessToken;
}

// POST a GraphQL operation. `name` tags the request for per-operation metrics.
export function graphql(headers, query, variables, name) {
  return http.post(
    GRAPHQL_URL,
    JSON.stringify({ query, variables: variables || {} }),
    { headers, tags: { name: name || 'graphql' } },
  );
}

// A GraphQL response is only "ok" if it is HTTP 200 AND carries no errors[].
export function gqlOk(res) {
  if (res.status !== 200) return false;
  try {
    const b = JSON.parse(res.body);
    return !b.errors && b.data != null;
  } catch (_e) {
    return false;
  }
}

// Write both a console summary and a JSON artefact under perf/results/.
export function makeSummary(tag) {
  return function handleSummary(data) {
    const out = {};
    out[`perf/results/${tag}-${__ENV.PROFILE || 'local'}.json`] = JSON.stringify(data, null, 2);
    out['stdout'] = textSummary(data, { indent: ' ', enableColors: true });
    return out;
  };
}
