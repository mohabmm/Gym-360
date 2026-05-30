# Observability — SigNoz (self-hosted on Dokploy, staging)

End-to-end OpenTelemetry: the .NET API exports **traces + metrics + logs** over OTLP to a
self-hosted SigNoz stack running as a Dokploy Compose app. This replaces the localhost-only
Seq sidecar (which was never externally reachable on staging).

## Why
- Seq ran at `http://localhost:5341` inside the API container — not exposed, so 500 stack
  traces couldn't be read off-box.
- SigNoz gives traces (with SQL spans + recorded exceptions), metrics, and logs in one UI,
  exposed via a Dokploy domain with TLS.
- The per-gym write 500 (member enroll / announcement) will appear as a **span with a
  recorded `SqlException`** — e.g. `Invalid column name 'RetryCount'` on a
  `g{N}_…outbox_messages` INSERT — pinpointing the fix (apply `src/DB/seed.sql` §0.6/§0.6.1).

## Part 1 — Deploy SigNoz as a Dokploy Compose app

1. Dokploy → **Create → Compose** → name `signoz`.
2. Use the official SigNoz docker-compose:
   `https://github.com/SigNoz/signoz/blob/main/deploy/docker/docker-compose.yaml`
   (services: `clickhouse`, `signoz-otel-collector`, `query-service`, `alertmanager`,
   `frontend`/`signoz`). Pin a release tag, don't track `main`.
3. **Persistence:** map a Dokploy volume for ClickHouse data (`/var/lib/clickhouse`) and the
   collector — otherwise telemetry is lost on every redeploy.
4. **Resources:** ClickHouse needs ~2–4 GB RAM. Confirm the staging host has headroom
   (RAM is the constraint, not disk).
5. **Network:** ensure the `signoz` compose and the **API app share a Docker network** so the
   API can reach `signoz-otel-collector:4318` (OTLP/HTTP) by service name. In Dokploy, attach both to the
   same network (e.g. the shared `dokploy-network`), or run them in the same compose project.

## Part 2 — Expose the SigNoz UI

- Dokploy → `signoz` app → **Domains** → add `signoz.gym-365.com` → target the frontend
  service port (`8080` or `3301`, depending on the compose version). Traefik handles TLS.
- Lock it down (basic-auth / IP allowlist) — it exposes all telemetry.

## Part 3 — Point the API at SigNoz (env vars only; code already merged)

The API auto-enables OpenTelemetry **only when an OTLP endpoint is set** — so nothing changes
locally. On the Dokploy **API app → Environment**, add:

```
OTEL_EXPORTER_OTLP_ENDPOINT=http://signoz-otel-collector:4318
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
OTEL_SERVICE_NAME=gymapp-api
```

- Use the **internal** collector service name + port `4318` (OTLP/HTTP), NOT the public domain.
- HTTP/protobuf is the recommended transport here — simpler over the Dokploy Docker network,
  no HTTP/2 negotiation. The code routes the Serilog logs sink to `/v1/logs` automatically for
  `http/protobuf`; the OTLP trace/metric exporter appends `/v1/traces` + `/v1/metrics` itself.
- gRPC alternative (only if you prefer it): `OTEL_EXPORTER_OTLP_PROTOCOL=grpc` with
  `OTEL_EXPORTER_OTLP_ENDPOINT=http://signoz-otel-collector:4317`.

Redeploy the API. Within ~1 min, `gymapp-api` appears in SigNoz → Services.

## What the code does (merged via this PR)

- `src/API/GymApp.Api/Extensions/ObservabilityExtensions.cs` — `AddObservability()`:
  registers OTel **traces** (AspNetCore + HttpClient + **SqlClient with `RecordException` +
  `SetDbStatementForText`**) and **metrics** (AspNetCore + HttpClient + Runtime), OTLP exporter.
  No-op when no OTLP endpoint is configured.
- `src/API/GymApp.Api/Extensions/InfrastructureExtensions.cs` — Serilog gains an **OTLP log
  sink** when the endpoint is set (Console + Seq remain; logs now also flow to SigNoz, tagged
  `service.name=gymapp-api` so they correlate with traces).
- `Program.cs` — `builder.AddObservability();` after `AddCommonInfrastructure()`.
- The OTLP exporter reads the standard `OTEL_EXPORTER_OTLP_*` env vars natively.

## Reading the per-gym 500 once SigNoz is live

1. SigNoz → **Traces** → filter `service.name = gymapp-api`, `http.route` contains
   `staff-create` (or status `Error`).
2. Open a failing trace → the **SqlClient span** carries the exception + the SQL text.
3. Expect `Invalid column name 'RetryCount'` / `Invalid object name 'g1_…outbox_messages'`
   → confirms the per-gym outbox lag → apply `src/DB/seed.sql` §0.6/§0.6.1 to the staging DB,
   then re-run `scripts/role_action_sweep.py` (browser UA) to confirm green.

## Optional follow-ups
- Retire the Seq sink once SigNoz logs are confirmed (drop `Serilog.Sinks.Seq` + the
  `Seq` WriteTo entry from `appsettings.json`).
- Add OTel instrumentation to the Quartz outbox processor / Redis if deeper tracing is wanted.
