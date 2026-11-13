-- psql -d <game_db> \
--      -v ro_password='...' -v rw_password='...' \
--      -f 01_roles.sql

\set ON_ERROR_STOP on

SELECT 'CREATE ROLE admin_assist_ro' WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'admin_assist_ro') \gexec
SELECT 'CREATE ROLE admin_assist_rw' WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'admin_assist_rw') \gexec

ALTER ROLE admin_assist_ro WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS
    CONNECTION LIMIT 6 PASSWORD :'ro_password';
ALTER ROLE admin_assist_rw WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS
    CONNECTION LIMIT 6 PASSWORD :'rw_password';

ALTER ROLE admin_assist_ro SET statement_timeout = '5s';
ALTER ROLE admin_assist_ro SET lock_timeout = '2s';
ALTER ROLE admin_assist_ro SET idle_in_transaction_session_timeout = '30s';
ALTER ROLE admin_assist_ro SET default_transaction_read_only = on;

ALTER ROLE admin_assist_rw SET statement_timeout = '30s';
ALTER ROLE admin_assist_rw SET lock_timeout = '5s';
ALTER ROLE admin_assist_rw SET idle_in_transaction_session_timeout = '60s';
ALTER ROLE admin_assist_rw SET search_path = admin_assist;

SELECT format('GRANT CONNECT ON DATABASE %I TO admin_assist_ro, admin_assist_rw', current_database()) \gexec

GRANT USAGE ON SCHEMA public TO admin_assist_ro;
REVOKE ALL ON ALL TABLES IN SCHEMA public FROM admin_assist_ro;
GRANT SELECT ON
    public.admin_log,
    public.admin_log_player,
    public.round,
    public.server,
    public.play_time,
    public.ahelp_message,
    public.ahelp_briefing_vote,
    public.ahelp_ticket_request,
    public.admin_notes,
    public.admin_watchlists,
    public.ban,
    public.ban_player,
    public.ban_role,
    public.unban
    TO admin_assist_ro;
GRANT SELECT (user_id, first_seen_time, last_seen_user_name, last_seen_time,
              last_seen_address, last_seen_hwid, last_seen_hwid_type)
    ON public.player TO admin_assist_ro;
GRANT SELECT (ban_id, hwid, hwid_type) ON public.ban_hwid TO admin_assist_ro;
GRANT SELECT (ban_id, address) ON public.ban_address TO admin_assist_ro;

CREATE SCHEMA IF NOT EXISTS admin_assist AUTHORIZATION admin_assist_rw;
REVOKE ALL ON SCHEMA admin_assist FROM PUBLIC;
