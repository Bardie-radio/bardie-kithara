# Runtime Data Flow

<!-- mermaid-source: docs/architecture/diagrams/runtime-data-flow.mmd -->
```mermaid
flowchart TB
  subgraph control [Control Plane gRPC]
    API -->|StartTrack| SrcMod[Source Module]
    API -->|Authenticate| AuthMod[Auth Provider]
  end
  subgraph audio [Audio Plane FIFO]
    SrcMod -->|PCM write| FIFO[Session FIFO]
    Silence[Silence feeder] --> FIFO
    FIFO --> FF[FFmpeg]
    FF -->|pipe| SS[Stream Server]
  end
```

Two planes stay separate:

| Plane | Transport | Data |
|-------|-----------|------|
| **Control** | gRPC (+ REST for clients) | Commands, auth identity proof, status |
| **Audio** | Named FIFO → FFmpeg pipe | Canonical PCM / encoded audio |

HTTP is for clients ↔ Kithara API, OIDC callback, and listeners ↔ Stream Server only.

**Related:** [ADR 003](../adrs/003-grpc-control-plane.md) · [ADR 004](../adrs/004-source-instance-socket-audio-plane.md)

**Read next:** [../domains/source-instances.md](../domains/source-instances.md)
