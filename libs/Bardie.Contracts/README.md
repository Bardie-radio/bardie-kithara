# Bardie.Contracts

Versioned **gRPC contract package** for Bardie module authors. Single source of truth for Module Registry join, Auth Adapter / Source Module work RPCs, and Kithara-hosted BlobStorage / Library services.

**Package id:** `Bardie.Contracts` · **Version:** `0.1.0` · **TFM:** `net10.0`

| Proto | Namespace | Docs |
|-------|-----------|------|
| `Protos/module_registry.proto` | `Bardie.Modules.V1` | [grpc-module-registry](../../docs/architecture/interfaces/grpc-module-registry.md) |
| `Protos/auth_adapter.proto` | `Bardie.Auth.V1` | [grpc-auth-adapter](../../docs/architecture/interfaces/grpc-auth-adapter.md) |
| `Protos/source_module.proto` | `Bardie.Source.V1` | [grpc-source-module](../../docs/architecture/interfaces/grpc-source-module.md) |
| `Protos/blob_storage.proto` | `Bardie.Storage.V1` | [grpc-blob-storage](../../docs/architecture/interfaces/grpc-blob-storage.md) |
| `Protos/library.proto` | `Bardie.Library.V1` | [grpc-library](../../docs/architecture/interfaces/grpc-library.md) |

C# stubs are generated with `GrpcServices=Both` (client + server). `.proto` files are also packed under `protos/` for non-C# generators.

**Blob key layout:** `tunes/<source_slug>/…` under the storage driver root (see blob-storage docs).

## Consume

| Context | How |
|---------|-----|
| Multi-root / sibling `kithara/libs` | `ProjectReference` to this project (Bes uses `Directory.Build.props` when `../kithara/libs` exists) |
| Standalone CI / published consumers | `PackageReference` to `Bardie.Contracts` `0.1.0` |

Pack with ModuleChannel: `dotnet pack libs/Bardie.Contracts/Bardie.Contracts.csproj libs/Bardie.ModuleChannel/Bardie.ModuleChannel.csproj -c Release`.
