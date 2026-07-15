# ADR 005: Isolated Instance Per Stream

**Status:** Accepted

## Context

When two Strunas play the same track, audio instances could be shared (reference-counted) or isolated.

## Decision

**Isolated instances:** each Struna gets its own source instance even for identical content. No sharing of sockets between Strunas.

## Consequences

- Clear lifecycle: Struna stop → instance stop.
- Higher resource use under duplicate demand (acceptable for MVP).
- Simpler debugging and OTel traces per Struna.
- Module enforces its own resource limits.

## Alternatives considered

- **Shared instance with refcount** — deferred; adds attach/detach complexity.
- **Hybrid fork-on-override** — deferred.

**Related:** [domains/source-instances.md](../domains/source-instances.md) · [domains/streams.md](../domains/streams.md)

**Read next:** [006-stream-source-tune-data-model.md](006-stream-source-tune-data-model.md)
