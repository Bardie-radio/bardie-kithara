# gRPC Auth Adapter Contract (sketch)

Auth adapters (**Bes**, **Argus**, **Hecate**, …) speak **one** gRPC contract to Kithara. Every adapter is a separate container.

**Unified token protocol: JWT.** Modules authenticate/verify and **return access + refresh JWTs** (mint their own, or forward a provider’s — Argus forwards OIDC tokens). Kithara stores users/bindings, verifies access JWTs with the module’s registered JWKS, and does **not** mint user JWTs.

There is **no** protocol-specific RPC. Redirect callbacks, form posts, and ceremony payloads all arrive as an opaque bag on `Authenticate`.

```protobuf
service AuthAdapter {
  rpc Register(RegisterRequest) returns (RegisterResponse);
  rpc Health(HealthRequest) returns (HealthResponse);
  rpc GetProviders(GetProvidersRequest) returns (GetProvidersResponse);
  rpc Authenticate(AuthenticateRequest) returns (AuthenticateResponse);
  rpc Refresh(RefreshRequest) returns (RefreshResponse);
  // User + binding persistence goes through Kithara — adapters do not open a DB.
}
```

Per-request `ValidateToken` against the module is **not** the hot path — Kithara verifies JWTs locally using JWKS from `Register` (or IdP JWKS URL Argus supplies).

## Key messages

```protobuf
message RegisterRequest {
  string slug = 1;
  string join_secret = 2;
  string jwks_uri = 3;       // or inline JWKS; Argus may point at the IdP
  // …
}

message ProviderDescriptor {
  string id = 1;           // provider slug (bes, argus, hecate, …)
  string display_name = 2;
  string ui_mode = 3;      // form_schema, redirect, …
  // Opaque client hints: form fields, authorize URL, etc.
}

message AuthenticateRequest {
  string provider_id = 1;
  map<string, string> payload = 2;  // credentials, callback params, ceremony data, …
}

message AuthenticateResponse {
  bool allowed = 1;
  string external_subject = 2;
  repeated string roles = 3;
  map<string, string> entities = 4;
  bytes binding_payload = 5;
  bool ensure_user = 6;
  string access_token = 7;   // JWT — minted by module or forwarded (e.g. OIDC)
  string refresh_token = 8;  // opaque to Kithara; module owns refresh semantics
  string token_type = 9;     // "Bearer"
  int64 expires_in = 10;
}

message RefreshRequest {
  string provider_id = 1;
  string refresh_token = 2;
}

message RefreshResponse {
  bool allowed = 1;
  string access_token = 2;
  string refresh_token = 3;
  int64 expires_in = 4;
}
```

Exact field shapes are sketch-level; invariants:

1. **JWT in, JWT out** for user API credentials.
2. **Module owns issue + refresh**; Kithara verifies and authorizes.
3. **Kithara owns** user DB rows when the module asks to store them.

## Registration

Adapters join with a **join secret** (same pattern as source modules) and publish **JWKS** (or a JWKS URI) so Kithara can verify their JWTs. gRPC stays internal-only. Image/Compose: `bes`, `argus`, `hecate`. OTel: `bardie.auth.<slug>`.

## How modules use the same RPCs

| Module | `GetProviders` | `Authenticate` / tokens |
|--------|----------------|-------------------------|
| **Bes** | `form_schema` | Verifies password; **mints** JWT (+ refresh) |
| **Argus** | `redirect` + authorize URL | Completes OIDC; **forwards** IdP access/refresh JWTs (refresh via IdP) |
| **Hecate** | ceremony hints | Completes WebAuthn; **mints** JWT (+ refresh) |

**Related:** [domains/auth-adapters.md](../domains/auth-adapters.md) · [interfaces/auth.md](auth.md) · [ADR 007](../adrs/007-auth-adapter-modules.md)

**Read next:** [uri-routing.md](uri-routing.md)
