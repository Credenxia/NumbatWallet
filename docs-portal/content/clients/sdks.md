---
tags:
  - clients
  - account-management
---

# SDK Quickstarts

> **Audience:** Clients (and Account Management). Source: the three SDK READMEs in the
> `NumbatWallet-sdks` repository — those are authoritative for each SDK's verified surface.

All three SDKs' **wallet, credential and presentation** operations are aligned to the real
backend and verified end-to-end against a running backend. Distribution is via a
**private feed for now** (public later).

| SDK | Package | Transport | Auth |
|---|---|---|---|
| .NET | `NumbatWallet.Sdk` (net10.0) | GraphQL | API key |
| TypeScript | `@numbatwallet/sdk` | REST (+ GraphQL for presentations) | API key |
| Flutter | `numbatwallet_sdk` | REST (+ GraphQL for presentations) | API key **or Bearer** |

=== ".NET (C#)"

    ```csharp
    using Microsoft.Extensions.DependencyInjection;
    using NumbatWallet.Sdk.Client;
    using NumbatWallet.Sdk.Extensions;

    var services = new ServiceCollection();
    services.AddNumbatWalletSdk(options =>
    {
        options.BaseUrl  = "https://tst.numbatwallet.credentry.com.au";
        options.ApiKey   = "<api-key>";        // X-API-Key
        options.TenantId = "<tenant-guid>";    // X-Tenant-Id
    });

    var client = services.BuildServiceProvider().GetRequiredService<IWalletClient>();

    var wallets = await client.Wallets.ListAsync();          // myWallets (plain array)
    var wallet  = await client.Wallets.CreateAsync(new CreateWalletInput
    {
        PersonId = "<person-guid>",  // required
        Name     = "My Wallet",      // required
    });
    ```

    - **GraphQL transport** to `{BaseUrl}/graphql`. Polly retry on the GraphQL handler
      (transient + 429).
    - Enum values are converted PascalCase → SCREAMING_SNAKE on the wire for you; the
      dictionary-pair quirk is mapped back to dictionaries.
    - `WalletException` subclasses carry backend error codes as strings (e.g.
      `WALLET_NOT_FOUND`); transport failures use the `ErrorCode` enum.
    - **API-key only — no Bearer mode.**

=== "TypeScript"

    ```typescript
    import { WalletClient } from '@numbatwallet/sdk';

    const client = new WalletClient({
      baseUrl: 'https://tst.numbatwallet.credentry.com.au',
      apiKey: '<api-key>',
      tenantId: '<tenant-guid>',
    });

    const created = await client.wallets.create({
      personId: '<person-guid>', type: 'HOLDER', name: 'My Wallet',
    });

    const issued = await client.credentials.issue({
      walletId: '<wallet-guid>',
      issuerId: '<issuer-organisation-guid>',
      credentialType: 'ProofOfAge',
      subject: '<person-guid>',
      claims: { age: '21+' },
    });
    ```

    - **REST** for wallets/credentials; **GraphQL** (`POST /graphql`, same headers) for
      presentations.
    - All operations return `Result<T>` — check `result.success` rather than catching.
    - **API-key only — no Bearer mode.**

=== "Flutter (Dart)"

    ```dart
    import 'package:numbatwallet_sdk/numbatwallet_sdk.dart';

    // Service-to-service (API key)
    final client = WalletClient(WalletClientOptions(
      baseUrl: 'https://tst.numbatwallet.credentry.com.au',
      apiKey: '<api-key>',
      tenantId: '<tenant-guid>',
    ));

    // Citizen (Bearer) — token from POST /api/v1/Authentication/login
    final citizenClient = WalletClient(WalletClientOptions(
      baseUrl: 'https://tst.numbatwallet.credentry.com.au',
      tokenProvider: () async => await authSession.currentAccessToken(),
    ));

    final wallets = await client.wallets.list();
    ```

    - **Only the Flutter SDK supports Bearer (citizen) auth.** Exactly one mode (API key
      XOR Bearer) is enforced at construction.
    - REST + GraphQL (presentations) both use the chosen auth headers; Bearer mode strips
      the API-key header.
    - Citizen access is scoped automatically (own wallet only; foreign ids → not-found).

## Presentations (all SDKs)

Present a credential disclosing only chosen claims, then verify the token. An **invalid
token is a success result with `isValid == false`** (it does not throw):

=== ".NET"

    ```csharp
    var presented = await client.Presentations.CreateAsync(/* credentialId, verifierId,
        purpose, selectiveClaims, nonce? */);
    var verified  = await client.Presentations.VerifyAsync(presented.PresentationToken);
    ```

=== "TypeScript"

    ```typescript
    const presented = await client.presentations.present({
      credentialId: '<credential-guid>', verifierId: 'my-verifier',
      purpose: 'age check', selectiveClaims: ['age'],
    });
    const verified = await client.presentations.verify(presented.data.presentationToken);
    ```

=== "Flutter"

    ```dart
    final presented = await client.presentations.present(PresentCredentialInput(
      credentialId: '<credential-guid>', verifierId: 'my-verifier',
      purpose: 'age check', selectiveClaims: ['age'],
    ));
    final verified = await client.presentations.verify(presented.value.presentationToken);
    ```

## Honest coverage notes (read before scoping a pilot)

- **Wallets, credentials, presentations** — contract-verified in all three SDKs.
- **Persons / Consent / Passes** — **not contract-validated** in any SDK; some endpoints
  may not exist backend-side. Experimental.
- **Bearer (citizen) auth** — **Flutter only**.
- **Presentations** — GraphQL-only on the backend (the TS/Flutter SDKs route through a
  small GraphQL call on the same headers).
- **mTLS / OAuth / offline-mode** — option properties exist but are **not verified paths**.

## Live contract tests

Each SDK ships an env-gated live-contract suite (skips cleanly when the env var is unset):

```bash
# .NET
NUMBATWALLET_API_URL=... NUMBATWALLET_API_KEY=... NUMBATWALLET_TENANT_ID=... \
  dotnet test numbatwallet-dotnet-sdk/tests/NumbatWallet.Sdk.IntegrationTests

# TypeScript
cd numbatwallet-typescript-sdk && NUMBATWALLET_API_URL=... npm run test:contract

# Flutter
cd numbatwallet-flutter-sdk && NUMBATWALLET_API_URL=... flutter test test/live_contract_test.dart
```
