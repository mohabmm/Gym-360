#!/usr/bin/env bash
# Deploy / upgrade self-hosted SigNoz on the staging host (plain Docker Compose).
#
# Why a script instead of vendoring the compose: SigNoz's compose bind-mounts
# several config files (clickhouse-setup/*.xml, otel-collector-config.yaml) and
# its image tags are matched per release. Fetching the WHOLE deploy/docker tree
# at a pinned tag keeps everything internally consistent — far safer than
# hand-pinning individual image tags.
#
# Run this ON THE STAGING HOST (it needs the local Docker daemon). For a
# Dokploy-managed app instead, see deploy/signoz/README.md (git-source path).
#
# Usage:
#   scripts/deploy_signoz.sh up        # fetch pinned tree + docker compose up -d
#   scripts/deploy_signoz.sh down      # stop (keeps volumes/data)
#   scripts/deploy_signoz.sh status    # ps + health
#   scripts/deploy_signoz.sh logs      # tail otel-collector + signoz
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="${HERE}/../deploy/signoz/.env"
WORK_DIR="${SIGNOZ_WORK_DIR:-/opt/signoz}"     # where the deploy tree lives on the host

[ -f "$ENV_FILE" ] || { echo "missing $ENV_FILE"; exit 1; }
# shellcheck disable=SC1090
set -a; source "$ENV_FILE"; set +a
: "${SIGNOZ_TAG:?set SIGNOZ_TAG in deploy/signoz/.env}"

fetch_tree() {
  echo "→ fetching SigNoz ${SIGNOZ_TAG} deploy/docker tree into ${WORK_DIR}"
  mkdir -p "$WORK_DIR"
  # Sparse-ish fetch via the GitHub tarball; extract only deploy/docker.
  local tmp; tmp="$(mktemp -d)"
  curl -fsSL "https://github.com/SigNoz/signoz/archive/refs/tags/${SIGNOZ_TAG}.tar.gz" -o "$tmp/signoz.tgz"
  tar -xzf "$tmp/signoz.tgz" -C "$tmp"
  local src; src="$(find "$tmp" -maxdepth 2 -type d -path '*/deploy/docker' | head -1)"
  [ -n "$src" ] || { echo "deploy/docker not found in tarball"; exit 1; }
  # Preserve persisted data/ across upgrades.
  rsync -a --delete --exclude 'data/' "$src/" "$WORK_DIR/"
  rm -rf "$tmp"
  # Pin the app image tag (VERSION) for the compose.
  if ! grep -q '^VERSION=' "$WORK_DIR/.env" 2>/dev/null; then
    echo "VERSION=${VERSION:-$SIGNOZ_TAG}" >> "$WORK_DIR/.env"
  fi
}

compose() { ( cd "$WORK_DIR" && VERSION="${VERSION:-$SIGNOZ_TAG}" docker compose "$@" ); }

case "${1:-up}" in
  up)
    fetch_tree
    echo "→ bringing SigNoz up (this pulls clickhouse + signoz + collector images; first run is slow)"
    compose up -d
    echo
    echo "✓ SigNoz starting. UI: map a Dokploy domain (or :8080) to the 'signoz' container."
    echo "  OTLP receivers: signoz-otel-collector:4318 (HTTP) / :4317 (gRPC) on the 'signoz-net' network."
    echo
    echo "NEXT — point the API at it (Dokploy → gymapp-api → Environment):"
    echo "  OTEL_EXPORTER_OTLP_ENDPOINT=http://signoz-otel-collector:4318"
    echo "  OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf"
    echo "  OTEL_SERVICE_NAME=gymapp-api"
    echo "  (the API must share the 'signoz-net' Docker network — see deploy/signoz/README.md)"
    ;;
  down)    compose down ;;
  status)  compose ps ;;
  logs)    compose logs -f --tail=100 otel-collector signoz ;;
  *) echo "usage: $0 {up|down|status|logs}" >&2; exit 2 ;;
esac
