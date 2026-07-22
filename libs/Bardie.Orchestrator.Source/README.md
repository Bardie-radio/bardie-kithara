# Bardie.Orchestrator.Source

Source module **orchestrator** library for Bardie hosts (Kithara today; external hosts later).

**Package id:** `Bardie.Orchestrator.Source` · **Version:** `0.1.0` · **TFM:** `net10.0`

Depends on [`Bardie.Module.Channel`](../Bardie.Module.Channel/README.md) (Contracts transitively).

## What it owns

- Source module catalog
- Host port `IBlobStorage` for shared library blob access
- Dial helpers via Module.Channel (Search / StartTrack land with Phase 3)

## Consume

```csharp
services.AddSourceModuleOrchestrator(registerModuleChannel: false);
// when Auth orch already called AddModuleChannel — avoid double-register
// host must register IBlobStorage
```

Pack: `dotnet pack libs/Bardie.Orchestrator.Source/Bardie.Orchestrator.Source.csproj -c Release`
