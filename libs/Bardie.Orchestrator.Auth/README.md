# Bardie.Orchestrator.Auth

Auth module **orchestrator** library for Bardie hosts (Kithara today; external hosts later).

**Package id:** `Bardie.Orchestrator.Auth` · **Version:** `0.1.0` · **TFM:** `net10.0`

Depends on [`Bardie.Contracts`](../Bardie.Contracts/README.md) + [`Bardie.Module.Channel`](../Bardie.Module.Channel/README.md).

## What it owns

- Auth module catalog (slug, JWKS, capabilities)
- Discovery merge (`GetProviders`), route `Authenticate` / `Refresh` / `SeedAdmin`
- Host port `IAuthPersistence` for user + binding persistence
- Dials modules via Module.Channel mTLS helpers

Bardie-only extras (guests, join secrets, REST BFF) stay in the Kithara host wrappers — see [org 07](https://github.com/Bardie-radio/.github/blob/main/profile/docs/architecture/07-modules-beyond-bardie.md).

## Consume

```csharp
services.AddAuthModuleOrchestrator();
// host must register IAuthPersistence
```

Pack: `dotnet pack libs/Bardie.Orchestrator.Auth/Bardie.Orchestrator.Auth.csproj -c Release`
