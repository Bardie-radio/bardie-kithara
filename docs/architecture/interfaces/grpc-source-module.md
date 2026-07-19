# gRPC Source Module Contract (sketch)

```protobuf
service SourceModule {
  rpc Register(RegisterRequest) returns (RegisterResponse);
  rpc Health(HealthRequest) returns (HealthResponse);
  rpc Search(SearchRequest) returns (SearchResponse);
  rpc StartTrack(StartTrackRequest) returns (StartTrackResponse);
  rpc StopTrack(StopTrackRequest) returns (StopTrackResponse);
  rpc TrackStatus(TrackStatusRequest) returns (stream TrackStatusEvent);
}
```

## Key messages

```protobuf
message RegisterRequest {
  string slug = 1;              // human id; may be overridden by operator env
  string join_secret = 2;
  repeated string capabilities = 3;
  string grpc_advertise_address = 4;
  // search_fields: advertised schema for structured Search (title mandatory if search)
}

message StartTrackRequest {
  string struna_id = 1;
  string track_ref = 2;         // module-specific (URL, tune id, YouTube link, stream URI, …)
  string fifo_path = 3;         // Kithara-owned session FIFO to write PCM into
}

message StartTrackResponse {
  string track_job_id = 1;
}

message TrackStatusEvent {
  string track_job_id = 1;
  TrackState state = 2;         // running, ended, error
  string title = 3;
  string artist = 4;
}
```

## Audio requirements

- Write **canonical PCM** (MVP: s16le / 48 kHz / stereo) to `fifo_path`
- Do not expect to own the listen side of the FIFO
- Informal prewarm allowed; no MVP prepare RPC

## Registration security

Authenticate with a **join secret**. gRPC `:5000` on Kithara is internal-only. Slug spoofing is rejected without a matching secret/claim.

## Observability

- Propagate W3C `traceparent` on all RPCs
- Export OTLP with `service.name=bardie.source.<slug>` (e.g. `bardie.source.magpie`)
- Enforce parallel track-job limits internally

**Related:** [domains/source-modules.md](../domains/source-modules.md) · [ADR 003](../adrs/003-grpc-control-plane.md) · [ADR 004](../adrs/004-source-instance-socket-audio-plane.md)

**Read next:** [grpc-auth-adapter.md](grpc-auth-adapter.md)
