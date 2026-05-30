# SigNoz deploy (staging)

Self-hosted SigNoz for the staging API's OpenTelemetry (traces + metrics + logs).
See `docs/OBSERVABILITY_SIGNOZ.md` for the full rationale + how to read the per-gym 500.

## Why a fetch-script, not a vendored compose

SigNoz's `docker-compose.yaml` **bind-mounts several config files** from its
`deploy/docker` tree (`../common/clickhouse/config.xml`, `users.xml`,
`otel-collector-config.yaml`, dashboards, …) and its image tags are matched
per release. Committing just the compose file would break those mounts. So
`scripts/deploy_signoz.sh` downloads the **whole `deploy/docker` tree at a
pinned tag** (`deploy/signoz/.env` → `SIGNOZ_TAG`) — guaranteed internally
consistent — and runs `docker compose` from it. Pinned, reproducible, upgradable
by bumping one line.

Pinned now: **SigNoz v0.126.1** (latest stable, 2026-05-30).

## Option A — host script (recommended; SigNoz is heavy + stateful)

On the staging host (needs the local Docker daemon):

```bash
scripts/deploy_signoz.sh up        # fetch pinned tree + compose up -d
scripts/deploy_signoz.sh status    # ps + health
scripts/deploy_signoz.sh logs      # tail otel-collector + signoz
scripts/deploy_signoz.sh down      # stop (keeps volumes)
```

Creates the `signoz-net` Docker network + named volumes
(`signoz-clickhouse`, `signoz-sqlite`, `signoz-zookeeper-1`) — data survives upgrades.
OTLP receivers: `signoz-otel-collector:4318` (HTTP) / `:4317` (gRPC). UI: container `signoz` :8080.

## Option B — Dokploy Compose app (git source)

If you prefer Dokploy-managed: create a **Compose** app pointed at the SigNoz repo
(`github.com/SigNoz/signoz`, ref `v0.126.1`, compose path `deploy/docker/docker-compose.yaml`).
Dokploy clones the full tree so the bind-mounts resolve. Add a domain
(`signoz.gym-365.com` → `signoz` :8080) + lock it down.

## Connect the API (both options)

The API container must reach the collector. Attach it to the SigNoz network and set env:

```bash
# one-time: join the API container to signoz-net (or set it in the API's compose/Dokploy network)
docker network connect signoz-net <gymapp-api-container>
```

Dokploy → `gymapp-api` → Environment:

```
OTEL_EXPORTER_OTLP_ENDPOINT=http://signoz-otel-collector:4318
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
OTEL_SERVICE_NAME=gymapp-api
```

Redeploy the API. `gymapp-api` appears under SigNoz → Services within ~1 min.

## Read the per-gym 500
SigNoz → Traces → `service.name=gymapp-api`, route contains `staff-create` (or status Error)
→ open the failing trace → the **SqlClient span** carries the exception + SQL. Expect
`Invalid column name 'RetryCount'` / `Invalid object name 'g1_…outbox_messages'` → apply
`src/DB/seed.sql` §0.6/§0.6.1 to staging, re-run `scripts/role_action_sweep.py`.

## Upgrade
Bump `SIGNOZ_TAG` (and `VERSION`) in `deploy/signoz/.env`, re-run `scripts/deploy_signoz.sh up`.
