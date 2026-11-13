-- psql -d <game_db> -v game_role='<role the game server connects as>' -f 03_game_reads_briefings.sql

\set ON_ERROR_STOP on

SET ROLE admin_assist_rw;

SELECT format('GRANT USAGE ON SCHEMA admin_assist TO %I', :'game_role') \gexec
SELECT format('GRANT SELECT ON admin_assist.ahelp_summary, admin_assist.ahelp_ticket TO %I', :'game_role') \gexec
