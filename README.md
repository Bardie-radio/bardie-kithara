# Kithara

> The core backend of Bardie — managing synchronized audio streams, playback control, and module orchestration.

Kithara is the main instrument of the Bardie ecosystem. It exposes a REST API for clients, coordinates source modules over gRPC, encodes live audio with FFmpeg, and serves synchronized streams to listeners over HTTP.

🚧 **Heavily Work in Progress.** No working builds yet

---

## ✨ Features

- 🎧 **Live broadcast** — one encoder per stream; everyone hears the same moment
- 🧩 **Module ecosystem** — source and auth adapters plug in via gRPC
- 🔐 **Flexible access** — independent playback and control permissions per stream
- 📡 **Native streaming** — FFmpeg → ICY-over-HTTP at `/stream/{slug}`
- 📊 **Observable** — OpenTelemetry across Kithara and connected modules

---

## 🏗️ Architecture

📖 **[Architecture documentation](docs/architecture/README.md)** — design decisions, domain model, API contracts, and ADRs.

Kithara sits at the center of Bardie. Clients and modules talk to it; it owns stream lifecycle, auth orchestration, and audio output.

### 🎼 Core responsibilities

- **Struna management** — create, configure, and tear down synchronized streams
- **Playback control** — play, skip, stop, and queue tunes via source instances
- **Neck service** — stream lifecycle: module coordination, FFmpeg encoding, listener fan-out
- **Auth orchestration** — delegates login and permissions to auth adapter modules

### 🔌 Connected components

```text
├── Plume              → Web UI; REST client and optional in-browser player
├── Source modules     → YouTube, local input, files (gRPC + Unix socket audio)
├── Auth adapters      → Local login (MVP); OIDC providers (v0.2)
└── External players   → VLC, VRChat, Discord bots via /stream/{slug}
```

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
| [Domains](docs/architecture/domains/) | Streams, source instances, auth, playback |
| [Interfaces](docs/architecture/interfaces/) | REST API, gRPC contracts, streaming |
| [ADRs](docs/architecture/adrs/) | Architecture decision records |
| [MVP v0.1](docs/architecture/mvp/v0.1-scope.md) | Scope and milestones |
| [Spike](docs/architecture/spike/prototype-neck-ffmpeg.md) | Prototype assessment |

---

## 🚀 Self-hosting

Kithara is designed to run as part of a self-hosted Bardie stack — typically alongside Plume, source modules, and an auth adapter behind a reverse proxy.

See [deployment guide](docs/architecture/operations/deployment.md) and [MVP scope](docs/architecture/mvp/v0.1-scope.md) for the reference Compose layout.
