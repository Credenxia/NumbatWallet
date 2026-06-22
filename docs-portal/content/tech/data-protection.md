---
tags:
  - tech
  - support
---

# Data Protection

> **Audience:** Tech (and Support). Source: `docs/ARCHITECTURE-CURRENT.md §4`,
> `docs/OPERATIONS.md §3`.

## Field encryption (PII at rest)

- **`AesGcmFieldEncryptor`** — AES-256-GCM, token format `FE1:base64(nonce|tag|cipher)`,
  with plaintext passthrough on decrypt for legacy rows.
- Wired through `ProtectedFieldConverter` into `PersonConfiguration` for
  **FirstName / LastName / DateOfBirth / Email / Phone**.
- Key source: config (`FieldEncryption:Key`, dev) or Key Vault secret
  `field-encryption-key` (`FieldEncryption:Source=KeyVault` + `FieldEncryption:KeyVaultUri`).
- API responses return **plaintext** (decrypted in-process); DB rows contain **no
  plaintext** for the protected fields (verified by inspection).

## Searchability (deterministic HMAC tokens)

Encrypting a field would normally make it unsearchable — but login looks up persons by
email. The design solves this with **deterministic HMAC search tokens**:

- Shadow columns `email_search_token` / `phone_search_token`, computed by
  `SearchTokenInterceptor` on save.
- A deployment-wide **pepper** (`Search:TokenPepper` config or KV secret
  `search-token-pepper`).
- **All** email/phone lookups — including **login** — go through the tokens.

!!! danger "Pepper and key are load-bearing"
    - **Rotating / losing the pepper** invalidates every stored token ⇒ email/phone lookup
      **and login break** until rows are re-tokenised. Treat the pepper as immutable per
      deployment.
    - **Changing the field key** makes existing `FE1:` ciphertext unreadable (typed
      converters then throw ⇒ 500s). There is no re-encryption job yet; rotation needs a
      decrypt-old/encrypt-new migration. In dev, reset the postgres volume.

## JWT signing

| Signer | Algorithm | Key | When |
|---|---|---|---|
| `HmacAccessTokenSigner` | HS256 | `Jwt:SecretKey` (throws if unset) | default, dev/test |
| `KeyVaultRsaAccessTokenSigner` | RS256 | Key Vault PEM, kid `nw-jwt-rs256` | required outside Development/Testing |

`AccessTokenSignerSelector` fails fast if RS256 is required but unconfigured, and rejects
explicit HMAC outside dev/test. Production hardening: move from a retrievable KV key to
KV-side / HSM signing.

## Transport & secrets

- TLS terminated at the edge (Azure Front Door) and the ingress.
- Secrets flow Key Vault → k8s secret `numbatwallet-secrets` (`bootstrap-namespace.sh`):
  connection string, `jwt-secret-key`, `field-encryption-key`, `admin-api-key`.
- Output cache **never** caches authenticated responses; the rate limiter is proxy-aware.

See the [Support runbook](../support/runbook.md) for the rotation procedures and the
environment-variable contract.
