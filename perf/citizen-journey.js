// citizen-journey.js — realistic citizen read journey.
//
// setup() logs the citizen in ONCE and shares the token with every VU, so the
// login endpoint is not hammered on the hot path (login is a deliberately
// expensive password+JWT operation and is not part of the steady-state SLA mix).
//
// Per iteration each VU exercises the two citizen read hot paths:
//   1. GraphQL  myWallets        (auth + tenant filter + DB read, projected)
//   2. REST     GET /api/v1/wallets   (auth + DB read)
//
//   k6 run -e BASE_URL=... -e HOST_HEADER=... -e PROFILE=ramp perf/citizen-journey.js

import http from 'k6/http';
import { check, sleep } from 'k6';
import {
  BASE_URL, bearerHeaders, graphql, gqlOk, login, scenario, SLA_THRESHOLDS, makeSummary, recordThrottle,
} from './lib/common.js';

export const options = {
  scenarios: scenario('citizen_journey'),
  thresholds: SLA_THRESHOLDS,
};

const MY_WALLETS = '{ myWallets { id name status personId } }';

export function setup() {
  const token = login();
  // Confirm the journey is viable before loading it.
  const res = graphql(bearerHeaders(token), MY_WALLETS, {}, 'myWallets');
  check(res, { 'setup myWallets ok': () => gqlOk(res) });
  return { token };
}

export default function (data) {
  const h = bearerHeaders(data.token);

  const gql = graphql(h, MY_WALLETS, {}, 'myWallets');
  recordThrottle(gql);
  check(gql, { 'myWallets ok': () => gqlOk(gql) });

  const rest = http.get(`${BASE_URL}/api/v1/wallets`, { headers: h, tags: { name: 'rest_wallets' } });
  recordThrottle(rest);
  check(rest, { 'GET /wallets 200': (r) => r.status === 200 });

  sleep(0.5); // light think-time between citizen actions
}

export const handleSummary = makeSummary('citizen-journey');
