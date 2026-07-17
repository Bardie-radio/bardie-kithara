# ADR 005: Isolated Track Jobs Per Stream

**Status:** Accepted

## Context

When two Strunas play the same track, decode work could be shared (reference-counted) or isolated.

## Decision

**Isolated track jobs:** each Struna gets its own source track job even for identical content. No sharing of FIFO writers or decode pipelines across Strunas.

## Consequences

- Clear lifecycle: Struna stop → stop active track job + tear down session FIFO/FFmpeg.
- Higher resource use under duplicate demand (acceptable for MVP).
- Simpler debugging and OTel traces per Struna.
- Module enforces its own parallel-job limits.

## Alternatives considered

- **Shared decode with refcount** — deferred; adds attach/detach complexity across Strunas.
- **Hybrid fork-on-override** — deferred.

**Related:** [domains/source-instances.md](../domains/source-instances.md) · [domains/streams.md](../domains/streams.md)

**Read next:** [006-stream-source-tune-data-model.md](006-stream-source-tune-data-model.md)
