-- psql -d <game_db> -f 02_admin_assist_schema.sql

\set ON_ERROR_STOP on

SET ROLE admin_assist_rw;

CREATE TABLE IF NOT EXISTS admin_assist.llm_ledger (
    id                bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    at                timestamptz NOT NULL DEFAULT now(),
    feature           text NOT NULL,
    model             text NOT NULL,
    prompt_tokens     integer NOT NULL,
    cached_tokens     integer NOT NULL,
    completion_tokens integer NOT NULL,
    cost_rub          numeric(12, 6) NOT NULL,
    fell_back         boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS llm_ledger_at_idx ON admin_assist.llm_ledger (at);

CREATE TABLE IF NOT EXISTS admin_assist.budget_alert (
    month date NOT NULL,
    level integer NOT NULL,
    at    timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (month, level)
);

CREATE TABLE IF NOT EXISTS admin_assist.ingest_cursor (
    name       text PRIMARY KEY,
    position   bigint NOT NULL,
    round_id   integer,
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS admin_assist.ahelp_ticket (
    ticket_id              bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    owner_user_id          uuid NOT NULL,
    round_id               integer,
    server_id              integer,
    first_message_id       bigint NOT NULL UNIQUE,
    last_message_id        bigint NOT NULL,
    opened_at              timestamptz NOT NULL,
    last_message_at        timestamptz NOT NULL,
    opened_by_staff        boolean NOT NULL DEFAULT false,
    last_player_message_id bigint,
    last_player_message_at timestamptz,
    first_staff_reply_at   timestamptz,
    handled_through_id     bigint,
    summaries              integer NOT NULL DEFAULT 0,
    priority               text,
    stuck_alert_at         timestamptz
);
ALTER TABLE admin_assist.ahelp_ticket ADD COLUMN IF NOT EXISTS briefing_started_at timestamptz;
ALTER TABLE admin_assist.ahelp_ticket ADD COLUMN IF NOT EXISTS briefing_trigger text;
CREATE INDEX IF NOT EXISTS ahelp_ticket_last_message_idx ON admin_assist.ahelp_ticket (last_message_at);
CREATE INDEX IF NOT EXISTS ahelp_ticket_owner_idx ON admin_assist.ahelp_ticket (owner_user_id, opened_at);

CREATE TABLE IF NOT EXISTS admin_assist.ahelp_summary (
    summary_id         bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    ticket_id          bigint NOT NULL REFERENCES admin_assist.ahelp_ticket ON DELETE CASCADE,
    created_at         timestamptz NOT NULL DEFAULT now(),
    through_message_id bigint NOT NULL,
    trigger            text NOT NULL,
    model              text NOT NULL,
    prompt_version     integer NOT NULL,
    text               text NOT NULL,
    priority           text,
    kind               text,
    unreliable         boolean NOT NULL,
    problems           text[] NOT NULL DEFAULT '{}',
    round_id           integer,
    citations          jsonb NOT NULL DEFAULT '{}',
    attempts           integer NOT NULL,
    cost_rub           numeric(12, 6) NOT NULL,
    delivered_at       timestamptz,
    delivery_ref       text
);
ALTER TABLE admin_assist.ahelp_summary ADD COLUMN IF NOT EXISTS participants uuid[] NOT NULL DEFAULT '{}';
CREATE INDEX IF NOT EXISTS ahelp_summary_ticket_idx ON admin_assist.ahelp_summary (ticket_id);
CREATE INDEX IF NOT EXISTS ahelp_summary_delivered_idx ON admin_assist.ahelp_summary (delivered_at);

CREATE TABLE IF NOT EXISTS admin_assist.summary_feedback (
    summary_id bigint NOT NULL REFERENCES admin_assist.ahelp_summary ON DELETE CASCADE,
    voter      text NOT NULL,
    vote       smallint NOT NULL CHECK (vote IN (-1, 1)),
    at         timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (summary_id, voter)
);

RESET ROLE;
