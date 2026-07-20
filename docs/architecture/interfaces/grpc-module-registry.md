# gRPC Module Registry (v0.1 draft)

Every Bardie module — **source**, **auth**, and **client** — joins the same way: the module **dials Kithara** and calls `Register` (plus heartbeats). There is no special case for Plume vs Magpie vs Bes on the join path.

**Status:** v0.1 draft — RPC set and dial rules are frozen; field names may still evolve slightly before NuGet publish. Checked-in proto: [`src/Kithara/Protos/module_registry.proto`](../../../src/Kithara/Protos/module_registry.proto).

Kithara hosts this service on internal gRPC (`:5000`). Modules default `KITHARA_GRPC_ADDRESS` to Compose DNS (e.g. `kithara:5000`) so local stacks need little wiring.

```protobuf
syntax = "proto3";

package bardie.modules.v1;

option csharp_namespace = "Bardie.Modules.V1";

service ModuleRegistry {
  rpc Register(RegisterRequest) returns (RegisterResponse);
  rpc Heartbeat(HeartbeatRequest) returns (HeartbeatResponse);
}

enum ModuleKind {
  MODULE_KIND_UNSPECIFIED = 0;
  MODULE_KIND_SOURCE = 1;
  MODULE_KIND_AUTH = 2;
  MODULE_KIND_CLIENT = 3;
}

message RegisterRequest {
  string slug = 1;                    // lowercase codename; operator may override via env
  string join_secret = 2;             // bootstrap trust (before mTLS cert exists)
  ModuleKind kind = 3;
  repeated string capabilities = 4;   // e.g. search, play, pause, seedAdmin
  string grpc_advertise_address = 5;  // where Kithara dials this module for work RPCs
  oneof details {
    SourceRegisterDetails source = 10;
    AuthRegisterDetails auth = 11;
    ClientRegisterDetails client = 12;
  }
}

message SourceRegisterDetails {
  repeated SearchFieldDescriptor search_fields = 1;
}

message SearchFieldDescriptor {
  string name = 1;     // title (mandatory for searchable modules), artist, owner, …
  bool required = 2;
}

message AuthRegisterDetails {
  string jwks_uri = 1;    // URL Kithara fetches for login-JWT verify
  string jwks_json = 2;   // optional inline JWKS snapshot at Register
}

message ClientRegisterDetails {
  string auth_mode = 1;                    // "user-aware" | "static"
  repeated string permission_ceiling = 2;  // static modules only; max rights for managed users
}

message RegisterResponse {
  // Populated in bootstrap mode `auto` (private mesh). Empty in `preshared`.
  string client_certificate_pem = 1;
  string client_private_key_pem = 2;
  string ca_certificate_pem = 3;
  // Non-secret metadata (safe in both modes)
  string ca_thumbprint = 4;
  int64 certificate_expires_unix = 5;
}

// Identity after Register is the mTLS client certificate — no join_secret here.
message HeartbeatRequest {
  string slug = 1;
}

message HeartbeatResponse {
  bool ok = 1;
  int64 next_heartbeat_after_seconds = 2;
}
```

## Dial rules

| Direction | When |
|-----------|------|
| **Module → Kithara** | `Register`, `Heartbeat`, storage put/get |
| **Kithara → module** | Each work RPC (`Search`, `StartTrack`, `Authenticate`, `SeedAdmin`, …) as a **fresh dial** to `grpc_advertise_address` |

Per-call dials keep operations atomic: one RPC = one span, one auth decision, easier timeouts and least-privilege checks. No long-lived command stream from module to Kithara for work.

## Channel security

1. First contact: `Register` with **join secret**.
2. Cert material depends on ModuleChannel bootstrap mode (`BARDIE_MODULE_MTLS_BOOTSTRAP`):
   - **`auto`** (default for private Compose/LAN): host **issues** a client cert and returns PEM fields on `RegisterResponse`. **Not for public networks** — private keys travel on the wire.
   - **`preshared`**: operator pre-places CA + module client cert/key offline; response PEM key fields stay **empty**; clients must not require them.
3. After pairing, the **whole gRPC surface** (both directions) uses **mTLS**. `Heartbeat` renews liveness (and may later rotate certs); it does **not** carry the join secret.

The join secret is only the bootstrap; it is not a standing impersonation key for work RPCs once mTLS is up.

## Rules

| Rule | Why |
|------|-----|
| **All modules Register over gRPC** | One join surface; UI modules are not “REST-only citizens” |
| **Join secret required on Register** | Bootstrap identity for source, auth, client |
| **`oneof details` matches `kind`** | JWKS / search schema / client auth mode stay typed without parallel RPCs |
| **Capabilities advertised at Register** | Kithara routes only what the module claims (e.g. auth `seedAdmin`, source `pause`) |
| **Static clients advertise a permission ceiling** | Managed users cannot be granted rights above what the module declared at handshake |
| **Heartbeat is mTLS-only** | No join secret on the steady-state path |
| **REST `/api` is for end users** | Client modules call REST to turn SPI into UX — not to join the mesh |

Work RPCs live on **per-kind contracts** the module hosts at `grpc_advertise_address` (Source/Auth work protos are separate — not part of this freeze).

## Client modules

Same `Register` as everyone else: join secret + `kind=CLIENT` + `ClientRegisterDetails` (`user-aware` \| `static` + permission ceiling when static). Day-to-day Struna control still uses REST `/api` — see [clients](../domains/clients.md).

## Observability

Each work RPC is its own client call from Kithara → module: propagate W3C `traceparent`, record module slug + RPC name. Module OTel names stay `bardie.source.*` / `bardie.auth.*` / `bardie.plume` (etc.).

**Related:** [grpc-source-module](grpc-source-module.md) · [grpc-auth-adapter](grpc-auth-adapter.md) · [clients](../domains/clients.md) · [ADR 003](../adrs/003-grpc-control-plane.md)

**Read next:** [grpc-source-module.md](grpc-source-module.md)
