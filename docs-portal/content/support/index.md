---
tags:
  - support
---

# Support

> **Audience:** Internal Support / operators. Source: `docs/OPERATIONS.md`,
> `perf/RESULTS-2026-06-12.md`, `README.md`.

Three pages:

- **[Environments & URLs](environments.md)** — where things run (local, AKS nonprod),
  URLs, tenants, and dev logins.
- **[Runbook](runbook.md)** — how to run/seed/login, the environment-variable contract,
  rotation implications, and the open operator follow-ups.
- **[Known Issues](known-issues.md)** — the seeded-login backfill, capacity sizing, and
  the admin stubs.

!!! danger "Three things that break login if you get them wrong"
    1. **Search-token pepper** — if it changes or is random per pod, email/phone lookup
       **and login** break until rows are re-tokenised.
    2. **Field-encryption key** — changing/losing it makes existing PII ciphertext
       unreadable (typed converters then throw 500s).
    3. **Tenant id** — the AKS Testing env seeds tenant `00000000-0000-0000-0000-000000000000`;
       local dev uses `…0001`. Using the wrong one makes lookups return nothing.
