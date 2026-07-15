# ADR 008: OpenTelemetry Observability

**Status:** Accepted

## Context

Bardie is modular; debugging requires visibility across all communication paths. SRE stack is Grafana-forward (Tempo, Loki, Prometheus) but should remain backend-agnostic.

## Decision

**Everything is under coverage.** Every component exports OTLP traces, metrics, and logs:

- Kithara (API, Neck, Stream Server, auth orchestrator)
- Plume (client-facing)
- Source modules
- Auth adapter modules
- Future bots

W3C `traceparent` propagated across HTTP and gRPC. Service names: `bardie.kithara`, `bardie.plume`, `bardie.auth.local`, `bardie.source.youtube`, etc.

## Consequences

- OTel is part of the **module contract**, not optional.
- Auth and client modules are first-class in trace graphs.
- Span attributes: `struna.id`, `struna.slug`, `source.instance.id`, `auth.adapter.id`.
- Never log tokens or passwords.

## Alternatives considered

- **Logging only** — rejected; insufficient for modular debugging.
- **Kithara-only tracing** — rejected; blind to modules and Plume.

**Related:** [operations/observability.md](../operations/observability.md)

**Read next:** [009-struna-access-and-routing.md](009-struna-access-and-routing.md)
