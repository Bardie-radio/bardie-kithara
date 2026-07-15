# gRPC Source Module Contract (sketch)

```protobuf
service SourceModule {
  rpc Register(RegisterRequest) returns (RegisterResponse);
  rpc Health(HealthRequest) returns (HealthResponse);
  rpc Search(SearchRequest) returns (SearchResponse);
  rpc CreateInstance(CreateInstanceRequest) returns (CreateInstanceResponse);
  rpc StopInstance(StopInstanceRequest) returns (StopInstanceResponse);
  rpc InstanceStatus(InstanceStatusRequest) returns (stream InstanceStatusEvent);
}
```

## Key messages

```protobuf
message CreateInstanceRequest {
  string track_ref = 1;  // module-specific (URL, tune id, …)
  string struna_id = 2;
}

message CreateInstanceResponse {
  string instance_id = 1;
  string socket_path = 2;  // Unix domain socket
}

message InstanceStatusEvent {
  string instance_id = 1;
  InstanceState state = 2;
  string title = 3;
  string artist = 4;
}
```

## Requirements

- Propagate W3C `traceparent` on all RPCs
- Export OTLP with `service.name=bardie.source.<module>`
- Enforce parallel instance limits internally

**Related:** [domains/source-modules.md](../domains/source-modules.md) · [ADR 003](../adrs/003-grpc-control-plane.md) · [ADR 004](../adrs/004-source-instance-socket-audio-plane.md)

**Read next:** [grpc-auth-adapter.md](grpc-auth-adapter.md)
