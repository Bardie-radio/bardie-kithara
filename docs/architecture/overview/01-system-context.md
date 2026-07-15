# System Context (C4 Level 1)

<!-- mermaid-source: diagrams/system-context.mmd -->
```mermaid
flowchart TB
  subgraph users [Users]
    Listener[Listener]
    DJ[DJ / Owner]
  end
  subgraph bardie [Bardie]
    Kithara[Kithara]
    Clients[Client Modules]
    Src[Source Modules]
    Auth[Auth Adapters]
  end
  subgraph external [External]
    Players[Player]
    OTel[OTel Collector]
  end
  DJ --> Clients
  Listener --> Players
  Clients --> Kithara
  Players --> Kithara
  Kithara --> Src
  Kithara --> Auth
  bardie --> OTel
```

**Kithara** is the core of Bardie: Struna (stream) lifecycle, module orchestration, and ICY audio output. DJs create and control Strunas through **client modules**; listeners can also tune in with ordinary players.

This page is the Kithara-side view. Whole-ecosystem actors and journeys live in the [org ecosystem context](https://github.com/Bardie-radio/.github/blob/main/profile/docs/architecture/02-ecosystem-context.md).

**Related:** [glossary](../glossary.md) · [org architecture hub](https://github.com/Bardie-radio/.github/tree/main/profile/docs/architecture)

**Read next:** [02-internal-structure.md](02-internal-structure.md)
