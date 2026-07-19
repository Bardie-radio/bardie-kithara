# gRPC Source Module Contract (sketch)

Source modules (**Magpie**, **Starling**, **Catbird**, …) host the **work** RPCs below. They **dial Kithara** only to join via [Module Registry](grpc-module-registry.md) (not via `Register` on this service).

```protobuf
service SourceModule {
  rpc Health(HealthRequest) returns (HealthResponse);
  rpc Search(SearchRequest) returns (SearchResponse);
  rpc StartTrack(StartTrackRequest) returns (StartTrackResponse);
  rpc StopTrack(StopTrackRequest) returns (StopTrackResponse);
  rpc PauseTrack(PauseTrackRequest) returns (PauseTrackResponse);
  rpc ResumeTrack(ResumeTrackRequest) returns (ResumeTrackResponse);
  rpc TrackStatus(TrackStatusRequest) returns (stream TrackStatusEvent);
}
```

`PauseTrack` / `ResumeTrack` are part of the **common** contract. Modules that cannot pause (e.g. Starling) omit the `pause` capability at Registry `Register` and return a clear error if called. Magpie **does** advertise and implement pause.

## Key messages

```protobuf
message StartTrackRequest {
  string struna_id = 1;
  string track_ref = 2;         // module-specific (URL, tune id, YouTube link, stream URI, …)
  string audio_endpoint = 3;    // Kithara-owned session path on the shared audio volume (FIFO / socket)
}

message StartTrackResponse {
  string track_job_id = 1;
}

message TrackStatusEvent {
  string track_job_id = 1;
  TrackState state = 2;         // running, paused, ended, error
  string title = 3;
  string artist = 4;
}
```

## Capabilities (Registry)

Advertised at Module Registry `Register` — not inventing source-type labels:

| Capability | Meaning |
|------------|---------|
| `search` | Implements `Search` |
| `play` | Implements `StartTrack` / `StopTrack` |
| `pause` | Implements `PauseTrack` / `ResumeTrack` without tearing down the job |

## Audio requirements

- Write **canonical PCM** (MVP: s16le / 48 kHz / stereo) to the session endpoint Kithara created
- Do not own the listen side; Neck/FFmpeg reads it for the Struna life
- Endpoint lives on the **shared Compose volume**; path is passed in `StartTrack` — see [ADR 004](../adrs/004-source-instance-socket-audio-plane.md)
- Informal prewarm allowed; no MVP `PrepareTrack` RPC

## Blob access

Library put/get goes through **Kithara’s storage API** (modules dial Kithara) — drivers stay on Kithara only ([storage](../domains/storage.md)).

## Observability

- Propagate W3C `traceparent` on all RPCs
- Export OTLP with `service.name=bardie.source.<slug>` (e.g. `bardie.source.magpie`)
- Enforce parallel track-job limits internally

**Related:** [grpc-module-registry](grpc-module-registry.md) · [domains/source-modules.md](../domains/source-modules.md) · [ADR 003](../adrs/003-grpc-control-plane.md) · [ADR 004](../adrs/004-source-instance-socket-audio-plane.md)

**Read next:** [grpc-auth-adapter.md](grpc-auth-adapter.md)
