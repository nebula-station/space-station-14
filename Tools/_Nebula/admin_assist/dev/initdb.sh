#!/bin/sh
set -e

run() { psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" "$@"; }

run -f /aa/fixtures/game_schema.sql
run -v ro_password=local_ro -v rw_password=local_rw -v game_role="$POSTGRES_USER" -f /aa/ops/01_roles.sql
run -f /aa/ops/02_admin_assist_schema.sql
run -v game_role="$POSTGRES_USER" -f /aa/ops/03_game_reads_briefings.sql
