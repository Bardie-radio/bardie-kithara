# ADR 006: Stream / Source / Tune Data Model

**Status:** Accepted

## Context

Prototype models conflate playlists, tunes, and streams. Multi-source architecture needs stream-centric design. Durable cache pointers must survive switching storage backends.

## Decision

- **Struna (stream)** plays **track jobs** from source modules into a session FIFO — not tunes directly.
- **Tune** is a **library/cache reference** for file and ytdl sources; not used for continuous-input sources (e.g. Starling).
- **Tune blob pointer** is an opaque **storage key** (plus content type / size), resolved by Kithara’s pluggable [blob storage](../domains/storage.md) — not a host filesystem path.
- **Magpie is cache-first:** resolve an existing Tune (blob present) before fetching; on miss, download into blob storage and **create/update a Tune**, then play.
- **QueueEntry** holds play intent: **module slug + track ref** (optional Tune id); resolved with `StartTrack` at play time.
- Struna has `slug` (public URL), internal `Id` (GUID), independent playback/control access fields, and listener encode mode.

Prototype `Tune.PlaylistId` + `Tune.Playlists` and `Struna` without source binding are **spike artifacts**, not target schema.

## Consequences

- Magpie always ties replayable ytdl content to a Tune (cache hit or create-on-download); it does not skip the library for “external ref only” play.
- One Struna can switch modules across queue entries.
- Library can grow independently of alive streams; deleting a Struna does not delete Tunes.
- Storage backend can change without rewriting Tune identity semantics ([ADR 010](010-blob-storage-backends.md)).
- Schema migration required from prototype.

## Alternatives considered

- **Tune-owned-by-playlist** — rejected; conflicts with multi-source.
- **Stream-owned tunes only** — rejected; no shared library.
- **Magpie play without Tune rows** — rejected; loses cache reuse and durable history/owned-track links.
- **Host filesystem paths as durable cache pointers** — rejected; breaks S3/WebDAV and multi-host layouts.

**Related:** [domains/library-and-tunes.md](../domains/library-and-tunes.md) · [domains/storage.md](../domains/storage.md) · [domains/streams.md](../domains/streams.md) · [010-blob-storage-backends.md](010-blob-storage-backends.md)

**Read next:** [007-auth-adapter-modules.md](007-auth-adapter-modules.md)
