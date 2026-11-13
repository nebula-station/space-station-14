from __future__ import annotations

import pytest

from admin_assist.__main__ import check, read_rules
from admin_assist.config import ConfigError, load_config

from .conftest import TEST_CVARS

DSNS = {"admin_assist.game_dsn": "postgresql://ro@db/game", "admin_assist.own_dsn": "postgresql://rw@db/game"}


def test_rules_file_must_be_set_readable_and_not_empty(tmp_path):
    with pytest.raises(ConfigError, match="admin_assist.rules_file is not set"):
        read_rules(load_config(TEST_CVARS))
    with pytest.raises(ConfigError, match="cannot be read"):
        read_rules(load_config({**TEST_CVARS, "admin_assist.rules_file": str(tmp_path / "nope.txt")}))
    path = tmp_path / "rules.txt"
    path.write_text(" \n", encoding="utf-8")
    with pytest.raises(ConfigError, match="is empty"):
        read_rules(load_config({**TEST_CVARS, "admin_assist.rules_file": str(path)}))
    path.write_text("\n1.1 Не гриферить.\n", encoding="utf-8")
    assert read_rules(load_config({**TEST_CVARS, "admin_assist.rules_file": str(path)})) == "1.1 Не гриферить."


def test_check_names_what_is_missing():
    check(load_config({**TEST_CVARS, **DSNS}))
    with pytest.raises(ConfigError, match="admin_assist.game_dsn"):
        check(load_config({**TEST_CVARS, "admin_assist.own_dsn": DSNS["admin_assist.own_dsn"]}))
    with pytest.raises(ConfigError, match="admin_assist.own_dsn"):
        check(load_config({**TEST_CVARS, "admin_assist.game_dsn": DSNS["admin_assist.game_dsn"]}))
    with pytest.raises(ConfigError, match="admin_assist.kidmesh_key"):
        check(load_config(DSNS))
