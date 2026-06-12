// health.js — baseline performance floor.
//
// Hits GET /health (no auth, no DB) to establish the lowest-latency path the
// platform can serve. Any latency the other scenarios show ABOVE this floor is
// attributable to auth + DB + business logic rather than network/ingress.
//
//   k6 run -e BASE_URL=... -e HOST_HEADER=... -e PROFILE=ramp perf/health.js

import http from 'k6/http';
import { check } from 'k6';
import { BASE_URL, baseHeaders, scenario, SLA_THRESHOLDS, makeSummary, recordThrottle } from './lib/common.js';

export const options = {
  scenarios: scenario('health'),
  thresholds: SLA_THRESHOLDS,
};

export default function () {
  const res = http.get(`${BASE_URL}/health`, { headers: baseHeaders(), tags: { name: 'health' } });
  recordThrottle(res);
  check(res, { 'health 200': (r) => r.status === 200 });
}

export const handleSummary = makeSummary('health');
