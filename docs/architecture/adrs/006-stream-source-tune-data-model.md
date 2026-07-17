# ADR 006: Stream / Source / Tune Data Model

**Status:** Accepted

## Context

Prototype models conflate playlists, tunes, and streams. Multi-source architecture needs stream-centric design.

## Decision

- **Struna (stream)** plays **track jobs** from source modules into a session FIFO — not tunes directly.
- **Tune** is a **library/cache reference** for file and ytdl sources; optional for live-input sources.
- **QueueEntry** holds play intent: **module slug + track ref** (optional Tune id); resolved with `StartTrack` at play time.
- Struna has `slug` (public URL), internal `Id` (GUID), independent playback/control access fields, and listener encode mode.

Prototype `Tune.PlaylistId` + `Tune.Playlists` and `Struna` without source binding are **spike artifacts**, not target schema.

## Consequences

- YouTube plays without a Tune row (external ref in queue).
- One Struna can switch modules across queue entries.
- Library can grow independently of alive streams.
- Schema migration required from prototype.

## Alternatives considered

- **Tune-owned-by-playlist** — rejected; conflicts with multi-source.
- **Stream-owned tunes only** — rejected; no shared library.

**Related:** [domains/library-and-tunes.md](../domains/library-and-tunes.md) · [domains/streams.md](../domains/streams.md)

**Read next:** [007-auth-adapter-modules.md](007-auth-adapter-modules.md)
