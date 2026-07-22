# Bardie.Orchestrator.Source

Source module **orchestrator** library for Bardie hosts (Kithara today; external hosts later).

**Package id:** `Bardie.Orchestrator.Source` · **Version:** `0.1.0` · **TFM:** `net10.0`

Depends on [`Bardie.Contracts`](../Bardie.Contracts/README.md) + [`Bardie.Module.Channel`](../Bardie.Module.Channel/README.md).

## What it owns

- Source module catalog
- Host port `IBlobStorage` for shared library blob access
- Capability vocabulary (`WellKnownSourceCapabilities`)
- Per-call dials: `SearchAsync`, `StartTrackAsync`, `StopTrackAsync`, `PauseTrackAsync`, `ResumeTrackAsync`, `TrackStatusAsync`

## Consume

```csharp
services.AddSourceModuleOrchestrator(registerModuleChannel: false);
// when Auth orch already called AddModuleChannel — avoid double-register
// host must register IBlobStorage
```

Pack: `dotnet pack libs/Bardie.Orchestrator.Source/Bardie.Orchestrator.Source.csproj -c Release`
