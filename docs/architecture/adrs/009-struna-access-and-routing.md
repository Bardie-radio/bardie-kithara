# ADR 009: Struna Access and Routing

**Status:** Accepted (amended: protected control = guest-code exchange → guest JWT)

## Context

Streams need human-readable URLs, separate listen vs control permissions, and legacy player compatibility without OIDC. Short guest codes must not be sent on every control API call.

## Decision

**URI map (one domain):**

| Path | Service |
|------|---------|
| `/` | Plume (auth required) |
| `/api/*` | Kithara REST |
| `/stream/{slug}` | Kithara Stream Server |
| `/player/{slug}` | Plume control page |

**Slug:** user-chosen; unique among **alive** Strunas; freed on **DELETE** (or silent cleanup).

**Playback access** (independent): `public` | `protected` (listen token) | `private` (full auth).

**Control access** (independent): `private` (auth) | `protected` (guest code → **guest control JWT** via exchange). **No public control.**

**Protected playback MVP:** query param `/stream/{slug}?token=...`. Listen token is a **Kithara-owned** Struna secret (no Bearer exchange — legacy players). Basic Auth and path token documented for v0.2 evaluation.

**Protected control:** short **guest code** (Kithara-owned) exchanged once at `POST /api/streams/{id}/guest/exchange` for a **Kithara-signed guest control JWT**. Subsequent control uses Bearer only. Rate-limit exchange; rotate code to invalidate guests. See [struna-access](../domains/struna-access.md).

## Consequences

- Paste-friendly URLs for VLC/VRChat on public/protected streams.
- Private playback incompatible with most legacy players (by design).
- Party DJ without accounts: share a code, exchange once, control with guest JWT.

## Repos needing follow-up

| Decision | Follow up in |
|----------|----------------|
| URI map + Plume routes | **bardie-plume**, org edge/Compose ([05-deployment](https://github.com/Bardie-radio/.github/blob/main/profile/docs/architecture/05-deployment.md)) |
| Listen token / guest exchange UX | **bardie-plume** (Kithara owns secrets + mints guest JWTs) |

## Alternatives considered

- **GUID-only URLs** — rejected; poor UX for external players.
- **Public control plane** — rejected; anonymous queue/skip omitted.
- **Single access level** — rejected; listen and control needs differ.
- **Guest code on every request** — rejected; exchange for guest JWT instead ([ADR 007](007-auth-adapter-modules.md)).

**Related:** [domains/struna-access.md](../domains/struna-access.md) · [interfaces/uri-routing.md](../interfaces/uri-routing.md) · [interfaces/auth.md](../interfaces/auth.md)

**Read next:** [../mvp/v0.1-scope.md](../mvp/v0.1-scope.md)
