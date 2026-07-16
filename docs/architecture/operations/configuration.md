# Configuration

Env and Compose knobs for the **Kithara container** — how it finds its database, collectors, and modules.

## Kithara

| Variable | Description |
|----------|-------------|
| `DbProvider` | `sqlite` or `postgres` |
| `DbConnectionString` | EF connection string |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Collector URL |
| `BARDIE_SERVICE_TOKENS` | JSON map of bot/service tokens |

## Module discovery

Source and auth modules register via gRPC on startup. Compose sets:

- `KITHARA_GRPC_ADDRESS=kithara:5000`
- Per-module listen address advertised in `Register` call

## Login+password auth adapter (MVP)

- User store path or bootstrap admin
- Token signing secret (compose secret)
- Module and repo name undecided

## Reserved slugs

Configured list: `api`, `stream`, `admin`, `player`, …

**Related:** [deployment.md](deployment.md) · [observability.md](observability.md)

**Read next:** [observability.md](observability.md)
