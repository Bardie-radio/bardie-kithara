# Bardie.Module.Hosting

ASP.NET Core **bootstrap kit** for Bardie module participants (Bes, Magpie, …).

**Package id:** `Bardie.Module.Hosting` · **Version:** `0.1.0` · **TFM:** `net10.0`

Depends on [`Bardie.Module.Channel`](../Bardie.Module.Channel/README.md). Owns Bardie Compose env aliases (`KITHARA_*` / `BARDIE_*`); Channel stays mesh-only.

## Consume

```csharp
var manifest = builder.AddBardieModuleHosting(o => o.ServerDnsNames = ["bes", "localhost"]);
// …
var app = builder.Build();
await app.EnsureModuleParticipantServerCertificateAsync();
app.MapModuleHostingEndpoints();
```

| API | Role |
|-----|------|
| `BardieComposeParticipantEnv.Apply` | Map Compose aliases onto `ModuleParticipantOptions` |
| `ModuleHostingPorts` | Resolve work / HTTP listen ports |
| `AddBardieModuleHosting` | Participant DI + aliases + Kestrel + optional OTel |
| `EnsureModuleParticipantServerCertificateAsync` | Work-port server cert before listen |
| `MapModuleHostingEndpoints` | `/healthz` + `/` identity JSON |

Pack: `dotnet pack libs/Bardie.Module.Hosting/Bardie.Module.Hosting.csproj -c Release`
