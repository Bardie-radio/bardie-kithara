# ADR 008: OpenTelemetry Observability

**Status:** Accepted

## Context

Bardie is modular — when something breaks, you need the full path across Kithara, Plume, and every module, not three disconnected log files. The reference SRE stack is Grafana-forward (Tempo, Loki, Prometheus), but the contract stays backend-agnostic.

## Decision

**Everything is under coverage.** Every component exports OTLP traces, metrics, and logs:

- Kithara (API, Neck, Stream Server, auth orchestrator)
- Plume (client-facing)
- Source modules
- Auth adapter modules
- Future bots

W3C `traceparent` propagated across HTTP and gRPC. Service names: `bardie.kithara`, `bardie.plume`, `bardie.auth.*`, `bardie.source.*` (exact module suffixes follow chosen module names).

## Consequences

- OTel is part of the **module contract**, not optional.
- Auth and client modules appear in the same trace graphs as Kithara.
- Span attributes: `struna.id`, `struna.slug`, `source.instance.id`, `auth.adapter.id`.
- Never log tokens or passwords.

## Repos needing follow-up

| Service name | Follow up in |
|--------------|----------------|
| `bardie.plume` | **bardie-plume** |
| `bardie.source.*` | Source module repos |
| `bardie.auth.*` | Auth adapter repos (login+password MVP name/repo TBD) |
| Collector wiring | Org Compose / [05-deployment](https://github.com/Bardie-radio/.github/blob/main/profile/docs/architecture/05-deployment.md) |

## Alternatives considered

- **Logging only** — rejected; insufficient for modular debugging.
- **Kithara-only tracing** — rejected; blind to modules and Plume.

**Related:** [operations/observability.md](../operations/observability.md)

**Read next:** [009-struna-access-and-routing.md](009-struna-access-and-routing.md)
