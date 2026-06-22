---
tags:
  - tech
---

# Tech

> **Audience:** Internal IT / engineers. Source: `docs/ARCHITECTURE-CURRENT.md`,
> `docs/OPERATIONS.md`, `README.md`, `perf/RESULTS-2026-06-12.md`.

The as-built technical reference. Five pages:

- **[Architecture (As-Built)](architecture.md)** — layers, CQRS, GraphQL rules.
- **[Authentication Schemes](auth-schemes.md)** — API-key / citizen-Bearer / Credentry
  SSO / CredentrySelector, and JWT signing.
- **[API Surface](api-surface.md)** — REST routes and GraphQL operations, including
  presentations / OID4VP and the admin surface.
- **[Data Protection](data-protection.md)** — AES-GCM field encryption, searchable HMAC
  tokens, signing.
- **[Deployment & CI](deployment.md)** — topology, Helm, migrations, the CI workflow.

## Stack at a glance

- **.NET 10 / C# 14**, ASP.NET Core 10, EF Core 10, PostgreSQL 17.
- **Custom CQRS** (`ICommandHandler<,>` / `IQueryHandler<,>`) — **no MediatR**.
- **HotChocolate 15** GraphQL at `/graphql`; Carter for one REST module (`PersonEndpoints`).
- FluentValidation, Serilog, .NET Aspire (dev), Helm/AKS (deploy).

!!! warning "HotChocolate captive-dependency rule (learned the hard way)"
    `[ExtendObjectType]` GraphQL types are **singletons**. Scoped services (command/query
    handlers, repositories, `DbContext`) must be injected via **`[Service]` resolver
    parameters**, never the constructor — constructor injection captures a request-scoped
    `DbContext` and throws "disposed context" on reuse.
