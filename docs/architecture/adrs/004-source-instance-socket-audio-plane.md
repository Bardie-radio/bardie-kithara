# ADR 004: Session FIFO Audio Plane

**Status:** Accepted

## Context

Audio must flow from source modules to Kithara encoders without restarting FFmpeg on every track (ICY listeners break). Options: per-track Unix sockets, module-owned session sockets, HTTP audio, or a Kithara-owned named FIFO with rotating writers.

## Decision

- Each **alive** Struna has one **session FIFO** owned by Kithara/Neck.
- **FFmpeg** reads that FIFO for the entire Struna life (until owner stop / cleanup).
- Source modules run **track jobs**: `StartTrack` receives the FIFO path and writes **canonical PCM**; `StopTrack` ends the job.
- Queue shifts **kill/restart the decoder job only** — not FFmpeg. Different modules may write in turn (multi-source Struna).
- When no writer is attached, Neck feeds **silence** into the FIFO.
- No HTTP for internal audio.

## Consequences

- Continuous listener experience across skips and source switches.
- Modules do not own the listen endpoint; spoofing another Struna’s FIFO is a permissions/FS concern.
- Cross-host modules would need a different transport (future).
- Informal prewarm is allowed; no MVP `PrepareTrack` RPC.

## Alternatives considered

- **Per-track new socket + FFmpeg restart** — rejected; breaks players.
- **Module-owned listen socket for the session** — weak for multi-source (socket dies with module).
- **Per-module Icecast** — rejected; operational overhead.
- **HTTP audio between services** — rejected; latency and complexity.
- **stdout/pipe only** — poor fit for parallel Strunas.

**Related:** [domains/source-instances.md](../domains/source-instances.md) · [interfaces/streaming-stack.md](../interfaces/streaming-stack.md)

**Read next:** [005-isolated-instance-per-stream.md](005-isolated-instance-per-stream.md)
