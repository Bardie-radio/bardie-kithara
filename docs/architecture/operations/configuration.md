# Configuration

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

## Auth-local (MVP)

- User store path or bootstrap admin
- Token signing secret (compose secret)

## Reserved slugs

Configured list: `api`, `stream`, `admin`, `player`, …

**Read next:** [observability.md](observability.md)
