// credential-ops.js — admin/service credential operations (read-heavy).
//
// Auth: admin/service API key (X-API-Key + X-Tenant-Id).
//
// setup() bootstraps a deterministic fixture via the API (mirrors the SDK
// contract-test pattern):
//   1. GraphQL createPerson           -> personId
//   2. REST   POST /api/v1/wallets    -> walletId
//   3. if ISSUER_ID is set: GraphQL issueCredential -> credentialId (seed read data)
//   4. read back credentialsByWallet  -> first credentialId (covers pre-seeded data)
//
// Hot path (90% reads), split into two co-running scenarios:
//   credops_read  : ramping-vus 10->50->100, GraphQL credentialsByWallet +
//                   credential(id) reads (the verifier/admin read hot path).
//   credops_write : constant-arrival-rate 1/s, GraphQL issueCredential — the
//                   light write mix, capped at <= 1 op/s TOTAL. Only enabled
//                   when ISSUER_ID is provided (no issuer-list API exists to
//                   discover one automatically — see perf/README.md).
//
//   k6 run -e BASE_URL=... -e HOST_HEADER=... -e API_KEY=... -e TENANT_ID=... \
//          -e ISSUER_ID=<guid> -e PROFILE=ramp perf/credential-ops.js

import http from 'k6/http';
import { check } from 'k6';
import {
  BASE_URL, apiKeyHeaders, graphql, gqlOk, scenario, rateScenario,
  ISSUER_ID, SLA_THRESHOLDS, makeSummary, recordThrottle,
} from './lib/common.js';

const readScenarios = scenario('credops_read', 'reads');
const writeScenarios = ISSUER_ID ? rateScenario('credops_write', 'writes', 1) : {};

export const options = {
  scenarios: Object.assign({}, readScenarios, writeScenarios),
  thresholds: SLA_THRESHOLDS,
};

const CREDS_BY_WALLET =
  'query($w: UUID!){ credentialsByWallet(walletId: $w){ nodes { id type status issuerId } } }';
const CRED_BY_ID = 'query($id: String!){ credential(id: $id){ id type status } }';
const CREATE_PERSON =
  'mutation($i: CreatePersonInput!){ createPerson(input: $i){ id email } }';
const ISSUE =
  'mutation($i: IssueCredentialInput!){ issueCredential(input: $i){ id type status } }';

export function setup() {
  const h = apiKeyHeaders();
  const stamp = Date.now();

  // 1. create person
  const pRes = graphql(h, CREATE_PERSON, {
    i: {
      firstName: 'Perf', lastName: 'Test',
      email: `perf-${stamp}-${Math.floor(Math.random() * 1e6)}@example.com`,
      dateOfBirth: '1990-01-01T00:00:00Z',
    },
  }, 'createPerson');
  check(pRes, { 'setup createPerson ok': () => gqlOk(pRes) });
  if (!gqlOk(pRes)) {
    throw new Error(`createPerson failed: ${String(pRes.body).slice(0, 300)}`);
  }
  const personId = JSON.parse(pRes.body).data.createPerson.id;

  // 2. create wallet (REST)
  const wRes = http.post(
    `${BASE_URL}/api/v1/wallets`,
    JSON.stringify({ personId, type: 'HOLDER', name: 'Perf Wallet' }),
    { headers: h, tags: { name: 'createWallet' } },
  );
  check(wRes, { 'setup createWallet 201': (r) => r.status === 201 });
  const walletId = JSON.parse(wRes.body).walletId;

  // 3. optional: seed a credential so reads exercise real rows
  let credentialId = null;
  if (ISSUER_ID) {
    const iRes = graphql(h, ISSUE, {
      i: {
        walletId, credentialType: 'PROOF_OF_AGE', subject: 'Perf Test',
        claimsJson: JSON.stringify({ age: '21', name: 'Perf Test' }),
        issuerOrganizationId: ISSUER_ID,
      },
    }, 'seedIssue');
    if (gqlOk(iRes)) credentialId = JSON.parse(iRes.body).data.issueCredential.id;
  }

  // 4. fall back to any pre-existing credential on the wallet
  if (!credentialId) {
    const cRes = graphql(h, CREDS_BY_WALLET, { w: walletId }, 'setupCredsByWallet');
    if (gqlOk(cRes)) {
      const nodes = JSON.parse(cRes.body).data.credentialsByWallet.nodes;
      if (nodes && nodes.length) credentialId = nodes[0].id;
    }
  }

  return { walletId, credentialId };
}

// 90% read mix.
export function reads(data) {
  const h = apiKeyHeaders();

  const byWallet = graphql(h, CREDS_BY_WALLET, { w: data.walletId }, 'credentialsByWallet');
  recordThrottle(byWallet);
  check(byWallet, { 'credentialsByWallet ok': () => gqlOk(byWallet) });

  if (data.credentialId) {
    const byId = graphql(h, CRED_BY_ID, { id: data.credentialId }, 'credentialById');
    recordThrottle(byId);
    check(byId, { 'credential(id) ok': () => gqlOk(byId) });
  }
}

// <= 1/s write mix.
export function writes(data) {
  const h = apiKeyHeaders();
  const res = graphql(h, ISSUE, {
    i: {
      walletId: data.walletId, credentialType: 'PROOF_OF_AGE', subject: 'Perf Write',
      claimsJson: JSON.stringify({ age: '21' }),
      issuerOrganizationId: ISSUER_ID,
    },
  }, 'issueCredential');
  recordThrottle(res);
  check(res, { 'issueCredential ok': () => gqlOk(res) });
}

export const handleSummary = makeSummary('credential-ops');
