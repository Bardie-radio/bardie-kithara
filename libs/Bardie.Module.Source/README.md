# Bardie.Module.Source

Shared helpers for **source adapter modules** (Magpie, Starling, Catbird, …).

**Package id:** `Bardie.Module.Source` · **Version:** `0.1.0` · **TFM:** `net10.0`

Depends on [`Bardie.Contracts`](../Bardie.Contracts/README.md) + [`Bardie.Module.Channel`](../Bardie.Module.Channel/README.md). Does **not** depend on `Bardie.Module.Hosting`.

## Consume

```csharp
builder.Services.AddSourceModuleDefaults(builder.Configuration);
// optional: SourceModule:SearchFields in appsettings / env
```

| Type | Role |
|------|------|
| `SourceModuleBase` | `Health`; default Pause/Resume gate on `pause` capability |
| `SourceSearchFieldsRegisterRequestCustomizer` | Attach `Source.search_fields` on Register (`title` default) |
| `ITrackJobRegistry` / `TrackJobRegistry` | Job id lifecycle for Stop / Pause / Resume / TrackStatus |
| `IFifoAudioSink` / `FifoAudioSink` | Write PCM to Kithara session FIFO path |
| `IModuleBlobStorageClient` / `IModuleLibraryClient` | Dial Kithara BlobStorage + Library over participant mTLS |

YoutubeExplode, libav transcoder, URI-only play, and module-specific Search/StartTrack stay in the module.

Pack: `dotnet pack libs/Bardie.Module.Source/Bardie.Module.Source.csproj -c Release`
