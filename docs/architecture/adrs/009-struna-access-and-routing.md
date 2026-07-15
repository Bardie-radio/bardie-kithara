# ADR 009: Struna Access and Routing

**Status:** Accepted

## Context

Streams need human-readable URLs, separate listen vs control permissions, and legacy player compatibility without OIDC.

## Decision

**URI map (one domain):**

| Path | Service |
|------|---------|
| `/` | Plume (auth required) |
| `/api/*` | Kithara REST |
| `/stream/{slug}` | Kithara Stream Server |
| `/player/{slug}` | Plume control page |

**Slug:** user-chosen; unique among **active** Strunas; freed on delete/stop.

**Playback access** (independent): `public` | `protected` (listen token) | `private` (full auth).

**Control access** (independent): `private` (auth) | `protected` (guest code). **No public control.**

**Protected playback MVP:** query param `/stream/{slug}?token=...`. Basic Auth and path token documented for v0.2 evaluation.

## Consequences

- Paste-friendly URLs for VLC/VRChat on public/protected streams.
- Private playback incompatible with most legacy players (by design).
- Guest code enables party DJ without accounts.

## Alternatives considered

- **GUID-only URLs** — rejected; poor UX for external players.
- **Public control plane** — rejected; anonymous queue/skip omitted.
- **Single access level** — rejected; listen and control needs differ.

**Related:** [domains/struna-access.md](../domains/struna-access.md) · [interfaces/uri-routing.md](../interfaces/uri-routing.md)

**Read next:** [../mvp/v0.1-scope.md](../mvp/v0.1-scope.md)
