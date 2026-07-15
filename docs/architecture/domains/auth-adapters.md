# Auth Adapters

```mermaid
flowchart TB
  Plume -->|discovery| Kithara
  Kithara -->|GetProviders| Local[login+password MVP]
  Kithara -->|ValidateToken| Local
  Kithara -.->|v0.2| OIDC[OIDC adapter]
```

**Auth adapters** mirror source modules: separate repos/containers registered with Kithara's **auth orchestrator** (the router lives inside Kithara). You swap login styles without rebuilding the core.

## MVP

| Module | Role |
|--------|------|
| Login+password adapter *(name TBD)* | Username/password, token issuance, `form_schema` UI for Plume |

## v0.2+

| Module | Role |
|--------|------|
| OIDC adapter *(name TBD)* | External OIDC (Zitadel, Google, …) |

## Adapter-owned login UI

Plume does **not** hardcode auth forms. Discovery returns `uiMode`:

- `form_schema` — Plume renders fields from adapter metadata (MVP)
- `embed` — iframe adapter login page
- `redirect` — OIDC / external IdP

## Service tokens

Bots and automation use **pre-provisioned tokens** in Kithara config — no auth adapter container.

**Related:** [interfaces/auth.md](../interfaces/auth.md) · [interfaces/grpc-auth-adapter.md](../interfaces/grpc-auth-adapter.md) · [ADR 007](../adrs/007-auth-adapter-modules.md)

**Read next:** [library-and-tunes.md](library-and-tunes.md)
