# Library and Tunes

> **Scope note:** This is a Kithara deep-dive. The shared library model, Tune persistence, and QueueEntry references belong here. **Where** bytes live is [storage.md](storage.md). How Magpie downloads and cache-hits is owned by [Magpie architecture](https://github.com/Bardie-radio/magpie/tree/main/docs/architecture).

```mermaid
flowchart LR
  Play[Play request]
  Lib[(Shared library)]
  Tune[Tune]
  Store[Blob storage]
  Queue[QueueEntry]
  Job[Track job]
  Play -->|cache hit| Tune
  Play -->|cache miss download| Tune
  Tune -->|storage key| Store
  Tune --> Lib
  Tune -.->|optional ref| Queue
  Queue -->|StartTrack| Job
```

A **Tune** is a **shared library** item: metadata plus an optional **blob** of content that can be replayed (downloaded ytdl media, local files). It is **not owned by a Struna** — many Strunas can point at the same Tune through queue entries.

The library exists so the same piece of audio isn’t fetched or uploaded twice, and so history / “owned tracks” can hang off durable `User` rows without tying them to a single stream.

## What a Tune holds (sketch)

| Kind of data | Examples |
|--------------|----------|
| Identity | Internal id; source module slug; module-native external id (e.g. YouTube video id) |
| Metadata | Title, artist/uploader, duration, artwork URL |
| Blob | Opaque **storage key** (+ content type / size) resolved by the active [blob storage](storage.md) driver — not a host path |
| Provenance | Who first brought it in (user or module-managed user), when |

Exact schema is still target-level — see [ADR 006](../adrs/006-stream-source-tune-data-model.md) and [ADR 010](../adrs/010-blob-storage-backends.md). The invariant is: **one library**, referenced from queues, not buried inside a playlist or a single Struna.

## Where tunes apply

| Source | How Tunes are used |
|--------|--------------------|
| **Magpie** (ytdl) | Cache-first Tune + blob — deep dive in [Magpie docs](https://github.com/Bardie-radio/magpie/blob/main/docs/architecture/02-contracts.md) |
| **Catbird** (files) | Tune required — metadata + storage key ([Catbird planned role](https://github.com/Bardie-radio/catbird/blob/main/docs/architecture/01-planned-role.md)) |
| **Starling** (external / continuous stream) | No Tune — live URI ([Starling planned role](https://github.com/Bardie-radio/starling/blob/main/docs/architecture/01-planned-role.md)) |

## Queue model

**QueueEntry** on a Struna holds play intent: **`module` slug + track ref**, and optionally a **Tune id** when the library already knows the item.

At play time, Neck calls `StartTrack` on that source module with the track ref (and FIFO path). The module resolves cache vs download (Magpie) or blob vs live URI (Catbird / Starling) via the shared storage contract and writes canonical PCM into the Struna’s session FIFO.

One Struna can switch modules across queue entries — Magpie track, then Catbird file, then Magpie again — reusing the same FIFO and FFmpeg process.

## Ownership and sharing

- Tunes live in a **shared library**, not under a playlist or a single Struna.
- Users (including module-managed ones) can accumulate **owned / history** references to Tunes over time without tying those rows to one stream’s lifetime.
- Deleting a Struna must not delete its Tunes; other Strunas and history may still need them.
- Blob bytes live in [pluggable storage](storage.md); deleting a Tune implies deleting its blob (when no longer referenced — exact GC policy later).

## Prototype artifacts

Current [Tune.cs](../../Models/Tune.cs) has conflicting `PlaylistId` FK and `List<Playlist> Playlists`. Target model uses a **shared library** + queue references + storage keys — see [ADR 006](../adrs/006-stream-source-tune-data-model.md).

**Related:** [storage.md](storage.md) · [ADR 006](../adrs/006-stream-source-tune-data-model.md) · [ADR 010](../adrs/010-blob-storage-backends.md) · [playback-control.md](playback-control.md) · [source-modules.md](source-modules.md) · [glossary](../glossary.md)

**Read next:** [storage.md](storage.md)
