# gRPC Module Registry (sketch)

Every Bardie module — **source**, **auth**, and **client** — joins the same way: the module **dials Kithara** and calls `Register` (plus heartbeats). There is no special case for Plume vs Magpie vs Bes on the join path.

Kithara hosts this service on internal gRPC (`:5000`). Modules default `KITHARA_GRPC_ADDRESS` to Compose DNS (e.g. `kithara:5000`) so local stacks need little wiring.

```protobuf
service ModuleRegistry {
  rpc Register(RegisterRequest) returns (RegisterResponse);
  rpc Heartbeat(HeartbeatRequest) returns (HeartbeatResponse);
}

enum ModuleKind {
  SOURCE = 0;
  AUTH = 1;
  CLIENT = 2;
}

message RegisterRequest {
  string slug = 1;                 // lowercase codename; operator may override via env
  string join_secret = 2;          // bootstrap trust (before mTLS cert exists)
  ModuleKind kind = 3;
  repeated string capabilities = 4;
  string grpc_advertise_address = 5; // where Kithara dials this module for each work RPC
  // kind-specific: JWKS / search_fields / client auth_mode + permission ceiling — sketch
}

message RegisterResponse {
  // client certificate material for mTLS on subsequent RPCs — sketch
}
```

## Dial rules

| Direction | When |
|-----------|------|
| **Module → Kithara** | `Register`, `Heartbeat`, storage put/get |
| **Kithara → module** | Each work RPC (`Search`, `StartTrack`, `Authenticate`, `SeedAdmin`, …) as a **fresh dial** to `grpc_advertise_address` |

Per-call dials keep operations atomic: one RPC = one span, one auth decision, easier timeouts and least-privilege checks. No long-lived command stream from module to Kithara for work.

## Channel security (target)

1. First contact: `Register` with **join secret**.
2. On success Kithara **issues a client certificate** to the module.
3. After that, the **whole gRPC surface** (both directions) uses **mTLS** with that cert. Heartbeat / re-Register renews or rotates it.

The join secret is only the bootstrap; it is not a standing impersonation key for work RPCs once mTLS is up.

## Rules

| Rule | Why |
|------|-----|
| **All modules Register over gRPC** | One join surface; UI modules are not “REST-only citizens” |
| **Join secret required** | Bootstrap identity for source, auth, client |
| **Capabilities advertised at Register** | Kithara routes only what the module claims (e.g. auth `seedAdmin`, source `pause`) |
| **Static clients advertise a permission ceiling** | Managed users cannot be granted rights above what the module declared at handshake |
| **REST `/api` is for end users** | Client modules call REST to turn SPI into UX — not to join the mesh |

Work RPCs live on **per-kind contracts** the module hosts at `grpc_advertise_address`.

## Client modules

Same `Register` as everyone else: join secret + `kind=CLIENT` + auth mode (`user-aware` | `static`) + module-level capability / permission ceiling when static. Day-to-day Struna control still uses REST `/api` — see [clients](../domains/clients.md).

## Observability

Each work RPC is its own client call from Kithara → module: propagate W3C `traceparent`, record module slug + RPC name. Module OTel names stay `bardie.source.*` / `bardie.auth.*` / `bardie.plume` (etc.).

**Related:** [grpc-source-module](grpc-source-module.md) · [grpc-auth-adapter](grpc-auth-adapter.md) · [clients](../domains/clients.md) · [ADR 003](../adrs/003-grpc-control-plane.md)

**Read next:** [grpc-source-module.md](grpc-source-module.md)
