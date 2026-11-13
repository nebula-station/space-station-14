#!/usr/bin/env bash
# SPDX-FileCopyrightText: 2026 Nebula Station
# SPDX-License-Identifier: AGPL-3.0-or-later

set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
SERVER_CONFIG="$REPO_ROOT/Content.Server/server_config.admin-assist.local.toml"
COMPOSE_FILE="$SCRIPT_DIR/docker-compose.yml"
DB_NAME="ss14_playtest"
DB_SERVICE="db"
RUN_LOG_DIR="$(mktemp -d "${TMPDIR:-/tmp}/admin-assist-playtest.XXXXXX")"
SERVER_LOG="$RUN_LOG_DIR/server.log"
SIDECAR_LOG="$RUN_LOG_DIR/sidecar.log"
ADMIN_LOG="$RUN_LOG_DIR/admin-client.log"
GUEST_LOG="$RUN_LOG_DIR/guest-client.log"

SERVER_PID=""
SIDECAR_PID=""
ADMIN_PID=""
GUEST_PID=""
LOG_TAIL_PID=""

cleanup() {
    trap - EXIT INT TERM
    for pid in "$ADMIN_PID" "$GUEST_PID" "$SIDECAR_PID" "$SERVER_PID"; do
        if [[ -n "$pid" ]]; then
            kill -INT "$pid" 2>/dev/null || true
        fi
    done
    sleep 2
    for pid in "$ADMIN_PID" "$GUEST_PID" "$SIDECAR_PID" "$SERVER_PID" "$LOG_TAIL_PID"; do
        if [[ -n "$pid" ]]; then
            kill -TERM "$pid" 2>/dev/null || true
            wait "$pid" 2>/dev/null || true
        fi
    done
    echo "Stopped local Admin Assist playtest. Logs: $RUN_LOG_DIR"
}

trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

fail() {
    echo "runlocal: $*" >&2
    exit 1
}

for tool in docker dotnet uv python3; do
    command -v "$tool" >/dev/null 2>&1 || fail "required command not found: $tool"
done

[[ -f "$SERVER_CONFIG" ]] || fail "missing local server config: copy ${SERVER_CONFIG%.toml}.example.toml to $SERVER_CONFIG and fill in its admin_assist section"

python3 - "$SCRIPT_DIR" "$SERVER_CONFIG" <<'PY'
import sys
import tomllib
from pathlib import Path
from urllib.parse import urlparse

sidecar_dir, config_path = map(Path, sys.argv[1:])
config = tomllib.loads(config_path.read_text(encoding="utf-8"))
aa = config.get("admin_assist", {})
if not (aa.get("kidmesh_key") or aa.get("model_main_key")):
    sys.exit("runlocal: set admin_assist.kidmesh_key (or admin_assist.model_main_key) in the server config")
for key in ("game_dsn", "own_dsn", "round_state_token", "round_state_url"):
    if not aa.get(key):
        sys.exit(f"runlocal: set admin_assist.{key} in the server config")
db_config = config.get("database", {})
if db_config.get("engine") != "postgres" or db_config.get("pg_database") != "ss14_playtest":
    sys.exit("runlocal: local server config must use PostgreSQL database ss14_playtest")
for key in ("game_dsn", "own_dsn"):
    if urlparse(aa[key]).path.lstrip("/") != "ss14_playtest":
        sys.exit(f"runlocal: admin_assist.{key} must point to ss14_playtest")
if config.get("auth", {}).get("mode") != 0 or not config.get("console", {}).get("loginlocal"):
    sys.exit("runlocal: local config needs auth.mode=0 and console.loginlocal=true")
rules_file = Path(aa.get("rules_file", ""))
if not rules_file.is_absolute():
    rules_file = sidecar_dir / rules_file
if not rules_file.is_file():
    sys.exit(f"runlocal: rules file is missing: {rules_file}")
PY

LAN_IP="${AA_LAN_IP:-}"
if [[ -z "$LAN_IP" ]]; then
    LAN_IP="$(ipconfig getifaddr en0 2>/dev/null || true)"
fi
[[ -n "$LAN_IP" ]] || fail "could not find this Mac's LAN address; set AA_LAN_IP and retry"

echo "Starting disposable Postgres..."
docker compose -f "$COMPOSE_FILE" up -d --wait
DB_EXISTS="$(docker compose -f "$COMPOSE_FILE" exec -T "$DB_SERVICE" \
    psql -U ss14 -d postgres -Atqc "SELECT 1 FROM pg_database WHERE datname = '$DB_NAME'")"
if [[ "$DB_EXISTS" != "1" ]]; then
    docker compose -f "$COMPOSE_FILE" exec -T "$DB_SERVICE" createdb -U ss14 "$DB_NAME"
fi

echo "Starting server and waiting for game database migrations..."
(
    cd "$REPO_ROOT"
    exec dotnet run --project Content.Trauma.Server -c Release -- \
        --config-file Content.Server/server_config.admin-assist.local.toml
) >"$SERVER_LOG" 2>&1 &
SERVER_PID=$!

ready=0
for _ in $(seq 1 300); do
    if grep -Fq -- "-> Ready" "$SERVER_LOG"; then
        ready=1
        break
    fi
    if ! kill -0 "$SERVER_PID" 2>/dev/null; then
        cat "$SERVER_LOG" >&2
        fail "server exited before it became ready"
    fi
    sleep 1
done
if [[ "$ready" != "1" ]]; then
    cat "$SERVER_LOG" >&2
    fail "server did not become ready within five minutes"
fi

MIGRATIONS="$(docker compose -f "$COMPOSE_FILE" exec -T "$DB_SERVICE" \
    psql -U ss14 -d "$DB_NAME" -Atqc \
    "SELECT count(*) FROM information_schema.tables WHERE table_schema='public' AND table_name='__EFMigrationsHistory'")"
[[ "$MIGRATIONS" == "1" ]] || fail "game migrations did not create __EFMigrationsHistory"

echo "Applying Admin Assist database roles and schema..."
docker compose -f "$COMPOSE_FILE" exec -T "$DB_SERVICE" \
    psql -v ON_ERROR_STOP=1 -U ss14 -d "$DB_NAME" \
    -v ro_password=local_ro -v rw_password=local_rw \
    -f /aa/ops/01_roles.sql
docker compose -f "$COMPOSE_FILE" exec -T "$DB_SERVICE" \
    psql -v ON_ERROR_STOP=1 -U ss14 -d "$DB_NAME" -f /aa/ops/02_admin_assist_schema.sql
docker compose -f "$COMPOSE_FILE" exec -T "$DB_SERVICE" \
    psql -v ON_ERROR_STOP=1 -U ss14 -d "$DB_NAME" \
    -v game_role=ss14 -f /aa/ops/03_game_reads_briefings.sql

echo "Starting Admin Assist sidecar..."
(
    cd "$SCRIPT_DIR"
    export UV_CACHE_DIR="${UV_CACHE_DIR:-${TMPDIR:-/tmp}/admin-assist-uv-cache}"
    exec uv run python -m admin_assist --config "$SERVER_CONFIG"
) >"$SIDECAR_LOG" 2>&1 &
SIDECAR_PID=$!

sidecar_ready=0
for _ in $(seq 1 60); do
    if grep -Fq "started:" "$SIDECAR_LOG"; then
        sidecar_ready=1
        break
    fi
    if ! kill -0 "$SIDECAR_PID" 2>/dev/null; then
        cat "$SIDECAR_LOG" >&2
        fail "Admin Assist sidecar exited during startup"
    fi
    sleep 1
done
if [[ "$sidecar_ready" != "1" ]]; then
    cat "$SIDECAR_LOG" >&2
    fail "sidecar did not start within one minute"
fi

echo "Building clients..."
(
    cd "$REPO_ROOT"
    dotnet build Content.Trauma.Client -c Release
)

echo "Starting admin client on localhost and non-admin client on $LAN_IP..."
(
    cd "$REPO_ROOT"
    exec dotnet run --project Content.Trauma.Client -c Release --no-build -- \
        --connect --connect-address udp://127.0.0.1:1212
) >"$ADMIN_LOG" 2>&1 &
ADMIN_PID=$!
(
    cd "$REPO_ROOT"
    exec dotnet run --project Content.Trauma.Client -c Release --no-build -- \
        --connect --connect-address "udp://$LAN_IP:1212"
) >"$GUEST_LOG" 2>&1 &
GUEST_PID=$!

echo "Playtest is running. Send A-Help with F1 from the non-admin client."
echo "Press Ctrl+C here to stop the server, sidecar, and both clients."
echo "Log files: $RUN_LOG_DIR"
tail -F "$SERVER_LOG" "$SIDECAR_LOG" "$ADMIN_LOG" "$GUEST_LOG" &
LOG_TAIL_PID=$!

while kill -0 "$SERVER_PID" 2>/dev/null && kill -0 "$SIDECAR_PID" 2>/dev/null; do
    sleep 2
done
echo "Server or sidecar stopped; shutting down the remaining playtest processes."
