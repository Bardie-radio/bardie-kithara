# ADR 004: Source Instance Socket Audio Plane

**Status:** Accepted

## Context

Audio must flow from source modules to Kithara encoders. Options: HTTP streaming between services, per-module Icecast, pipes, or Unix domain sockets.

## Decision

On `CreateInstance`, a source module spawns an isolated playback and exposes audio on a **Unix domain socket**. Neck's FFmpeg reads from that socket. No HTTP for internal audio.

## Consequences

- Low overhead; no network stack for raw audio on same host.
- Module manages N parallel instances and socket paths.
- Kithara discovers socket via gRPC response (`socketPath`).
- Cross-host modules would need a different transport (future).

## Alternatives considered

- **stdout/pipe only** — poor fit for parallel instances.
- **Per-module Icecast** — rejected; operational overhead.
- **HTTP audio between services** — rejected; latency and complexity.

**Related:** [domains/source-instances.md](../domains/source-instances.md)

**Read next:** [005-isolated-instance-per-stream.md](005-isolated-instance-per-stream.md)
