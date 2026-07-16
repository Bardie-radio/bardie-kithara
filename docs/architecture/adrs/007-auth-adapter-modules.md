# ADR 007: Auth Adapter Modules

**Status:** Accepted

## Context

Auth must be modular (login+password, OIDC, custom) without forcing self-hosted IdP. Options: baked into Kithara, separate auth-core container, or adapter modules.

## Decision

- **Auth orchestrator** lives **inside Kithara** (registry, discovery aggregation, token routing, service tokens).
- **Auth adapters** are separate repos/containers (login+password for MVP; OIDC in v0.2). Module and repo names are undecided.
- Multiple adapters attach simultaneously; discovery merges all providers.
- **Adapter-owned login UI** — Plume delegates via `uiMode` (form_schema, embed, redirect).
- Service tokens for bots validated in Kithara config (no adapter container).

## Consequences

- No separate auth-core container (rejected alternative).
- MVP: login+password adapter only; no external IdP required.
- Plume never hardcodes password fields.

## Alternatives considered

- **Separate auth gateway container** — rejected; 1:1 with Kithara makes core redundant.
- **Kithara validates Zitadel JWT directly only** — rejected; limits modularity.
- **Auth baked into Kithara** — rejected for adapter extensibility.

**Related:** [domains/auth-adapters.md](../domains/auth-adapters.md) · [interfaces/auth.md](../interfaces/auth.md)

**Read next:** [008-otel-observability.md](008-otel-observability.md)
