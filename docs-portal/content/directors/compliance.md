---
tags:
  - directors
---

# Compliance Posture

> **Audience:** Directors. **Read the framing carefully** — this page states **alignment
> and intent**, not certification.

!!! warning "Claims, not certifications"
    NumbatWallet is **aligned to** the standards and frameworks below. It has **not** been
    independently certified or accredited against any of them. This is tender-adjacent
    material — do not represent alignment as certification.

## Standards & frameworks

| Framework | What we claim | What we do **not** claim |
|---|---|---|
| **TDIF** (Trusted Digital Identity Framework) | Architecture and data-handling are **TDIF-aligned** in intent. | Not TDIF-accredited / not certified. |
| **Australian Privacy Act** | Privacy-by-design posture: PII encrypted at rest, tenant isolation, minimal disclosure on presentation. | No formal privacy assessment / no APP compliance attestation. |
| **ISO 27001** | Design **aligned** to ISO 27001 controls (secrets in Key Vault, least-privilege, audit logging). | Not ISO 27001 certified. |
| **W3C Verifiable Credentials** | Credentials and presentations follow the **W3C VC Data Model v1.1 JWT mapping** (verified by decoding live tokens). | Not formally conformance-tested against the W3C test suite. |
| **OID4VP** | A **minimal experimental subset** (DIF Presentation Exchange v2 request + one-shot submission). | Not a full OID4VP implementation; SD-JWT and multi-credential VPs are out of scope. |
| **ISO 18013-5 mDL** | On the roadmap. | **Not implemented.** |

## What is genuinely standards-grade today

- **W3C VP-JWT presentations.** Presentation tokens are spec-conformant verifiable
  presentations (correct `@context`, `VerifiablePresentation` type, an embedded JWT-VC
  whose `credentialSubject` contains **only the disclosed claims**), with nonce/audience
  binding and algorithm pinning (no algorithm-confusion). Verification re-checks the
  embedded credential's revocation status, so a credential revoked **after** presentation
  fails verification.
- **PII encryption.** Person fields (name, date of birth, email, phone) are encrypted at
  rest with AES-256-GCM. Email/phone remain searchable via deterministic HMAC tokens, so
  no plaintext PII is stored for these fields while login still works.
- **Tenant isolation.** Tenant context is taken from the **validated token claim**;
  spoofed `X-Tenant-Id` headers are ignored; cross-tenant writes are blocked; defaults
  fail closed outside development.

## What is POA-grade (be precise)

- **OID4VP** — minimal subset, marked experimental.
- **Selective disclosure** — full-redisclosure model (only disclosed claims are embedded);
  **not SD-JWT**.
- **Admin governance surfaces** (backup, key rotation, reporting, feature flags) — in-memory
  POA stubs, not production controls.
- **JWT signing** — HS256 by default; RS256 via Azure Key Vault is available and **required
  outside development/testing**, but the key is retrievable from the vault today (a
  test-acceptable posture); production hardening is to move to non-exportable / HSM-side
  signing.

For the per-feature breakdown see the
[Account Management capabilities matrix](../account-management/capabilities.md).
