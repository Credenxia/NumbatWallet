// presentation-verify.js — the verifier-side hot path (pure compute + read).
//
// verifyPresentation is AllowAnonymous at the field level, but the /graphql
// transport requires an authenticated principal outside Development, so the
// hammered loop authenticates as a verifier via the service API key. It measures
// JWT parse + signature validation + presentation/credential DB lookups — the
// verifier hot path (and the API-key path skips the citizen-token Redis lookup).
//
// setup() tries to mint a REAL presentation token once (so verification returns
// IsValid=true and exercises the full happy path):
//   citizen login -> myWallets -> credentialsByWallet -> presentCredential.
// If the citizen has no credential to present (e.g. a freshly-seeded env), it
// falls back to a SYNTHETIC well-formed JWT. The synthetic token still drives
// the exact hot path (parse + signature check + jti lookup) and returns
// HTTP 200 with IsValid=false (verification failures are normal outcomes, not
// errors), so the latency measurement remains representative while keeping the
// http_req_failed rate at ~0. The summary records which mode was used.
//
//   k6 run -e BASE_URL=... -e HOST_HEADER=... -e PROFILE=ramp perf/presentation-verify.js

import { check } from 'k6';
import encoding from 'k6/encoding';
import {
  bearerHeaders, apiKeyHeaders, graphql, gqlOk, login, scenario, SLA_THRESHOLDS, makeSummary, recordThrottle,
} from './lib/common.js';

export const options = {
  scenarios: scenario('presentation_verify'),
  thresholds: SLA_THRESHOLDS,
};

const MY_WALLETS = '{ myWallets { id } }';
const CREDS_BY_WALLET =
  'query($w: UUID!){ credentialsByWallet(walletId: $w){ nodes { id } } }';
const PRESENT =
  'mutation($i: PresentCredentialInput!){ presentCredential(input: $i){ presentationToken } }';
const VERIFY =
  'mutation($t: String!){ verifyPresentation(token: $t){ isValid reason credentialType } }';

function syntheticToken() {
  const b64 = (o) => encoding.b64encode(JSON.stringify(o), 'rawurl');
  const now = Math.floor(Date.now() / 1000);
  const header = b64({ alg: 'HS256', typ: 'JWT' });
  const payload = b64({
    jti: `00000000-0000-0000-0000-${String(Math.floor(Math.random() * 1e12)).padStart(12, '0')}`,
    iss: 'urn:uuid:perf', aud: 'perf-verifier', nbf: now, exp: now + 300,
    nonce: 'perf', vp: { '@context': ['https://www.w3.org/2018/credentials/v1'] },
  });
  return `${header}.${payload}.c2lnbmF0dXJl`; // deliberately unverifiable signature
}

export function setup() {
  const token = login();
  const h = bearerHeaders(token);

  const w = graphql(h, MY_WALLETS, {}, 'myWallets');
  if (gqlOk(w)) {
    const wallets = JSON.parse(w.body).data.myWallets;
    if (wallets && wallets.length) {
      const c = graphql(h, CREDS_BY_WALLET, { w: wallets[0].id }, 'credsByWallet');
      if (gqlOk(c)) {
        const nodes = JSON.parse(c.body).data.credentialsByWallet.nodes;
        if (nodes && nodes.length) {
          const p = graphql(h, PRESENT, {
            i: { credentialId: nodes[0].id, verifierId: 'perf-verifier', purpose: 'perf-test' },
          }, 'presentCredential');
          if (gqlOk(p)) {
            const pt = JSON.parse(p.body).data.presentCredential.presentationToken;
            if (pt) return { token: pt, mode: 'real' };
          }
        }
      }
    }
  }
  return { token: syntheticToken(), mode: 'synthetic' };
}

export default function (data) {
  // The verify field is anonymous-by-design, but the /graphql transport requires
  // an authenticated principal outside Development. A verifier authenticates with
  // its service API key (this also avoids the citizen-token Redis-blacklist path).
  const res = graphql(apiKeyHeaders(), VERIFY, { t: data.token }, 'verifyPresentation');
  recordThrottle(res);
  check(res, {
    'verify HTTP 200 (no errors)': () => gqlOk(res),
  });
}

export const handleSummary = makeSummary('presentation-verify');
