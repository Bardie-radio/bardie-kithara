# System Context (C4 Level 1)

```mermaid
flowchart TB
  subgraph users [Users]
    Listener[Listener]
    DJ[DJ / Owner]
  end
  subgraph bardie [Bardie]
    Kithara[Kithara]
    Plume[Plume]
  end
  subgraph modules [Modules]
    Src[Source Modules]
    Auth[Auth Adapters]
  end
  subgraph external [External]
    Players[VLC VRChat]
    OTel[OTel Collector]
  end
  DJ --> Plume
  Listener --> Players
  Plume --> Kithara
  Players --> Kithara
  Kithara --> Src
  Kithara --> Auth
  bardie --> OTel
  modules --> OTel
```

Bardie is a **self-hosted modular audio broadcast platform**. Users create **Strunas** (streams), queue music via source modules, and listen via legacy players or Plume.

**Org overview:** [Bardie-radio/.github/docs/architecture](https://github.com/Bardie-radio/.github/tree/main/docs/architecture)

**Read next:** [02-container-diagram.md](02-container-diagram.md)
