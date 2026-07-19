# Configuration

Env and Compose knobs for the **Kithara container** — database, collectors, modules, and auth.

## Kithara

| Variable | Description |
|----------|-------------|
| `DbProvider` | `sqlite` or `postgres` |
| `DbConnectionString` | EF connection string |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | External collector URL (e.g. Alloy) |
| `BARDIE_JOIN_SECRETS` | Map of module slug → secret (source, auth, and client modules — register + static admin) |
| `BARDIE_AUTH_PROVIDER_PRIORITY` | Ordered provider slugs for claim/role arbitration |
| `BARDIE_STRUNA_SILENCE_CLEANUP` | Auto-delete after silent duration (planned) |
| `BARDIE_GUEST_JWT_SIGNING_KEY` | Optional. If set, Kithara uses this key to sign ephemeral guest JWTs. If unset, auto-generate on first boot and persist on the data volume |
| `BARDIE_GUEST_JWT_ACCESS_TTL` | Access-token lifetime for ephemeral guests (default ~15m) |
| `BARDIE_GUEST_JWT_REFRESH_*` | Refresh window for ephemeral guests (until Struna teardown / capped lifetime — sketch) |
| `BARDIE_SEARCH_CACHE_TTL` | Timeout for durable/managed search-result cache (guests clear on Struna teardown) |
| `BARDIE_STORAGE_DRIVER` | Blob backend: `local` (MVP default) \| `s3` \| later `webdav` |
| `BARDIE_STORAGE_PATH` | Local driver root (volume or NFS/SMB mount) |
| `BARDIE_STORAGE_S3_*` | S3-compatible endpoint, bucket, region, credentials (sketch) |

**User/login** JWT mint / refresh TTLs belong on the **auth module** (e.g. Bes) — Kithara only verifies those via module JWKS. Optional Kithara knobs later: JWKS cache / clock-skew tolerances.

Library blobs (Magpie cache, Catbird uploads) use the storage driver above on **Kithara only** — modules do not duplicate `BARDIE_STORAGE_*`; they use Kithara as storage interface/discovery. See [storage](../domains/storage.md). Not Redis.

## Module discovery

Source and auth modules register via gRPC on startup. Compose sets:

- `KITHARA_GRPC_ADDRESS` (internal DNS to Kithara `:5000`)
- Join secret matching Kithara (`BARDIE_JOIN_SECRETS`)
- Optional `MODULE_SLUG_OVERRIDE` when community slugs collide

## Bes (MVP password auth)

Separate `bes` container. User + `UserAuthBinding` rows stay in Kithara’s DB — Bes has no separate auth DB. JWT mint / refresh lifetime knobs live on Bes (not on Kithara).

## Reserved slugs

Configured list must stay consistent: `api`, `stream`, `admin`, `player`, …

**Related:** [deployment.md](deployment.md) · [observability.md](observability.md) · [auth-adapters](../domains/auth-adapters.md)

**Read next:** [observability.md](observability.md)
