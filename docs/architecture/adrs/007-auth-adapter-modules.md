# ADR 007: Auth Adapter Modules

**Status:** Accepted

## Context

Auth must be modular (login+password, OIDC, custom) without forcing a self-hosted IdP for every deploy. Options: bake auth into Kithara only, a separate auth-core container, or adapter modules. Operators also want **one Bardie database** (Kithara’s); external IdPs may keep their own stores.

## Decision

- **Auth Orchestrator** lives **inside Kithara** (discovery, authenticate routing, JWT issuance, service tokens, listen/guest secrets).
- **Local password** is a built-in provider using Kithara tables (MVP).
- **External auth adapters** (OIDC in v0.2; names TBD) are separate containers on gRPC; they do **not** own a Bardie user DB.
- **User core + `UserAuthBinding`** in Kithara DB: thin `User` plus `(user_id, provider_slug, payload)` for provider-specific state.
- After identity proof, **Kithara issues JWT + refresh** for API clients. Adapters do not mint the client Bearer.
- **OIDC callback** lands on **Kithara** (Plume optional). Adapters stay off the public edge.
- **Client-rendered UI** via discovery (`form_schema`, `redirect`) — no adapter-hosted login pages.
- **Listen tokens and guest codes** are Struna secrets owned by Kithara (independent of which login provider is used).
- **Explicit account linking** across providers; **provider priority tier-list** (config at start) arbitrates mapped org roles when bindings disagree.
- Env **bootstrap admin** on empty DB; disabled when an OIDC user-provider is configured from the start.
- Service / module **join** tokens in Kithara config.

## Consequences

- One Bardie DB; IdPs remain external for OIDC user lifecycle.
- Same session model for local, OIDC, and bots (service tokens).
- Plume never hardcodes password fields; stack works without Plume.
- Struna ACLs and listen/guest secrets stay in Kithara regardless of IdP.

## Alternatives considered

- **Separate auth gateway container** — rejected; 1:1 with Kithara makes core redundant.
- **Adapter issues client tokens / ValidateToken every request** — rejected; fights unified sessions and Plume-optional clients.
- **Adapter-owned user database** — rejected; second Bardie DB.
- **Adapter-hosted login HTTP** — rejected; browsers must not call modules (ADR 003).
- **Kithara validates only raw Zitadel JWT on every API call** — rejected as sole model; still use OIDC for identity, then Kithara JWT for API.

**Related:** [domains/auth-adapters.md](../domains/auth-adapters.md) · [interfaces/auth.md](../interfaces/auth.md)

**Read next:** [008-otel-observability.md](008-otel-observability.md)
