# ADR 007: Auth Adapter Modules

**Status:** Accepted (amended: modules issue/forward user JWTs + own refresh; Kithara verifies via JWKS + owns user DB and join secrets; Kithara may mint Struna-scoped guest capability JWTs only.)

## Context

Auth must be modular (login+password, OIDC, passkeys, custom) without forcing a self-hosted IdP for every deploy. Operators want **one Bardie user database** (Kithara’s). OIDC already issues JWTs; forging a second “Bardie-only” **identity** session token in core fights that model. Modules must stay decoupled. Public edge should stay Kithara + UI clients where protocolally possible. Short guest codes must not ride every control request.

## Decision

- **Auth Orchestrator** lives **inside Kithara** (discovery merge, route opaque auth/refresh payloads, **user JWT verification** via registered JWKS, **join secrets**, listen/guest secrets, **guest-code exchange**). Kithara does **not** mint **user/login** JWTs.
- **Guest control JWTs:** Kithara **may** mint Struna-scoped capability JWTs after a rate-limited guest-code exchange (`iss=kithara`, `struna_id`, `stream:control`, `exp`). Not a User; not routed through auth modules. See [struna-access](../domains/struna-access.md).
- **No built-in auth provider.** Every login method is a separate auth-adapter container on gRPC.
- **Named adapters:** **Bes** (password, MVP), **Argus** (OIDC, v0.2), **Hecate** (passkeys, future). Modules are independent — no cross-module “modes.”
- **Unified adapter contract:** `GetProviders` + `Authenticate` + `Refresh`. No protocol-specific RPCs. Opaque payloads for forms, callbacks, ceremonies.
- **Unified credential protocol for login: JWT.** Modules return `access_token` (JWT) + `refresh_token`. **Argus** forwards OIDC tokens and refreshes through the IdP; **Bes** / **Hecate** mint their own JWTs. Kithara verifies user access JWTs locally (module/IdP JWKS).
- **Module result** also carries allow + rights/entities and optional ensure-user / binding payload for Kithara’s DB.
- **User core + `UserAuthBinding`** live only in Kithara. Provider slug = lowercase codename (`bes`, `argus`, `hecate`).
- **Client modules** register as **user-aware** (Plume, Cauda — Bearer JWT) or **static** (Beak — **join secret** to manage **persistent module-managed users** with **per-user credentials**; Beak tenancy = Discord guild) — see [clients](../domains/clients.md).
- **Join secrets** in Kithara config authenticate source, auth, and client modules (`Register` + static managed-user admin). Not a shared impersonation key for all managed users. Auth modules also publish JWKS at register.
- **Public edge:** Kithara + UI clients. Adapters stay internal (BFF). IdP is the other public hop for OIDC redirects.
- **Client-rendered UI** via discovery (`form_schema`, `redirect`) — no adapter-hosted login pages.
- **Listen tokens** and **guest codes** are Struna secrets owned by Kithara (listen token stays on `/stream`; guest code is exchange-only).
- **Explicit account linking**; **provider priority tier-list** when bindings disagree.
- **First admin** on empty DB is user creation/management (typical for Bes deploys) — not a Kithara container config env.

## Consequences

- One Bardie user DB; login token **issuance** aligns with industry OIDC practice for Argus and a parallel mint path for Bes/Hecate.
- Same **JWT Bearer** shape on `/api` for user login paths; guests use a separate Kithara-issued guest JWT; static modules use a **join secret** for admin and per-user credentials for Struna work.
- Plume never hardcodes password fields; stack works without Plume.
- Struna ACLs and listen/guest secrets stay in Kithara.
- MVP Compose includes `bes` alongside Kithara.

## Alternatives considered

- **Kithara mints Bardie identity JWT after module allow** — rejected; duplicates OIDC issuing and splits login behaviour across modules.
- **Guest code on every control request** — rejected; short shared secrets are weak on the hot path; exchange for a guest JWT instead.
- **Guest exchange via auth modules (Bes/Argus)** — rejected; guests are capability grants, not accounts.
- **Opaque guest bearer only (no JWT)** — rejected for this amendment; prefer JWT claims (`struna_id`, scope, `exp`) verified with Kithara’s signing key.
- **Built-in local password inside Kithara** — rejected; every auth method is a module (Bes).
- **Per-request ValidateToken to the module** — rejected as the hot path; prefer local JWKS verify.
- **Protocol-specific RPCs** (e.g. `ExchangeOidcCode`) — rejected; breaks a unified module contract.
- **Adapter-owned user database** — rejected; second Bardie DB.
- **Adapter-hosted login HTTP** — rejected; browsers must not call modules (ADR 003).
- **Separate bot tokens vs join secrets** — rejected; one **join secret** class covers registration and static-module admin.
- **Join secrets issued by auth modules** — rejected; operator-provisioned on Kithara; client modules declare static vs user-aware.

**Related:** [domains/auth-adapters.md](../domains/auth-adapters.md) · [interfaces/auth.md](../interfaces/auth.md)

**Read next:** [008-otel-observability.md](008-otel-observability.md)
