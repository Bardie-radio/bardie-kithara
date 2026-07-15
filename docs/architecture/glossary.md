# Glossary

Dual naming: **codename** (Ukrainian/Greek instrument theme) + **plain English**.

| Codename | Plain English | Description |
|----------|---------------|-------------|
| **Kithara** | Core backend | Bardie's main service: API, Neck, Stream Server, module registries |
| **Struna** | Stream | A named broadcast channel; user-chosen `slug` for URLs |
| **Neck** / **Neck Service** | Stream lifecycle service | Kithara service that starts/stops Strunas, runs FFmpeg encoders |
| **Struna Encoder** | Per-stream FFmpeg process | Encodes audio from a source instance; managed by Neck |
| **Stream Server** | ICY HTTP output | Serves `GET /stream/{slug}` to listeners |
| **Tune** | Library item | Cached/metadata reference for file or ytdl content; not stream-owned |
| **Plume** | Web UI client module | Client-facing interface: `/`, `/player/{slug}` |
| **Client module** | User-facing integration | Deployable surface (Plume, Discord bot, Telegram bot, …) that calls Kithara REST API |
| **Source module** | Audio provider | External container (YouTube, file, …) registered with Kithara |
| **Source instance** | Playback handle | Ephemeral audio output created by a source module on a Unix socket |
| **Auth adapter** | Auth provider module | External container (`bardie-auth-local`, OIDC, …) for login/validation |
| **Auth orchestrator** | Auth router | Inside Kithara: discovery aggregation, token routing, service tokens |

## Prototype vs target

| Term | Prototype (`Neck.cs` spike) | Target |
|------|----------------------------|--------|
| Neck | Playlist → FFmpeg concat | Source instance → FFmpeg → Stream Server |
| Struna | Metadata only, no source binding | Slug, access modes, active instance + queue |
| Tune | `PlaylistId` + `Playlists` conflict | Library reference; queue holds intents |

See [spike/prototype-neck-ffmpeg.md](spike/prototype-neck-ffmpeg.md).

**Read next:** [overview/01-system-context.md](overview/01-system-context.md)
