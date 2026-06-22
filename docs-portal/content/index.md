---
tags:
  - directors
  - account-management
  - support
  - tech
  - clients
---

# NumbatWallet Documentation Portal

**NumbatWallet** is a digital wallet and verifiable-credentials platform (originating
from WA Department of the Premier & Cabinet tender DPC2142, continuing as a Credenxia
platform product). It issues, holds, presents and verifies digital credentials — W3C
Verifiable Credentials, JWT-VC/VP and a minimal OID4VP subset — with multi-tenant
isolation, PII encrypted at rest, and client SDKs for .NET, TypeScript and Flutter.

!!! info "Current state (June 2026)"
    All planned build pillars are **done**: full credential lifecycle, W3C VP-JWT
    presentations + a minimal OID4VP subset, three live-contract-verified SDKs, citizen
    Bearer auth + Credentry SSO federation, PII encrypted with searchable tokens, deployed
    to shared nonprod AKS, and a performance pass where both SLA defects were closed.
    Unit suites **835 / 0**; integration **84 / 0 / 2** (2 documented skips).
    Live nonprod URL: **<https://tst.numbatwallet.credentry.com.au>**.

    This portal is written to be **honest** — stubs, experimental features and pending
    operator steps are flagged throughout. Where it says "aligned" it does not mean
    "certified".

## Choose your view

This is a single portal with five audience lenses. Pick the one that matches you:

<div class="grid cards" markdown>

-   :material-account-tie: **[Directors](directors/index.md)**

    Where the project stands, readiness, SLA verdict, compliance posture, and the honest
    risk / pending-items register. Executive, non-technical.

-   :material-handshake: **[Account Management](account-management/index.md)**

    Capability sheets and SDK quickstarts to hand to clients — what is pilot-ready today
    versus on the roadmap.

-   :material-lifebuoy: **[Support](support/index.md)**

    Runbooks, environments & URLs, known issues, and the open operator follow-ups
    (AKS pepper/reseed, `Jwt:Signer`, secrets).

-   :material-code-braces: **[Tech](tech/index.md)**

    As-built architecture, the REST + GraphQL API surface (including presentations /
    OID4VP), authentication schemes, data protection, and deployment / CI.

-   :material-cellphone-link: **[Clients](clients/index.md)**

    Integration guides, the three SDK quickstarts, the auth model, the public test URL,
    and honest per-SDK coverage notes.

</div>

## Source documents

Every claim in this portal traces back to the repository's as-built documents — this
portal summarises and links them rather than duplicating them:

- `README.md` — project overview and verified capabilities
- `docs/ARCHITECTURE-CURRENT.md` — as-built architecture
- `docs/OPERATIONS.md` — operator guide (environment contract, runbook)
- `perf/RESULTS-2026-06-12.md` — SLA baseline, defects, and post-fix results
- The three SDK READMEs (separate `NumbatWallet-sdks` repository)

## How this portal is built

Static site generated with **MkDocs Material**. To preview or build it, see the
[portal README](https://github.com/Credenxia/NumbatWallet/blob/main/docs-portal/README.md)
(`mkdocs serve` / `mkdocs build --strict`).
