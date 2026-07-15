# Kithara

> The core backend of Bardie — managing synchronized audio streams, playback control, and module orchestration.

Kithara is the main instrument of the Bardie ecosystem. It exposes a REST API for clients, coordinates source and auth modules over gRPC, encodes live audio with FFmpeg, and serves synchronized streams to listeners over HTTP.

🚧 **Heavily Work in Progress.** No working builds yet

---

## ✨ Features

- 🎧 **Live broadcast** — one encoder per stream; everyone hears the same moment
- 🧩 **Modular ecosystem** — client, source, and auth adapters plug into a shared core
- 🔐 **Flexible access** — independent playback and control permissions per stream
- 📡 **Native streaming** — FFmpeg → ICY-over-HTTP at `/stream/{slug}`
- 📊 **Observable** — OpenTelemetry across Kithara and connected modules

---

## 🏗️ Architecture

📖 **[Architecture documentation](docs/architecture/README.md)** — design decisions, domain model, API contracts, and ADRs.

Kithara sits at the center of Bardie. **Client modules**, **source modules**, and **auth adapters** all connect to it; Kithara owns stream lifecycle, auth orchestration, and audio output.

### 🎼 Core responsibilities

- **Struna management** — create, configure, and tear down synchronized streams
- **Playback control** — play, skip, stop, and queue tunes via source instances
- **Neck service** — stream lifecycle: module coordination, FFmpeg encoding, listener fan-out
- **Auth orchestration** — delegates login and permissions to auth adapter modules

### 🔌 Modular components

```text
├── Client modules       → User-facing control and discovery surfaces
│   ├── Plume            → Web UI (MVP); / and /player/{slug}
│   ├── Discord bot      → Voice channels + stream control (name TBD)
│   └── Telegram bot     → Remote Struna control (name TBD)
├── Source modules       → Audio providers (gRPC + Unix socket)
│   ├── YouTube / ytdl   → Search and play from online sources
│   ├── Local input      → Re-broadcast direct audio from your PC
│   └── File source      → Play uploaded audio files
├── Auth adapters        → Login and token validation (gRPC)
│   ├── auth-local       → Username + password (MVP)
│   └── auth-oidc        → Zitadel, Google, … (v0.2)
└── Legacy players       → Listen-only: VLC, VRChat via /stream/{slug}
```

More client modules may follow — Bardie does not assume a single UI; it assumes the channels your community already uses.

### 🌐 URI map

| Path | Handler |
|------|---------|
| `/api` | Kithara REST API |
| `/stream/{slug}` | Live ICY-over-HTTP audio |
| `/player/{slug}` | Stream control surface (Plume) |

Ecosystem overview: [Bardie-radio/.github](https://github.com/Bardie-radio/.github/tree/main/docs/architecture)

---

## 📖 Documentation

| Section | Contents |
|---------|----------|
| [Overview](docs/architecture/overview/) | System context, containers, data flow |
| [Domains](docs/architecture/domains/) | Streams, source instances, auth, [clients](docs/architecture/domains/clients.md) |
| [Interfaces](docs/architecture/interfaces/) | REST API, gRPC contracts, streaming |
| [ADRs](docs/architecture/adrs/) | Architecture decision records |
| [MVP v0.1](docs/architecture/mvp/v0.1-scope.md) | Scope and milestones |
| [Spike](docs/architecture/spike/prototype-neck-ffmpeg.md) | Prototype assessment |

---

## 🚀 Self-hosting

Kithara is designed to run as part of a self-hosted Bardie stack — typically alongside Plume (or another client module), source modules, and an auth adapter behind a reverse proxy.

See [deployment guide](docs/architecture/operations/deployment.md) and [MVP scope](docs/architecture/mvp/v0.1-scope.md) for the reference Compose layout.
