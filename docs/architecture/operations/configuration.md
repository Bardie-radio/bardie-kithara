# Configuration

Env and Compose knobs for the **Kithara container** — database, collectors, modules, and auth.

## Kithara

| Variable | Description |
|----------|-------------|
| `DbProvider` | `sqlite` or `postgres` |
| `DbConnectionString` | EF connection string |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | External collector URL (e.g. Alloy) |
| `BARDIE_SERVICE_TOKENS` | JSON map of bot/service tokens |
| `BARDIE_MODULE_JOIN_SECRET` | Shared secret for source/auth module `Register` |
| `BARDIE_AUTH_PROVIDER_PRIORITY` | Ordered provider slugs for claim/role arbitration |
| `BARDIE_BOOTSTRAP_ADMIN_*` | First local admin when DB empty (disabled if OIDC-from-start) |
| `BARDIE_JWT_*` / refresh TTLs | Session lifetime knobs (defaults TBD) |
| `BARDIE_STRUNA_SILENCE_CLEANUP` | Auto-stop after silent duration (planned) |

## Module discovery

Source and auth modules register via gRPC on startup. Compose sets:

- `KITHARA_GRPC_ADDRESS` (internal DNS to Kithara `:5000`)
- Join secret matching Kithara
- Optional `MODULE_SLUG_OVERRIDE` when community slugs collide

## Local password (MVP)

Built into Kithara — user + `UserAuthBinding` rows. No separate auth DB.

## Reserved slugs

Configured list must stay consistent: `api`, `stream`, `admin`, `player`, …

**Related:** [deployment.md](deployment.md) · [observability.md](observability.md) · [auth-adapters](../domains/auth-adapters.md)

**Read next:** [observability.md](observability.md)
