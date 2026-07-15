# ADR 006: Stream / Source / Tune Data Model

**Status:** Accepted

## Context

Prototype models conflate playlists, tunes, and streams. Multi-source architecture needs stream-centric design.

## Decision

- **Struna (stream)** plays **source instances**, not tunes directly.
- **Tune** is a **library/cache reference** for file and ytdl sources; optional for live-input sources.
- **QueueEntry** holds play intent; resolved to source instance at play time.
- Struna has `slug` (public URL), internal `Id` (GUID), independent playback/control access fields.

Prototype `Tune.PlaylistId` + `Tune.Playlists` and `Struna` without source binding are **spike artifacts**, not target schema.

## Consequences

- YouTube plays without a Tune row (external ref in queue).
- Library can grow independently of active streams.
- Schema migration required from prototype.

## Alternatives considered

- **Tune-owned-by-playlist** — rejected; conflicts with multi-source.
- **Stream-owned tunes only** — rejected; no shared library.

**Related:** [domains/library-and-tunes.md](../domains/library-and-tunes.md) · [domains/streams.md](../domains/streams.md)

**Read next:** [007-auth-adapter-modules.md](007-auth-adapter-modules.md)
