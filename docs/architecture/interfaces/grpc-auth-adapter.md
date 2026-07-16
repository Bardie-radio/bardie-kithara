# gRPC Auth Adapter Contract (sketch)

```protobuf
service AuthAdapter {
  rpc Register(RegisterRequest) returns (RegisterResponse);
  rpc Health(HealthRequest) returns (HealthResponse);
  rpc GetProviders(GetProvidersRequest) returns (GetProvidersResponse);
  rpc ValidateToken(ValidateTokenRequest) returns (ValidateTokenResponse);
  rpc Authenticate(AuthenticateRequest) returns (AuthenticateResponse);
  rpc GetLoginUi(GetLoginUiRequest) returns (GetLoginUiResponse);
}
```

## Key messages

```protobuf
message ProviderDescriptor {
  string id = 1;
  string type = 2;  // password, oidc, …
  string display_name = 3;
  string ui_mode = 4;  // form_schema, embed, redirect
  // form fields, redirect URL, etc.
}

message ValidateTokenResponse {
  string subject = 1;
  repeated string roles = 2;
  repeated string scopes = 3;
}

message AuthenticateRequest {
  string provider_id = 1;
  map<string, string> credentials = 2;
}

message AuthenticateResponse {
  string token = 1;
  int64 expires_at = 2;
}
```

## MVP: login+password adapter *(name TBD)*

Implements `Authenticate` + `ValidateToken` + `GetProviders` with `uiMode=form_schema`.

**Related:** [domains/auth-adapters.md](../domains/auth-adapters.md) · [interfaces/auth.md](auth.md) · [ADR 007](../adrs/007-auth-adapter-modules.md)

**Read next:** [uri-routing.md](uri-routing.md)
