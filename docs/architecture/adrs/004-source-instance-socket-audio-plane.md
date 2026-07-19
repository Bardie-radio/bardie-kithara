# ADR 004: Session FIFO Audio Plane

**Status:** Accepted (amended: Compose MVP = shared volume; Kithara creates per-Struna audio endpoints on demand)

## Context

Audio must flow from source modules to Kithara encoders without restarting FFmpeg on every track (ICY listeners break). Options: per-track sockets with FFmpeg restart, module-owned session sockets, HTTP audio, or a Kithara-owned session endpoint with rotating writers on a shared volume.

## Decision

- Each **alive** Struna has one **session audio endpoint** owned by Kithara/Neck (product name: **session FIFO**).
- **MVP attach:** modules and Kithara share a Compose **volume**. Kithara creates the endpoint **on demand** when the Struna becomes alive; `StartTrack` passes the path; the source module writes **canonical PCM**; FFmpeg reads for the Struna life.
- Access control is filesystem / endpoint permissions on that shared volume (not a public network listen). Exact node type: **named pipe (mkfifo)** or **Unix domain socket** — both fit the model; prefer whichever is easier to lock down for “only the intended writer” in implementation (Unix sockets + `SO_PEERCRED` are a strong option on Linux).
- Queue shifts **kill/restart the decoder job only** — not FFmpeg. Different modules may write in turn (multi-source Struna).
- When no writer is attached, Neck feeds **silence** into the endpoint.
- No HTTP for internal audio in MVP.
- Cross-host / non-shared-volume modules need a different transport later (out of MVP).

## Consequences

- Continuous listener experience across skips and source switches.
- Modules do not own the listen endpoint.
- Compose must mount the same audio volume on Kithara and every source module that writes PCM.
- Informal prewarm is allowed; no MVP `PrepareTrack` RPC.

## Alternatives considered

- **Per-track new socket + FFmpeg restart** — rejected; breaks players.
- **Module-owned listen socket for the session** — weak for multi-source (socket dies with module).
- **Per-module Icecast** — rejected; operational overhead.
- **HTTP/gRPC streaming PCM into Kithara** — deferred; more moving parts and latency for bulky PCM; revisit if shared volumes become painful.
- **stdout/pipe only** — poor fit for parallel Strunas.

**Related:** [domains/source-instances.md](../domains/source-instances.md) · [interfaces/streaming-stack.md](../interfaces/streaming-stack.md) · [interfaces/grpc-source-module.md](../interfaces/grpc-source-module.md)

**Read next:** [005-isolated-instance-per-stream.md](005-isolated-instance-per-stream.md)
