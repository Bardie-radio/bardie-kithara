# ADR 006: Stream / Source / Tune Data Model

**Status:** Accepted (amended: Tune is the unified library unit for queue, history, and optional blob cache — including sparse Tunes without bytes, e.g. Starling)

## Context

Prototype models conflate playlists, tunes, and streams. Multi-source architecture needs one reusable library item for queue, history, and cache — including live/external refs that have no downloadable blob.

## Decision

- **Struna (stream)** plays **track jobs** from source modules into a session FIFO. The control plane thinks in **Tunes**, not ad-hoc URIs alone.
- **Tune** is the **shared library unit**: used as **queue items**, **history items**, and (when applicable) **cache items**. It is not owned by a Struna.
- **Blob / storage key is optional.** Magpie/Catbird attach opaque storage keys when bytes are cached. Starling-style items are still Tunes: module slug + external id (stream URI), sparse or empty metadata, **no** cache blob. Replay from history is `play` by Tune id — no re-typing the URI.
- **Magpie is cache-first:** resolve an existing Tune (blob present) before fetching; on miss, download into blob storage and **create/update a Tune**, then play.
- **QueueEntry** references a **Tune id** (plus position / Struna binding). At play time Neck resolves the Tune → module slug + track ref (external id / storage key) and calls `StartTrack`.
- **History / owned tracks** are references to Tune ids for a user (durable or managed), independent of Struna lifetime.
- Struna has `slug` (public URL), internal `Id` (GUID), owner + grants, independent playback/control access fields.

Prototype `Tune.PlaylistId` + `Tune.Playlists` and `Struna` without source binding are **spike artifacts**, not target schema.

## Consequences

- Every playable intent can land in the library — including raw external streams — so history and re-queue work the same way across modules.
- Magpie always ties replayable ytdl content to a Tune (cache hit or create-on-download).
- One Struna can switch modules across queue entries (each entry → a Tune from any module).
- Deleting a Struna does not delete Tunes; history and other queues may still need them.
- Deleting a Tune deletes its blob only when a storage key exists and GC says it is unreferenced ([ADR 010](010-blob-storage-backends.md)).
- Storage backend can change without rewriting Tune identity semantics.

## Alternatives considered

- **Tune-owned-by-playlist** — rejected; conflicts with multi-source.
- **Stream-owned tunes only** — rejected; no shared library.
- **Magpie play without Tune rows** — rejected; loses cache reuse and durable history.
- **No Tune for Starling (URI-only queue)** — superseded; sparse Tune-without-blob keeps history/queue uniform.
- **Host filesystem paths as durable cache pointers** — rejected; breaks S3/WebDAV and multi-host layouts.

**Related:** [domains/library-and-tunes.md](../domains/library-and-tunes.md) · [domains/storage.md](../domains/storage.md) · [domains/streams.md](../domains/streams.md) · [010-blob-storage-backends.md](010-blob-storage-backends.md)

**Read next:** [007-auth-adapter-modules.md](007-auth-adapter-modules.md)
