# Bardie.Contracts

Versioned **gRPC contract package** for Bardie module authors. Single source of truth for Module Registry join and Auth Adapter work RPCs.

**Package id:** `Bardie.Contracts` · **Version:** `0.1.0` · **TFM:** `net10.0`

| Proto | Namespace | Docs |
|-------|-----------|------|
| `Protos/module_registry.proto` | `Bardie.Modules.V1` | [grpc-module-registry](../../docs/architecture/interfaces/grpc-module-registry.md) |
| `Protos/auth_adapter.proto` | `Bardie.Auth.V1` | [grpc-auth-adapter](../../docs/architecture/interfaces/grpc-auth-adapter.md) |

C# stubs are generated with `GrpcServices=Both` (client + server). `.proto` files are also packed under `protos/` for non-C# generators.

## Consume

| Context | How |
|---------|-----|
| Multi-root / sibling `kithara/libs` | `ProjectReference` to this project (Bes uses `Directory.Build.props` when `../kithara/libs` exists) |
| Standalone CI / published consumers | `PackageReference` to `Bardie.Contracts` `0.1.0` |

Pack with ModuleChannel: `dotnet pack libs/Bardie.Contracts/Bardie.Contracts.csproj libs/Bardie.ModuleChannel/Bardie.ModuleChannel.csproj -c Release`.

Source modules (`SourceModule`) land in a later freeze; until then Magpie scaffolds against the interface sketch.
