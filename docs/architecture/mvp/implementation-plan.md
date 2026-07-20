# Implementation plan (v0.1)

Ordered build plan to bring Kithara (and the MVP module stack) alive without coupling modules to each other’s guts.

**Scope:** [v0.1-scope.md](v0.1-scope.md) · **Milestone sketch:** [v0.1-milestones.md](v0.1-milestones.md)

This page is the **how and in what order**. Milestones stay the short delivery ladder; here we expand work packages, freeze points, and modularity rules.

## Philosophy: modularity first

Kithara must not care **which** auth, source, or UI module is connected — only that each speaks the **unified contract for its type**. Modules must not depend on each other’s implementation details.


| Rule                                  | Means in practice                                                                                                                                      |
| ------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **One contract per module type**      | Source → `SourceModule` gRPC; Auth → `AuthModule` gRPC; Client → `ClientModule` gRPC + REST `/api` for UX; **all** kinds join via Module Registry gRPC |
| **Opaque payloads at the edge**       | Clients never call Bes/Magpie; Kithara routes bags and verifies tokens                                                                                 |
| **Identity by slug + join secret**    | Module swap = Compose + secret map, not Kithara code changes                                                                                           |
| **Orchestrators as libraries**        | Auth module orch + source module orch are **library-shaped** (host ports for persistence / storage / Bardie extras). Kithara is one host; outside reuse is planned — [org 07](https://github.com/Bardie-radio/.github/blob/main/profile/docs/architecture/07-modules-beyond-bardie.md) |
| **Spike code is not the model**       | Follow docs/ADRs over `Neck.cs` / prototype `Tune`/`Playlist` shapes                                                                                   |
| **Freeze the socket before the guts** | Lock proto/REST sketches enough to implement both sides, then fill behaviour                                                                           |
| **OTel from day one**                 | Wire OpenTelemetry in `Program.cs` / module entrypoints in **Phase 1** — not a Phase 8 afterthought. Auto-instrument HTTP/gRPC/EF; custom spans only where middleware is blind (Neck, FIFO, FFmpeg). |


If a feature requires Magpie to know Bes exists (or Plume to know Magpie’s ytdl quirks), the design is wrong — put the knowledge in Kithara’s orchestrators or in the shared contract.

```mermaid
flowchart TB
  subgraph freeze [Freeze early]
    Proto[gRPC contracts]
    REST[REST /api sketch]
    Data[Core data model]
  end
  subgraph kithara_core [Kithara vertical slices]
    Skeleton[Skeleton + persistence]
    Registry[Module Registry]
    AuthOrch[Auth Orchestrator]
    Neck[Neck + FIFO + FFmpeg]
    Stream[Stream Server ICY]
  end
  subgraph modules [Parallel module work after freeze]
    Bes[Bes]
    Magpie[Magpie]
    Plume[Plume]
  end
  Proto --> Registry
  Proto --> Bes
  Proto --> Magpie
  REST --> Skeleton
  REST --> Plume
  Data --> Skeleton
  Skeleton --> Registry
  Registry --> AuthOrch
  AuthOrch --> Bes
  Registry --> Neck
  Neck --> Magpie
  Neck --> Stream
  Stream --> Plume
```





## Current baseline (honest)

In-tree code is a **spike**, not a product core:


| Area    | Today                                                    | Target                                          |
| ------- | -------------------------------------------------------- | ----------------------------------------------- |
| Layout  | Controllers + singleton `NeckService`                    | Feature-first Minimal APIs + hosted supervisors |
| Models  | `Struna` without slug/access; `Tune`↔`Playlist` conflict | ADR 006 library + queue intents                 |
| Audio   | Playlist concat into FFmpeg                              | Session FIFO → FFmpeg → Stream Server           |
| Modules | None                                                     | Magpie + Bes over gRPC; Plume over REST         |


Treat spike files as reference for “FFmpeg from .NET works,” then replace — see [spike/prototype-neck-ffmpeg](../spike/prototype-neck-ffmpeg.md).

## Phase map

Phases are **dependency-ordered**. Later phases may start stubs earlier, but do not ship behaviour that bypasses an unfrozen contract.


| Phase | Name             | Outcome                                                               |
| ----- | ---------------- | --------------------------------------------------------------------- |
| **1** | Kithara skeleton | Feature layout, DB, Module Registry, join secrets, **OTel bootstrap** |
| **2** | Auth vertical | Orchestrator + Bes + JWT verify + bootstrap user path |
| **3** | Source vertical | Source protocol + Magpie proof (`Search` / `StartTrack` / FIFO write) |
| **4** | Neck + encode | Alive Struna, silence feeder, FFmpeg supervisor |
| **5** | Stream Server | `GET /stream/{slug}` ICY + listen-token gate |
| **6** | Control REST | Play/queue/search/skip/pause/delete + guest exchange |
| **7** | Plume MVP | Discovery login + control UI (optional client) |
| **8** | Compose + verify | Reference stack, join secrets, **confirm** OTLP → external collector end-to-end |


Phases 2 and 3 can run **in parallel** after Phase 0–1. Phases 4–6 are mostly Kithara. Phase 7 needs Phase 2 + enough of 6. Phase 8 needs all MVP apps green enough to compose — OTel export itself is already live from Phase 1 / each module’s first boot.

### OTel in practice (ASP.NET / modules)

You do not hand-wrap every method. Typical pattern:

1. **Bootstrap once** in `Program.cs` (or module main): OpenTelemetry SDK + OTLP exporter + `service.name=bardie.kithara` (etc.).
2. **Auto-instrumentation** for ASP.NET Core HTTP, gRPC, HttpClient, EF Core — middleware/handlers create spans for inbound/outbound calls and propagate W3C `traceparent`.
3. **Custom Activity / spans** only where auto-instrumentation is blind: Neck lifecycle, silence feeder, FFmpeg process, session FIFO attach, track-job state machines.
4. **Attributes** from [observability](../operations/observability.md): `struna.id`, `struna.slug`, `source.module`, … — never tokens/passwords.
5. If `OTEL_EXPORTER_OTLP_ENDPOINT` is unset, export can no-op or log locally; when the collector is present, traces already flow.

Same contract on Bes/Magpie/Plume from their first runnable container ([ADR 008](../adrs/008-otel-observability.md)).

---



## Phase 0 — Contract freeze

**Why first:** Magpie/Bes/Plume cannot safely implement against moving sketches. Modularity dies if each module invents its own register/auth/play shape.

### Work

1. **Own the** `.proto` **files in Kithara** and publish a **versioned contract package** for module authors — single source of truth for:
  - `ModuleRegistry` on Kithara (modules dial in; mTLS cert issued on success)
  - `SourceModule` / `AuthAdapter` work RPCs (Kithara dials per call)
  - Thin storage put/get on Kithara (modules dial)
2. Promote interface pages from “sketch” to **v0.1 draft** (field names may still evolve; RPC set and dial rules must not).
3. Lock REST path set in [rest-api](../interfaces/rest-api.md) for MVP verbs (auth, streams, play, queue, **global** search, guest exchange).
4. Lock **target EF model** outline: `User` kinds, `UserAuthBinding`, `Struna`, `Tune`, `QueueEntry`, search-result cache — discard prototype `Playlist` as product schema ([ADR 006](../adrs/006-stream-source-tune-data-model.md)).
5. Document shared **audio volume** + session endpoint conventions ([ADR 004](../adrs/004-source-instance-socket-audio-plane.md)).



### Exit criteria

- Magpie and Bes can scaffold servers/clients against checked-in protos.
- Plume can stub against documented REST paths.
- No phase-1+ code invents a second register or auth protocol.



### Cross-repo


| Repo                | Follow-up                                                                 |
| ------------------- | ------------------------------------------------------------------------- |
| **magpie**, **bes** | Import / generate from frozen protos                                      |
| **plume**           | REST client stubs from rest-api                                           |
| **org**             | Join-secret / volume notes in deployment narrative when attach is decided |


---



## Phase 1 — Kithara skeleton

**Why:** Everything else hangs off registry, persistence, HTTP/gRPC hosts, and telemetry plumbing.

### Work

1. **Feature-first layout** (replace layer dump) — foreshadow from [02-internal-structure](../overview/02-internal-structure.md):

```text
Features/
  Streams/
  Auth/
  Modules/
  Streaming/
  Library/          # Tune metadata + storage keys
Infrastructure/
  Persistence/      # EF + IDbContextFactory
  Observability/    # OTel registration helpers
  Neck/             # hosted later in Phase 4
  Storage/          # local driver MVP
```

2. Config: `DbProvider` / `DbConnectionString`, `BARDIE_JOIN_SECRETS`, `OTEL_EXPORTER_OTLP_ENDPOINT` ([configuration](../operations/configuration.md)).
3. **OpenTelemetry bootstrap** in `Program.cs`: OTLP exporter, `service.name=bardie.kithara`, ASP.NET + gRPC + HttpClient + EF auto-instrumentation; W3C propagation on. Safe when collector is absent.
4. EF migrations for core tables (empty Struna/Tune shapes OK if fields match ADR 006).
5. **Module Registry** service: accept register + heartbeat authenticated by join secret; track slug, capabilities, advertise address, JWKS (auth), search schema (sources). Registry RPCs appear as spans automatically once gRPC instrumentation is on.
6. Dual listeners: HTTP `:8080`, gRPC `:5000` (internal).
7. Health/readiness endpoints suitable for Compose.

### Exit criteria

- Empty Kithara boots with SQLite.
- A dummy module can register with a join secret and appear in registry state.
- With a collector configured, a health or register request produces a trace for `bardie.kithara`.
- No playlist-centric API.

### Explicitly not yet

- Real Bes/Magpie behaviour, FFmpeg, ICY, Plume.

---



## Phase 2 — Auth vertical (Bes + Orchestrator)

**Why:** Control APIs need a verified identity. Auth stays behind Kithara (BFF).

### Work (Kithara)

1. Auth Orchestrator: merge `GetProviders`, route opaque `Authenticate` / `Refresh`, persist `User` + `UserAuthBinding` when `ensure_user`.
2. JWT Bearer middleware: verify **user** JWTs via registered module JWKS (cache JWKS).
3. REST: `/api/auth/discovery`, `/authenticate`, `/refresh` ([auth](../interfaces/auth.md)).
4. Guest JWT signing: env key if set, else auto-generate + persist; mint path used in Phase 6.
5. Bootstrap via `seedAdmin` on Bes when DB empty; log welcome text; `must_rotate_credentials`.



### Work (Bes — parallel)

1. Implement `AuthAdapter` against frozen proto.
2. `form_schema` discovery; mint access + refresh JWT; publish JWKS.
3. Binding payload = password hash material for Kithara to store.



### Exit criteria

- `curl`/client: discovery → authenticate → call a protected stub endpoint with Bearer.
- Swapping Bes for a mock adapter requires only registry + secret — no Kithara auth-code fork.



### Cross-repo


| Repo      | Follow-up                                             |
| --------- | ----------------------------------------------------- |
| **bes**   | MVP container + OTel `bardie.auth.bes`                |
| **plume** | Can start discovery-driven login UI against real auth |


---



## Phase 3 — Source vertical (protocol + Magpie proof)

**Why:** Prove multi-container audio control before investing in FFmpeg lifecycle.

### Work (Kithara)

1. Registry dials module advertise address for `Search` / `StartTrack` / `StopTrack` / `TrackStatus`.
2. Temporary **dev harness**: create a session FIFO path, call Magpie `StartTrack`, verify PCM bytes appear (even before Stream Server).
3. Storage interface MVP: local driver + opaque keys; Magpie put/get path (resolve open question).
4. Library write path: create/update `Tune` when Magpie reports cache miss → download (orchestration as designed after storage attach is chosen).



### Work (Magpie — parallel)

1. Implement source contract: register, search (+ URL/id fallback), track jobs writing **s16le / 48 kHz / stereo** to `fifo_path`.
2. Cache-first Tune resolve via storage contract.
3. Honor `StopTrack` / `PauseTrack` / `ResumeTrack`; advertise `search` | `play` | `pause`.



### Exit criteria

- Magpie registers; Kithara can Search and StartTrack; PCM lands on a FIFO Kithara created.
- A second fake source module could register without Magpie code changes.



### Cross-repo


| Repo       | Follow-up                                     |
| ---------- | --------------------------------------------- |
| **magpie** | ytdl + decode + OTel `bardie.source.magpie`   |
| **org**    | Shared volume / storage networking in Compose |


---



## Phase 4 — Neck (alive Struna + FFmpeg)

**Why:** Broadcast sync and ICY continuity require long-lived encoder + silence ([ADR 001](../adrs/001-broadcast-sync-model.md), [ADR 004](../adrs/004-source-instance-socket-audio-plane.md)).

### Work

1. Hosted **FFmpeg supervisor** (not request-scoped) + `IDbContextFactory` — discard spike singleton+scoped pattern.
2. `POST /api/streams` → alive: reserve slug, create session FIFO, start silence feeder, start FFmpeg reading FIFO.
3. `DELETE /api/streams/{id}` → `StopTrack` first, then kill FFmpeg, close FIFO, free slug.
4. Pause = silence feeder on; empty `play` = unpause ([playback-control](../domains/playback-control.md)).
5. Queue head → `StartTrack` / skip → `StopTrack` + next; **never** restart FFmpeg on queue shift.
6. Encode mode wiring (`compatibility` | `quality`) once profiles are defined (open question).



### Exit criteria

- Alive Struna produces continuous encoded audio on FFmpeg’s output pipe with silence between tracks.
- Skip does not drop ICY listeners (verified once Phase 5 exists; pipe continuity checked here).



### Discard from spike

- Playlist concat demuxer approach, Icecast-style output URL, ICY via FFmpeg stdin — see [spike](../spike/prototype-neck-ffmpeg.md).

---



## Phase 5 — Stream Server (ICY)

**Why:** Listeners are the product surface; API-only is not a radio.

### Work

1. `GET /stream/{slug}` with ICY headers + `icy-metaint` metadata injection ([http-stream-output](../interfaces/http-stream-output.md)).
2. Fan-out from FFmpeg pipe to N listeners.
3. Playback access gates: public / protected query token / private Bearer ([struna-access](../domains/struna-access.md)).
4. Push now-playing → `StreamTitle` updates from Neck/track status.



### Exit criteria

- VLC (or equivalent) plays a public slug URL continuously across a skip.
- Protected stream rejects missing/wrong token.

---



## Phase 6 — Control REST complete

**Why:** Clients (Plume or raw HTTP) need the full DJ surface.

### Work

1. Play / quickplay / queue / quickqueue / skip / pause / delete / now-playing / queue CRUD ([rest-api](../interfaces/rest-api.md)).
2. Quicksearch / search fan-out via registry capabilities.
3. Guest code → `POST …/guest/exchange` → **ephemeral guest user** + Kithara-minted JWT (+ refresh); rate limit; destroy guests with Struna.
4. Control ACL: private vs protected (ephemeral guests); Struna ownership checks (still open — see below).
5. Global search + principal-scoped result cache; quickplay source priority (multi-source ready).
6. `GET /api/streams/{id}/now-playing` aligned with ICY metadata.



### Exit criteria

- Full DJ loop with Bes JWT and with guest JWT on a protected-control Struna.
- Magpie is selectable only via `module` slug / priority — no Magpie-specific REST.

---



## Phase 7 — Plume MVP (optional client)

**Why:** Reference user-aware UI; stack must still work without it.

### Work (Plume)

1. Edge routes `/`, `/player/{slug}`.
2. Discovery-driven Bes login; store Bearer + refresh.
3. Wire control verbs; guest exchange UX for protected control.
4. Browser player **off by default**; optional listen to `/stream/{slug}`.
5. OTel `bardie.plume`.



### Exit criteria

- Human can create a Struna, search Magpie, play, and hear it in VLC via `/stream/{slug}`.
- Removing Plume from Compose leaves API + stream + modules working.



### Cross-repo


| Repo      | Follow-up                                                                                             |
| --------- | ----------------------------------------------------------------------------------------------------- |
| **plume** | [mvp/v0.1-scope](https://github.com/Bardie-radio/plume/blob/main/docs/architecture/mvp/v0.1-scope.md) |
| **org**   | Edge path map already documented — keep aligned                                                       |


---



## Phase 8 — Compose bundle + verify telemetry

**Why:** Modularity is proven only when modules attach by config. OTel export already exists from Phase 1 — this phase **wires the collector** and proves cross-service traces.

### Work

1. Reference Compose: edge + `plume` + `kithara` + `magpie` + `bes` ([org deployment](https://github.com/Bardie-radio/.github/blob/main/profile/docs/architecture/05-deployment.md)).
2. `BARDIE_JOIN_SECRETS` for all modules; audio/storage volumes as decided.
3. Point every app at the **external** OTel collector (`OTEL_EXPORTER_OTLP_ENDPOINT`); confirm `service.name` values per [observability](../operations/observability.md).
4. Smoke script / checklist: register → login → create → play → listen → skip — **and** a single play trace spanning Plume → Kithara → Magpie.

### Exit criteria

- Documented `docker compose up` path for the MVP quartet.
- Collector shows a continuous play path across all four `bardie.*` service names.

---



## Suggested coding order inside Kithara (Phase 1–6)

Use this when slicing PRs:

1. Solution layout + DI + config + **OTel bootstrap** + DB migrations
2. Module Registry (join secret) + gRPC host (spans via auto-instrumentation)
3. Auth Orchestrator + JWT verify + auth REST
4. Library/Tune + local storage driver
5. Source client (dial Magpie) + FIFO harness (+ custom spans on attach)
6. Neck supervisor + silence + FFmpeg (+ custom spans on lifecycle)
7. Stream Server + listen ACL
8. Remaining stream control REST + guest JWT

Prefer **vertical slices** that end in a demoable behaviour over horizontal “all models then all APIs.” Add custom Activity attributes as each feature lands — do not defer a big “instrumentation pass.”

---



## What “done” means for v0.1

Aligned with [v0.1-scope](v0.1-scope.md):

- [ ] Alive Struna with slug; silence until first track; DELETE frees slug  
- [ ] Magpie search/play via unified source contract  
- [ ] Bes login via unified auth contract; Kithara verifies JWTs  
- [ ] ICY `/stream/{slug}` with metadata; protected listen token  
- [ ] Guest code → ephemeral guest user + JWT; guests die with Struna  
- [ ] Plume optional; Compose + join secrets; OTel live from Phase 1, verified E2E in Phase 8

Out of scope stays out: Argus/Hecate, Beak/Cauda, Catbird/Starling, Icecast/HLS primary, multi-instance Kithara, `PrepareTrack`.

---



## Decisions locked (from design review)


| Topic                   | Decision                                                                                                                                                                                                                                   |
| ----------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Register dial**       | Modules **dial Kithara** to join; default `KITHARA_GRPC_ADDRESS` = Compose DNS (`kithara:5000`). Kithara hosts [Module Registry](../interfaces/grpc-module-registry.md).                                                                   |
| **Work RPCs**           | Module advertises address; **Kithara dials the module per operation** (no long-lived command stream) — atomic calls for OTel + access control.                                                                                             |
| **All modules equal**   | Source, auth, **and client** (Plume, …) Register over gRPC. REST `/api` is the end-user surface UI modules use for UX.                                                                                                                     |
| **Audio attach**        | Shared Compose volume; Kithara creates per-Struna session endpoints on demand; modules write PCM ([ADR 004](../adrs/004-source-instance-socket-audio-plane.md)). Prefer Unix sockets in implementation.                                    |
| **Storage**             | Drivers **only on Kithara**; modules dial a **thin** put/get API. No per-module `BARDIE_STORAGE_`*.                                                                                                                                        |
| **Pause**               | Part of common source contract (`PauseTrack` / `ResumeTrack`); Magpie implements; Starling omits `pause` capability.                                                                                                                       |
| **Bootstrap admin**     | Auth capability `seedAdmin`; Kithara calls module; welcome text → Kithara logs; `must_rotate_credentials`.                                                                                                                                 |
| **Multi-source**        | Design for many sources from day one (priority / fan-out) — no Magpie-only shortcuts.                                                                                                                                                      |
| **Search**              | **Global** REST; principal-scoped cache. Guests: clear on Struna teardown. Durable/managed: replace on next search + configurable timeout.                                                                                                 |
| **Guests**              | Guest code **per Struna** → each exchange creates an **ephemeral guest user** + Kithara JWTs (+ refresh); destroyed with Struna. **Rotate code = block new joins only** (existing guests keep working until Struna delete).                |
| **ACL**                 | Any registered durable/managed user may create Strunas; **owner** on Struna model; private control = owner + owner grants; ephemeral guests = **only** control that Struna; managed users ≤ static module’s advertised permission ceiling. |
| **Proto packaging**     | **Published package** (versioned contracts) for module authors / contributors.                                                                                                                                                             |
| **Guest JWT signing**   | If `BARDIE_GUEST_JWT_SIGNING_KEY` (or key file) is set → use it; else **auto-generate** on first boot and **persist** next to data volume. Access TTL default ~15m; refresh until Struna teardown (or capped refresh lifetime).            |
| **Module channel auth** | Target: join secret at Register → Kithara issues module client cert → **mTLS on the whole gRPC surface** afterward.                                                                                                                        |
| **Encode mode UI**      | Dropped from user-facing create for now; operator/FFmpeg profile instead.                                                                                                                                                                  |
| **Tune model**          | Unified library unit for **queue + history + optional blob cache**; sparse Tunes OK (e.g. Starling URI, no bytes). `QueueEntry` → Tune id.                                                                                                  |
| **Naming**              | **durable user** / **managed user** (static UI; long-lived) / **ephemeral guest user** (guest code; Struna-scoped).                                                                                                                        |


Design-review open questions are **closed**. Phase 0 can proceed from the locked table above.

---



## Related

- [v0.1-scope.md](v0.1-scope.md) · [v0.1-milestones.md](v0.1-milestones.md)
- [glossary](../glossary.md) · [grpc-module-registry](../interfaces/grpc-module-registry.md) · [auth](../interfaces/auth.md)
- Org: [05-deployment](https://github.com/Bardie-radio/.github/blob/main/profile/docs/architecture/05-deployment.md)

**Read next:** [v0.1-milestones.md](v0.1-milestones.md) · start Phase 0 contract freeze.