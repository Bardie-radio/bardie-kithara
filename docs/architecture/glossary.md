# Glossary

Dual naming: **codename** (musical instruments theme) + **plain English**. When docs say Struna, they mean a stream — and vice versa once you've met the glossary.

| Codename | Plain English | Description |
|----------|---------------|-------------|
| **Kithara** | Core backend | Bardie's main service: API, Neck, Stream Server, module registries |
| **Struna** | Stream | A named broadcast channel; user-chosen `slug` for URLs |
| **Neck** / **Neck Service** | Stream lifecycle service | Starts/stops Strunas, owns session FIFOs, runs FFmpeg encoders, silence feeder |
| **Struna Encoder** | Per-stream FFmpeg process | Reads session FIFO for Struna life; managed by Neck |
| **Session FIFO** | Per-Struna PCM pipe | Kithara-owned named FIFO; modules write; FFmpeg reads |
| **Track job** | Decode / playback job | Ephemeral source-module work writing PCM for one queue item |
| **Stream Server** | ICY HTTP output | Serves `GET /stream/{slug}` to listeners |
| **ICY** / **ICY-over-HTTP** | Shoutcast-style metadata stream | Continuous HTTP audio with in-band `StreamTitle` / `icy-metaint` metadata |
| **Tune** | Library item | Cached/metadata reference for file or ytdl content; not stream-owned |
| **QueueEntry** | Queue item | Play intent: module slug + track ref (optional Tune id) |
| **Plume** | Web UI client module | Optional client: `/`, `/player/{slug}` |
| **Client module** | User-facing integration | Deployable surface (Plume, Discord bot, …) that calls Kithara REST |
| **Source module** | Audio provider | External container (YouTube, file, …) registered with Kithara |
| **Module Registry** | Module directory | Inside Kithara: source modules and auth adapters |
| **Auth adapter / provider** | Auth provider | Local (built-in) or external container (OIDC); names TBD |
| **Auth orchestrator** | Auth router | Discovery, identity routing, JWT issue, service tokens |
| **UserAuthBinding** | Provider binding | `(user, provider_slug)` row + payload in Kithara DB |
| **Listen token** | Playback secret | Query credential for **protected** playback (Kithara-owned) |
| **Guest code** | Control share code | Short code for **protected** control (Kithara-owned) |
| **Service token** / **join secret** | Bot / module credential | Long-lived secrets in Kithara/Compose config |
| **DbProvider** | Persistence backend | Config switch: `sqlite` or `postgres` |

## Prototype vs target

| Term | Prototype (`Neck.cs` spike) | Target |
|------|----------------------------|--------|
| Neck | Playlist → FFmpeg concat | Session FIFO → FFmpeg → Stream Server |
| Struna | Metadata only, no source binding | Slug, access modes, alive-on-create, queue |
| Audio job | N/A | Track job per queue item; multi-source capable |
| Tune | `PlaylistId` + `Playlists` conflict | Library reference; queue holds intents |

See [spike/prototype-neck-ffmpeg.md](spike/prototype-neck-ffmpeg.md).

**Related:** [overview/README](overview/README.md) · [ADRs](adrs/README.md)

**Read next:** [overview/01-system-context.md](overview/01-system-context.md)
