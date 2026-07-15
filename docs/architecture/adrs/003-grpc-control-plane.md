# ADR 003: gRPC Control Plane

**Status:** Accepted

## Context

Kithara orchestrates source modules and auth adapters. Internal communication could use REST, gRPC, or message buses.

## Decision

Use **gRPC** for all internal control-plane calls between Kithara and modules (source + auth). REST is reserved for Plume-facing `/api/*`.

## Consequences

- Strong typing via protobuf; good OTel trace propagation.
- Not "a web app per module" — modules are gRPC servers/clients.
- Requires HTTP/2 between containers; standard in Docker Compose.
- Browser cannot call modules directly (by design).

## Alternatives considered

- **REST between modules** — rejected; couples modules to HTTP semantics.
- **NATS / message bus** — deferred; gRPC sufficient for MVP scale.
- **In-process .NET plugins** — rejected for Docker modularity.

**Related:** [interfaces/grpc-source-module.md](../interfaces/grpc-source-module.md) · [interfaces/grpc-auth-adapter.md](../interfaces/grpc-auth-adapter.md)

**Read next:** [004-source-instance-socket-audio-plane.md](004-source-instance-socket-audio-plane.md)
