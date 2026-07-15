# ADR 002: Kithara-Native FFmpeg Streaming

**Status:** Accepted

## Context

Listeners need ICY-compatible metadata for VLC, VRChat, and similar players. Options: external Icecast, HLS, or Kithara-served HTTP with ICY headers.

## Decision

- **FFmpeg** (in Neck / Struna Encoder) ingests source-instance audio, encodes MP3, writes to a pipe.
- **Kithara Stream Server** serves `GET /stream/{slug}` with **ICY-over-HTTP** (`icy-metaint`, inline `StreamTitle`).
- **No Icecast** in MVP.

## Consequences

- One fewer container in Compose.
- Kithara owns listener URLs on the same domain as `/api`.
- Stream Server must implement SHOUTcast-style metadata injection.
- Kithara process is heavier (encoding + serving).

## Alternatives considered

- **Icecast relay** — community demand only; low priority; no dedicated documentation.
- **GStreamer / Liquidsoap** — evaluate if FFmpeg limits hit.
- **HLS** — future output adapter for web-first clients.

**Related:** [interfaces/streaming-stack.md](../interfaces/streaming-stack.md) · [interfaces/http-stream-output.md](../interfaces/http-stream-output.md)

**Read next:** [003-grpc-control-plane.md](003-grpc-control-plane.md)
