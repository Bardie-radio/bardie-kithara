# Streams (Struna)

```mermaid
stateDiagram-v2
  [*] --> Alive: POST /api/streams
  Alive --> Alive: play / skip / pause / queue
  Alive --> Stopped: stop or cleanup
  Stopped --> [*]: slug freed
  Alive --> [*]: delete
  Stopped --> [*]: delete
```

A **Struna** (stream) is a named broadcast channel managed by **Neck** inside Kithara.

## Identity

| Field | Purpose |
|-------|---------|
| `Id` | Internal GUID — API paths, DB, traces |
| `Slug` | User-chosen URL name — `/stream/{slug}`, `/player/{slug}` |
| `Title` | Display name |

**Slug rules:** lowercase alphanumeric + hyphens; unique among **alive** Strunas; reserved names blocked (`api`, `stream`, `admin`, `player`, … — see [configuration](../operations/configuration.md)); HTTP 409 on conflict.

**Alive from create:** `POST /api/streams` reserves the slug and starts FFmpeg + session FIFO (silence until the first track). Owner **stop** (or silent **cleanup**) frees the slug.

## Neck service responsibilities

1. Start FFmpeg once per alive Struna; keep it running until stop
2. Own the per-Struna **session FIFO**; feed silence when no module is writing
3. `StartTrack` / `StopTrack` on source modules (multi-source via module slug)
4. Register Stream Server endpoint `/stream/{slug}`
5. Push now-playing metadata for ICY injection
6. Monitor track-job health via gRPC status stream

Neck lives **inside Kithara** — not a separate container. FFmpeg process ownership belongs to a hosted supervisor (not a single HTTP request scope) — see [internal structure](../overview/02-internal-structure.md).

## Target schema fields

- `PlaybackAccess`: public | protected | private
- `ControlAccess`: private | protected
- `ListenToken`, `GuestCode` (nullable; owned by Kithara)
- `EncodeMode`: compatibility | quality (listener encode profile)
- Active track job + queue entries (`module` slug + track ref; Tune optional)

## Cleanup (planned)

Operator-configurable: auto-**stop** Strunas that stay silent longer than a threshold (frees slug).

**Related:** [domains/struna-access.md](struna-access.md) · [ADR 006](../adrs/006-stream-source-tune-data-model.md) · [domains/source-instances.md](source-instances.md)

**Read next:** [struna-access.md](struna-access.md)
