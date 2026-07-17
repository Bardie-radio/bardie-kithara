# gRPC Auth Adapter Contract (sketch)

External auth adapters (OIDC, …) speak gRPC to Kithara. The **local** password provider is in-process and uses the same conceptual operations against Kithara’s user/binding store.

```protobuf
service AuthAdapter {
  rpc Register(RegisterRequest) returns (RegisterResponse);
  rpc Health(HealthRequest) returns (HealthResponse);
  rpc GetProviders(GetProvidersRequest) returns (GetProvidersResponse);
  rpc Authenticate(AuthenticateRequest) returns (AuthenticateResponse);
  rpc ExchangeOidcCode(ExchangeOidcCodeRequest) returns (ExchangeOidcCodeResponse);
  // Binding read/write goes through Kithara-owned APIs / orchestrator — adapters
  // do not open a direct DB connection.
}
```

`ValidateToken` is **not** used for client API Bearers — those are Kithara JWTs. Adapters prove identity; Kithara issues sessions.

## Key messages

```protobuf
message ProviderDescriptor {
  string id = 1;           // provider slug
  string type = 2;         // password, oidc, …
  string display_name = 3;
  string ui_mode = 4;      // form_schema, redirect
  // form fields, authorize URL, etc.
}

message AuthenticateRequest {
  string provider_id = 1;
  map<string, string> credentials = 2;
}

message AuthenticateResponse {
  string external_subject = 1;
  map<string, string> claims = 2;
  // No client JWT here — Kithara mints that after success
}

message ExchangeOidcCodeRequest {
  string provider_id = 1;
  string code = 2;
  string redirect_uri = 3;
}
```

## Registration

Adapters join with a **Compose env join secret** (same pattern as source modules). gRPC stays internal-only.

## MVP local provider

In-process `Authenticate` + `GetProviders` with `uiMode=form_schema`. Writes password material into `UserAuthBinding` for provider slug `local`.

## OIDC adapter *(name TBD, v0.2)*

Implements `GetProviders` + `ExchangeOidcCode` (and related). Callback HTTP is on Kithara; adapter only talks IdP from the internal network.

**Related:** [domains/auth-adapters.md](../domains/auth-adapters.md) · [interfaces/auth.md](auth.md) · [ADR 007](../adrs/007-auth-adapter-modules.md)

**Read next:** [uri-routing.md](uri-routing.md)
