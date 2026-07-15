# ADR 001: Broadcast Sync Model

**Status:** Accepted

## Context

Bardie promises "one audio stream for everyone." Multiple sync models exist: live broadcast (radio), synchronized local playback (Spotify Jam-like), or hybrid.

## Decision

Use **live broadcast**: one FFmpeg encoder per active Struna pushes a continuous stream. Skip, queue changes, and track transitions affect **all listeners** on that Struna simultaneously.

## Consequences

- Simple mental model: tune in like radio.
- No per-client position tracking required for MVP.
- Seek affects everyone — appropriate for shared listening.
- Latency is encoder + network buffer (seconds), not milliseconds.

## Alternatives considered

- **Synchronized playback state** — each client plays locally but follows shared track+position. Rejected for MVP complexity.
- **Hybrid** — broadcast + state API. Deferred to v0.2+ if needed.

**Related:** [domains/playback-control.md](../domains/playback-control.md) · [ADR 002](002-kithara-native-ffmpeg-streaming.md)

**Read next:** [002-kithara-native-ffmpeg-streaming.md](002-kithara-native-ffmpeg-streaming.md)
