# ModuleChannel (mTLS + participant library)

Kithara (and future external hosts) embed **`Bardie.Module.Channel`** for module gRPC channel security. Modules (Bes, Magpie, …) embed the same package for **manifest identity**, Register/Heartbeat, and work-port TLS. Mesh join RPCs stay host-owned; **crypto, bootstrap policy, and static module identity live in the library** so embedders do not reinvent Kestrel/GrpcChannel wiring.

**Library home:** [`libs/Bardie.Module.Channel`](../../../libs/Bardie.Module.Channel/README.md) · contracts: [`Bardie.Contracts`](../../../libs/Bardie.Contracts/README.md)

## Why it exists

Modules dial the host to `Register`, then speak mTLS for Heartbeat and work RPCs. Auth/source orchestrator libraries reuse the same outbound dial helpers. Shipping mTLS as a packable library keeps Bardie Compose and outside hosts on one trust story ([org 07](https://github.com/Bardie-radio/.github/blob/main/profile/docs/architecture/07-modules-beyond-bardie.md)).

## Pack + consume (no proto copies)

| Context | How modules reference libs |
|---------|----------------------------|
| Multi-root workspace / Local Compose sibling layout | If `../kithara/libs` exists → **`ProjectReference`** (Bes `Directory.Build.props`) |
| Standalone CI / published consumers | **`PackageReference`** to versioned `Bardie.Contracts` + `Bardie.Module.Channel` (`0.1.0`) |

Do **not** git-submodule Kithara, copy `.proto`/`.cs` into module repos, or path-include protos from another repo in a module csproj.

## Module manifest (static identity)

Each module ships one **`module.manifest.json`**. ModuleChannel loads **generic** identity only — slug, kind, capabilities, display name, OTel name. It does **not** model Bardie auth/source/client Register bags.

```json
{
  "slug": "bes",
  "kind": "auth",
  "displayName": "Bes",
  "otelServiceName": "bardie.auth.bes",
  "capabilities": ["seedAdmin"]
}
```

| Field | Who owns | Notes |
|-------|----------|--------|
| `slug`, `kind`, `capabilities` | Manifest (ModuleChannel) | Defaults for core `RegisterRequest` fields |
| `otelServiceName`, `displayName` | Manifest (ModuleChannel) | OTel / ops |
| Kind-specific `oneof details` (JWKS, search fields, permission ceiling) | **Module / host customizer** | `IModuleRegisterRequestCustomizer` — not typed on the shared manifest |
| Extra JSON keys | Opaque `Extensions` | Preserved for module-local parsing; ModuleChannel ignores them |
| Join secret | **Env only** | Never in the manifest file |
| `grpc_advertise_address` | **Env / Compose** | Deployment-specific (`GRPC_ADVERTISE_ADDRESS`) |
| `MODULE_SLUG_OVERRIDE` | **Env** | Overrides manifest slug when community slugs collide |

Loader: `ModuleManifestLoader` + `BuildRegisterRequest(joinSecret, advertiseAddress, customizers?)`.

## Bardie capabilities vocabulary (host convention)

Capabilities are **open strings** on the wire. ModuleChannel never interprets them. The tables below are **Bardie host** conventions (Kithara’s Auth Orchestrator gates RPCs on these values via `Bardie.Orchestrator.Auth.WellKnownAuthCapabilities` / host `WellKnownSourceCapabilities`) — documented here so module authors see the vocabulary next to Register.

| Put in `capabilities[]` | Keep elsewhere |
|-------------------------|----------------|
| Optional RPCs / behaviours that some modules of the same kind omit | `kind` (`source` / `auth` / `client`) |
| Host routing gates (“may I call SeedAdmin / PauseTrack / Search fan-out?”) | Register `details.source.searchFields` (form schema) — set by module customizer |
| | Register `details.auth` JWKS — runtime customizer |
| | Register `details.client.authMode` + `permissionCeiling` — module customizer |

### MVP (advertise what you implement)

| Kind | Capability | Meaning | Who |
|------|------------|---------|-----|
| **source** | `search` | Implements `Search`; eligible for `/api/search` fan-out | Magpie yes; Starling typically no |
| **source** | `play` | Implements `StartTrack` / `StopTrack` (PCM to session FIFO) | Magpie, Starling, Catbird |
| **source** | `pause` | Implements `PauseTrack` / `ResumeTrack` without tearing down the job | Magpie yes; **Starling omits** |
| **auth** | `seedAdmin` | Host may call `SeedAdmin` when user DB empty | **Bes yes**; Argus typically **no** |

### Auth — reserved (document now; advertise only when implemented)

| Capability | Why useful |
|------------|------------|
| `selfRegister` | Open signup via Authenticate (Bes “register” form) without operator seed |
| `passwordReset` | Host/UI can expose reset; module owns ceremony in the opaque `Authenticate` bag |

**Not a module capability:** account linking stays **Kithara’s story** (explicit multi-provider link in the user DB / orchestrator). Auth adapters only prove identity for their provider — they do not advertise `accountLink`.

### Do not put in `capabilities[]`

- `authenticate` / `refresh` / `getProviders` / `health` — core auth/source contract; every well-known auth module must speak them
- `form_schema` / `redirect` — discovery `ui_mode` on `GetProviders`, not Register caps
- Permission strings (`create_struna`, …) — **`client.permissionCeiling`** on Register (customizer)
- Source type labels (`youtube`, `live`, `files`) — do not invent these as capabilities
- `PrepareTrack` — out of MVP until the RPC exists
- `accountLink` — Kithara-owned linking, not an adapter Register flag

Auth-focused prose also lives in [grpc-auth-adapter.md](../interfaces/grpc-auth-adapter.md).

## Bootstrap modes

| Mode | Safe on | Register response |
|------|---------|-------------------|
| **`auto`** | Private Docker/Compose overlay, trusted LAN | May include client cert + **private key** PEMs after join-secret check |
| **`preshared`** | Public / untrusted paths | **No private keys on the wire.** Pre-place CA + module client material via a secure offline channel before process start |

**Do not use `auto` across the public internet.** Prefer `preshared` whenever module gRPC leaves a private overlay.

Env knobs: [configuration.md](configuration.md) (`BARDIE_MODULE_MTLS_BOOTSTRAP`, `BARDIE_GRPC_TLS_DATA_PATH`, `BARDIE_MODULE_MTLS_PRESHARED_DIR`).

## Host vs library vs participant

| Library (host) | Host (Kithara) | Library (participant) |
|----------------|----------------|-------------------------|
| Cert issue/validate, Kestrel helper, interceptor, outbound dial factory | Module Registry **service**, `BARDIE_JOIN_SECRETS`, port binding, catalog projection | Manifest load, Register→PEM persist, Heartbeat loop, work-port Kestrel with mesh CA trust |

## Related

- [grpc-module-registry.md](../interfaces/grpc-module-registry.md) · [grpc-auth-adapter.md](../interfaces/grpc-auth-adapter.md) · [security-audit-module-mesh.md](security-audit-module-mesh.md) · [configuration.md](configuration.md)

**Read next:** [grpc-module-registry.md](../interfaces/grpc-module-registry.md)
