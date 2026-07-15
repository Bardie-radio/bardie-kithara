# Container Diagram (C4 Level 2)

```mermaid
flowchart TB
  subgraph compose [Docker Compose MVP]
    Plume[Plume :80]
    Kithara[Kithara]
    YT[YouTube Module]
    AuthLocal[auth-local]
  end
  subgraph kithara_internal [Inside Kithara]
    API[REST API]
    Neck[Neck Service]
    StreamSrv[Stream Server]
    AuthOrch[Auth Orchestrator]
  end
  Plume --> API
  Plume --> StreamSrv
  API --> AuthOrch
  API --> Neck
  Neck --> YT
  AuthOrch --> AuthLocal
  Neck --> StreamSrv
```

## Containers (MVP authenticated = 4)

| Container | Repo |
|-----------|------|
| kithara | bardie-kithara |
| plume | bardie-plume |
| youtube-module | TBD |
| auth-local | bardie-auth-local |

Streaming runs **inside kithara** — no Icecast container.

**Read next:** [03-runtime-data-flow.md](03-runtime-data-flow.md)
