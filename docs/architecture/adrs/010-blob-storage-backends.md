# ADR 010: Blob Storage Backends

**Status:** Accepted

## Context

The shared library caches Magpie downloads and Catbird uploads as binary blobs. Operators will want different places for those bytes (local volume, S3-compatible object stores, NAS). Host filesystem paths on Tune rows break when the backend changes or when multiple modules need the same cache.

## Decision

- **Kithara owns** blob storage configuration and the optional **storage key** on each Tune (plus content type / size when present).
- Source modules (Magpie, Catbird) read/write through a **shared storage contract** — one backend for the stack, not per-module buckets as the primary model.
- **Drivers:** local filesystem (**MVP default**, `BARDIE_STORAGE_PATH`); **S3-compatible** next (AWS S3, MinIO, Garage, R2, B2, …); **WebDAV** later (NAS / Nextcloud).
- NFS/SMB = mount under the local driver, not a separate driver.
- **Not Redis** or other in-memory stores for audio blobs — wrong size and durability model.
- Session FIFOs and FFmpeg scratch stay **local ephemeral**, outside blob storage.
- Library blobs are **not** served on the public edge; listen stays ICY `/stream/{slug}`.

## Consequences

- Magpie cache-first and Catbird uploads share one coherent library.
- Switching drivers does not rewrite Tune semantics — only config.
- Modules put/get via a **thin Kithara storage API** (modules dial Kithara) — no parallel `BARDIE_STORAGE_*` on Magpie/Catbird; drivers stay inside Kithara. Keep the hop cheap for bulky writes.

## Alternatives considered

- **Per-module independent S3 buckets** — rejected as primary model; fragments the shared library.
- **Durable host paths on Tune** — rejected; ties library to one machine layout.
- **Redis / in-memory as audio cache** — rejected; large binaries belong on disk or object storage.
- **Public HTTP for raw library files** — rejected; playback remains Stream Server ICY.

**Related:** [domains/storage.md](../domains/storage.md) · [domains/library-and-tunes.md](../domains/library-and-tunes.md) · [006-stream-source-tune-data-model.md](006-stream-source-tune-data-model.md)

**Read next:** [../domains/storage.md](../domains/storage.md)
