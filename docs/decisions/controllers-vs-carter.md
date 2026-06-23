# Decision: MVC Controllers vs Carter (minimal-API) modules

Status: Assessed 2026-06-23. Outcome: **partial consolidation** — removed the one
unambiguously-dead helper; documented the remaining (intentional, pending-design)
overlaps rather than forcing risky deletions.

## Context

The Web.Api project historically grew two parallel REST surfaces:

- **MVC controllers** under `Controllers/` (the canonical, mapped surface).
- **Carter minimal-API modules** under `Endpoints/` and `Rest/Modules/`.

The duplicate **Wallet / Credential / BulkOperation** Carter modules were *already
deleted* in an earlier change. Carter is registered in `Program.cs` with an explicit
allow-list:

```csharp
builder.Services.AddCarter(configurator: c => c.WithModule<PersonEndpoints>());
```

`WithModule<…>` means **only `PersonEndpoints` is registered and mapped** — every other
Carter module file is inert (not registered, not routed).

## Current state (as assessed)

| Carter module | Route prefix | Registered/Mapped? | Controller overlap | Notes |
|---|---|---|---|---|
| `Endpoints/PersonEndpoints` | `/api/v1/persons` | **YES** (`WithModule`) | none (no `PersonController`) | Canonical person surface. **Keep.** |
| `Endpoints/WalletPassEndpoints` | `/api/v1/passes` | no | `WalletGenerationController` (`/api/v1/wallet-generation`) | **Not an identical duplicate.** Uses the CQRS `GenerateApple/GooglePassCommand` handlers; the controller uses `IAppleWalletBuilder`/`IGoogleWalletBuilder` directly with template ids. `Program.cs` comment: "remains unmapped pending design." |
| `Rest/Modules/DtpModule` | `/api/v1/dtp` | no | `WalletController` / `CredentialController` | 307 lines of real handler logic over a distinct **DTP** API contract (`/dtp/wallets`, `/dtp/credentials/{verify,issue}`). Looks like an intended external-integration surface, not a stray copy. |
| `Rest/Modules/WebhookModule` | `/webhooks` | no | `WebhookController` (`/api/v1/webhook`) | Different path + payload shape (`/servicewa/callback`, `/issuer/notification`) vs the controller (`subscribe`/`test`/`validate`). |
| `Rest/Modules/LegacyModule` | `/api/legacy` | no | none | Catch-all 301 redirector for legacy paths. |
| `Rest/Modules/HealthModule` | `/health`, `/ready`, `/live` | no | `HealthController` + `HealthCheckExtensions` | Overlaps the mapped health endpoints. |
| `Extensions/CarterExtensions` | n/a | **never called** | n/a | `AddCarterWithValidation` and `MapCarterEndpoints` are not referenced anywhere (Program.cs calls `AddCarter`/`MapCarter` directly). **Dead.** |

No test in the solution references any of the unmapped modules or `CarterExtensions`
(verified by grep across `src/Tests`).

## Decision

1. **Removed now (safe):** `Extensions/CarterExtensions.cs`. Both of its methods
   (`AddCarterWithValidation`, `MapCarterEndpoints`) are dead — never called, no tests,
   and its own comment states `MapCarter()` is invoked directly in `Program.cs`. Deleting
   it has zero behavioural effect and removes a misleading second `AddCarter()` path that
   (had it ever been wired) would have scanned and mapped ALL the inert modules, silently
   re-activating duplicate routes.

2. **Documented, NOT removed (ambiguous / pending design):** `WalletPassEndpoints`,
   `DtpModule`, `WebhookModule`, `LegacyModule`, `HealthModule`. These are unmapped today
   so they cause no runtime duplication, but they contain real, *distinct* implementations
   (different routes, payloads, or code paths) rather than copies of a controller. The
   `passes` module is explicitly flagged "pending design." Deleting them would risk
   discarding intended work, so per the honesty-over-churn principle they are left in place.

## Recommendation (future work)

Pick ONE surface per capability and delete the other:

- **Passes:** decide between the CQRS-handler path (`WalletPassEndpoints`) and the
  builder-direct path (`WalletGenerationController`). The handler path is the cleaner
  Clean-Architecture fit; if adopted, map `WalletPassEndpoints` via `WithModule<…>` and
  retire the pass-generation actions on `WalletGenerationController` (keep `preview`/web).
- **DTP:** confirm whether `/api/v1/dtp` is a required external contract. If yes, map it
  and make it the single wallet/credential read+verify+issue surface for that consumer; if
  no, delete `DtpModule` + its validators (`DtpVerifyRequest`/`DtpIssueRequest`/`DtpRevokeRequest`).
- **Webhooks / Health / Legacy:** keep the controller/HealthCheck surfaces; delete the
  corresponding inert Carter modules once confirmed no consumer expects the alternate paths.

Each retirement should be its own commit with a route-map diff and a smoke check.
