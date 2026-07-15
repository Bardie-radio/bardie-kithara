# Glossary

Dual naming: **codename** (Musical instruments theme) + **plain English**. When docs say Struna, they mean a stream — and vice versa once you've met the glossary.

| Codename | Plain English | Description |
|----------|---------------|-------------|
| **Kithara** | Core backend | Bardie's main service: API, Neck, Stream Server, module registries |
| **Struna** | Stream | A named broadcast channel; user-chosen `slug` for URLs |
| **Neck** / **Neck Service** | Stream lifecycle service | Kithara service that starts/stops Strunas, runs FFmpeg encoders |
| **Struna Encoder** | Per-stream FFmpeg process | Encodes audio from a source instance; managed by Neck |
| **Stream Server** | ICY HTTP output | Serves `GET /stream/{slug}` to listeners |
| **ICY** / **ICY-over-HTTP** | Shoutcast-style metadata stream | Continuous HTTP audio with in-band `StreamTitle` / `icy-metaint` metadata |
| **Tune** | Library item | Cached/metadata reference for file or ytdl content; not stream-owned |
| **QueueEntry** | Queue item | Play intent on a Struna (tune ID or external ref); resolved at play time |
| **Plume** | Web UI client module | Client-facing interface: `/`, `/player/{slug}` |
| **Client module** | User-facing integration | Deployable surface (Plume, Discord bot, Telegram bot, …) that calls Kithara REST API |
| **Source module** | Audio provider | External container (YouTube, file, …) registered with Kithara |
| **Source instance** | Playback handle | Ephemeral audio output created by a source module on a Unix socket |
| **Module Registry** | Module directory | Inside Kithara: tracks connected source modules and auth adapters |
| **Auth adapter** | Auth provider module | External container for login/validation (login+password MVP, OIDC later; names TBD) |
| **Auth orchestrator** | Auth router | Inside Kithara: discovery aggregation, token routing, service tokens |
| **Listen token** | Playback secret | Query/basic/path credential for **protected** playback modes |
| **Guest code** | Control share code | Short code for **protected** control without a full user account |
| **Service token** | Bot credential | Long-lived token for client bots / modules calling Kithara REST |
| **DbProvider** | Persistence backend | Config switch: `sqlite` or `postgres` |

## Prototype vs target

| Term | Prototype (`Neck.cs` spike) | Target |
|------|----------------------------|--------|
| Neck | Playlist → FFmpeg concat | Source instance → FFmpeg → Stream Server |
| Struna | Metadata only, no source binding | Slug, access modes, active instance + queue |
| Tune | `PlaylistId` + `Playlists` conflict | Library reference; queue holds intents |

See [spike/prototype-neck-ffmpeg.md](spike/prototype-neck-ffmpeg.md).

**Related:** [overview/README](overview/README.md) · [ADRs](adrs/README.md)

**Read next:** [overview/01-system-context.md](overview/01-system-context.md)
