from __future__ import annotations

import tomllib
from collections.abc import Mapping
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, BinaryIO

TIERS = ("main", "fallback", "small")
EFFORTS = ("none", "low", "medium", "high")

DEFAULT_FEATURES: dict[str, tuple[str, str]] = {
    "ahelp_summary": ("main", "low"),
    "done_typing": ("main", "none"),
}

BUDGET_EXEMPT = frozenset({"ahelp_summary"})


class ConfigError(Exception):
    pass


@dataclass(frozen=True)
class ModelConfig:
    id: str
    api_key: str = field(repr=False)
    multiplier: float


@dataclass(frozen=True)
class FeatureConfig:
    tier: str
    effort: str


@dataclass(frozen=True)
class Config:
    game_dsn: str
    own_dsn: str
    db_statement_timeout_ms: int

    kidmesh_base_url: str
    kidmesh_timeout_s: float
    kidmesh_max_retries: int
    kidmesh_pause_s: float
    kidmesh_fold_system: bool
    kidmesh_catalog_url: str
    models: Mapping[str, ModelConfig]
    features: Mapping[str, FeatureConfig]

    price_rub_per_mtok: float
    cached_input_factor: float
    budget_rub: float
    budget_alert_fraction: float
    budget_exempt: frozenset[str]

    erp_text_to_model: bool

    round_state_url: str
    round_state_token: str
    round_state_timeout_s: float

    isolate_records: bool
    server_name: str

    context_before_s: int
    context_after_s: int
    context_max_log_lines: int
    context_max_participants: int
    new_player_hours: float

    rules_file: str
    poll_s: float
    detector_debounce_s: float
    detector_force_s: float
    ticket_gap_s: float
    ticket_stale_s: float
    ticket_max_summaries: int
    summary_max_tokens: int
    max_parallel_calls: int

    feedback_poll_s: float
    catalog_poll_s: float


def _bool(value: Any) -> bool:
    if isinstance(value, bool):
        return value
    return str(value).strip().lower() in ("1", "true", "yes", "on")


def _feature(name: str, raw: str) -> FeatureConfig:
    tier, _, effort = raw.partition(":")
    tier, effort = tier.strip(), (effort.strip() or "none")
    if tier not in TIERS or effort not in EFFORTS:
        raise ConfigError(f"admin_assist.feature_{name}={raw!r}: expected <{'|'.join(TIERS)}>:<{'|'.join(EFFORTS)}>")
    return FeatureConfig(tier, effort)


def read_cvars(path: str | Path) -> dict[str, Any]:
    try:
        with open(path, "rb") as f:
            data = f.read()
    except OSError as e:
        raise ConfigError(f"server config {str(path)!r} cannot be read: {e}") from None
    return parse_cvars(data, repr(str(path)))


def read_stdin_cvars(stream: BinaryIO) -> dict[str, Any]:
    header = stream.readline()
    try:
        size = int(header)
    except ValueError:
        raise ConfigError("stdin: expected the config length in bytes on the first line") from None
    data = stream.read(size)
    if len(data) != size:
        raise ConfigError(f"stdin: config cut short, got {len(data)} of {size} bytes")
    return parse_cvars(data, "from stdin")


def parse_cvars(data: bytes, source: str) -> dict[str, Any]:
    try:
        tables = tomllib.loads(data.decode("utf-8"))
    except (UnicodeDecodeError, tomllib.TOMLDecodeError) as e:
        raise ConfigError(f"server config {source} is not valid TOML: {e}") from None
    return {
        f"{table}.{key}": value
        for table, values in tables.items()
        if isinstance(values, dict)
        for key, value in values.items()
    }


def load_config(cvars: Mapping[str, Any]) -> Config:
    def raw(name: str, default: Any) -> Any:
        value = cvars.get(name)
        return default if value is None or value == "" else value

    def get(key: str, default: Any) -> Any:
        return raw(f"admin_assist.{key}", default)

    shared_key = str(get("kidmesh_key", ""))

    models = {}
    for tier, default_id, default_mult in (
        ("main", "deepseek-v4.1-flash-1017", 1.0),
        ("fallback", "glm-5.3", 2.5),
        ("small", "gpt-6-luna", 0.9),
    ):
        prefix = f"model_{tier}_"
        models[tier] = ModelConfig(
            id=str(get(prefix + "id", default_id)),
            api_key=str(get(prefix + "key", shared_key)),
            multiplier=float(get(prefix + "multiplier", default_mult)),
        )

    features = {}
    for name, (tier, effort) in DEFAULT_FEATURES.items():
        features[name] = _feature(name, str(get(f"feature_{name}", f"{tier}:{effort}")))

    return Config(
        game_dsn=str(get("game_dsn", "")),
        own_dsn=str(get("own_dsn", "")),
        db_statement_timeout_ms=int(get("db_statement_timeout_ms", 5000)),
        kidmesh_base_url=str(get("kidmesh_base_url", "https://api.kidmesh.best/v1")).rstrip("/"),
        kidmesh_timeout_s=float(get("kidmesh_timeout_s", 30)),
        kidmesh_max_retries=int(get("kidmesh_max_retries", 2)),
        kidmesh_pause_s=float(get("kidmesh_pause_s", 600)),
        kidmesh_fold_system=_bool(get("kidmesh_fold_system", True)),
        kidmesh_catalog_url=str(cvars.get("admin_assist.kidmesh_catalog_url", "https://kidmesh.best/api/models")),
        models=models,
        features=features,
        price_rub_per_mtok=float(get("price_rub_per_mtok", 4.35)),
        cached_input_factor=float(get("cached_input_factor", 0.5)),
        budget_rub=float(get("budget_rub", 500)),
        budget_alert_fraction=float(get("budget_alert_fraction", 0.8)),
        budget_exempt=BUDGET_EXEMPT,
        erp_text_to_model=_bool(get("erp_text_to_model", False)),
        round_state_url=str(get("round_state_url", "")),
        round_state_token=str(get("round_state_token", "")),
        round_state_timeout_s=float(get("round_state_timeout_s", 3)),
        isolate_records=_bool(raw("nebula.isolate_records", False)),
        server_name=str(raw("adminlogs.server_name", "")),
        context_before_s=int(get("context_before_s", 300)),
        context_after_s=int(get("context_after_s", 10)),
        context_max_log_lines=int(get("context_max_log_lines", 150)),
        context_max_participants=int(get("context_max_participants", 6)),
        new_player_hours=float(get("new_player_hours", 10)),
        rules_file=str(get("rules_file", "")),
        poll_s=float(get("poll_s", 2)),
        detector_debounce_s=float(get("detector_debounce_s", 6)),
        detector_force_s=float(get("detector_force_s", 75)),
        ticket_gap_s=float(get("ticket_gap_s", 900)),
        ticket_stale_s=float(get("ticket_stale_s", 600)),
        ticket_max_summaries=int(get("ticket_max_summaries", 3)),
        summary_max_tokens=int(get("summary_max_tokens", 4000)),
        max_parallel_calls=int(get("max_parallel_calls", 3)),
        feedback_poll_s=float(get("feedback_poll_s", 300)),
        catalog_poll_s=float(get("catalog_poll_s", 600)),
    )
