# Bardie.ModuleChannel

Shared **mTLS / gRPC channel** helpers for Bardie module hosts (Kithara today; external auth/source orchestrator hosts later).

**Package id:** `Bardie.ModuleChannel` · **TFM:** `net10.0`

## What it owns

| Capability | Notes |
|------------|--------|
| CA + server cert load/generate + persist | `BARDIE_GRPC_TLS_DATA_PATH` / `TlsDataPath` |
| Issue module client certs | Used in bootstrap mode **`auto`** |
| Validate inbound client cert → slug | Heartbeat + privileged RPCs |
| Outbound `GrpcChannel` helpers | Auth/Source orchestrators dial modules |
| Kestrel bind helper | HTTPS `:5000` + allow client certificates |
| Bootstrap interceptor | Register may omit client cert; other RPCs require it |

## Bootstrap modes

| Mode | Env | Wire behaviour |
|------|-----|----------------|
| **`auto`** (default) | `BARDIE_MODULE_MTLS_BOOTSTRAP=auto` | After join-secret OK, Register may return client cert + **private key** PEMs. **Private mesh / Compose only.** |
| **`preshared`** | `BARDIE_MODULE_MTLS_BOOTSTRAP=preshared` | Operator pre-places CA + per-slug client cert/key under `BARDIE_MODULE_MTLS_PRESHARED_DIR` **before** start. Register **never** returns private keys. Use whenever gRPC may cross a public/untrusted network. |

Steady-state after Register is always mTLS. Mode only changes how certs land on disk.

Offline provision helper: `IModuleCertificateIssuer.ProvisionPresharedClientCertificate(slug)` writes files under the preshared directory (admin tooling / DummyRegistrar-style setup).

## DI

```csharp
services.AddModuleChannel(options =>
{
    options.UseMtls = true; // default
    options.BootstrapMode = ModuleChannelBootstrapMode.Auto; // or Preshared
    options.TlsDataPath = "/data/grpc-tls";
});
```

`AddAuthModuleOrchestrator()` / `AddSourceModuleOrchestrator()` call this with mTLS on by default.

## Related

- Architecture: [module-channel.md](../../docs/architecture/operations/module-channel.md)
- Registry contract: [grpc-module-registry.md](../../docs/architecture/interfaces/grpc-module-registry.md)
- Config knobs: [configuration.md](../../docs/architecture/operations/configuration.md)
- Mesh trust audit: [security-audit-module-mesh.md](../../docs/architecture/operations/security-audit-module-mesh.md)
