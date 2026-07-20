# ModuleChannel (mTLS library)

Kithara (and future external hosts) embed **`Bardie.ModuleChannel`** for module gRPC channel security. Mesh join RPCs stay host-owned; **crypto and bootstrap policy live in the library** so embedders do not reinvent Kestrel/GrpcChannel wiring.

**Library home:** [`libs/Bardie.ModuleChannel`](../../../libs/Bardie.ModuleChannel/README.md)

## Why it exists

Modules dial the host to `Register`, then speak mTLS for Heartbeat and work RPCs. Auth/source orchestrator libraries reuse the same outbound dial helpers. Shipping mTLS as a packable library keeps Bardie Compose and outside hosts on one trust story ([org 07](https://github.com/Bardie-radio/.github/blob/main/profile/docs/architecture/07-modules-beyond-bardie.md)).

## Bootstrap modes

| Mode | Safe on | Register response |
|------|---------|-------------------|
| **`auto`** | Private Docker/Compose overlay, trusted LAN | May include client cert + **private key** PEMs after join-secret check |
| **`preshared`** | Public / untrusted paths | **No private keys on the wire.** Pre-place CA + module client material via a secure offline channel before process start |

**Do not use `auto` across the public internet.** Prefer `preshared` whenever module gRPC leaves a private overlay.

Env knobs: [configuration.md](configuration.md) (`BARDIE_MODULE_MTLS_BOOTSTRAP`, `BARDIE_GRPC_TLS_DATA_PATH`, `BARDIE_MODULE_MTLS_PRESHARED_DIR`).

## Host vs library

| Library | Host (Kithara) |
|---------|----------------|
| Cert issue/validate, Kestrel helper, interceptor policy, outbound channel factory | Module Registry **service**, `BARDIE_JOIN_SECRETS`, port binding, catalog projection into orch libs |

## Related

- [grpc-module-registry.md](../interfaces/grpc-module-registry.md) · [security-audit-module-mesh.md](security-audit-module-mesh.md) · [configuration.md](configuration.md)

**Read next:** [grpc-module-registry.md](../interfaces/grpc-module-registry.md)
