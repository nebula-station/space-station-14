import pytest

from admin_assist.config import DEFAULT_FEATURES, ConfigError, load_config, read_cvars


def test_defaults_match_the_brief():
    cfg = load_config({})
    assert cfg.kidmesh_base_url == "https://api.kidmesh.best/v1"
    assert (cfg.models["main"].id, cfg.models["main"].multiplier) == ("deepseek-v4.1-flash-1017", 1.0)
    assert (cfg.models["fallback"].id, cfg.models["fallback"].multiplier) == ("glm-5.3", 2.5)
    assert (cfg.models["small"].id, cfg.models["small"].multiplier) == ("gpt-6-luna", 0.9)
    assert cfg.price_rub_per_mtok == 4.35
    assert cfg.cached_input_factor == 0.5
    assert cfg.budget_rub == 500
    assert cfg.budget_alert_fraction == 0.8
    assert cfg.budget_exempt == {"ahelp_summary"}
    assert cfg.erp_text_to_model is False
    assert set(cfg.features) == set(DEFAULT_FEATURES)
    assert (cfg.features["ahelp_summary"].tier, cfg.features["ahelp_summary"].effort) == ("main", "low")
    assert (cfg.features["done_typing"].tier, cfg.features["done_typing"].effort) == ("main", "none")
    assert cfg.kidmesh_fold_system is True


def test_one_master_key_serves_every_tier():
    cfg = load_config({"admin_assist.kidmesh_key": "master", "admin_assist.model_fallback_key": "own"})
    assert [cfg.models[t].api_key for t in ("main", "fallback", "small")] == ["master", "own", "master"]
    assert "master" not in repr(cfg)


def test_overrides():
    cfg = load_config(
        {
            "admin_assist.model_main_id": "deepseek-v4.1-flash-1201",
            "admin_assist.model_main_key": "k",
            "admin_assist.feature_ahelp_summary": "fallback:medium",
            "admin_assist.budget_rub": "250",
            "admin_assist.erp_text_to_model": "true",
            "admin_assist.kidmesh_base_url": "http://localhost:9/v1/",
        }
    )
    assert cfg.models["main"].id == "deepseek-v4.1-flash-1201"
    assert cfg.models["main"].api_key == "k"
    assert (cfg.features["ahelp_summary"].tier, cfg.features["ahelp_summary"].effort) == ("fallback", "medium")
    assert cfg.budget_rub == 250
    assert cfg.erp_text_to_model is True
    assert cfg.kidmesh_base_url == "http://localhost:9/v1"


@pytest.mark.parametrize("raw", ["huge:low", "main:extreme", ":low"])
def test_bad_feature_override_is_rejected(raw):
    with pytest.raises(ConfigError):
        load_config({"admin_assist.feature_done_typing": raw})



def test_reads_cvars_from_the_server_toml(tmp_path):
    path = tmp_path / "server_config.toml"
    path.write_text(
        '[admin_assist]\nmodel_main_key = "k"\nbudget_rub = 250\nerp_text_to_model = true\n'
        '[nebula]\nisolate_records = true\n[adminlogs]\nserver_name = "nebula-1"\n',
        encoding="utf-8",
    )
    cfg = load_config(read_cvars(path))
    assert cfg.models["main"].api_key == "k"
    assert cfg.budget_rub == 250
    assert cfg.erp_text_to_model is True
    assert (cfg.isolate_records, cfg.server_name) == (True, "nebula-1")


def test_unreadable_server_config_is_a_config_error(tmp_path):
    with pytest.raises(ConfigError, match="cannot be read"):
        read_cvars(tmp_path / "nope.toml")
    bad = tmp_path / "bad.toml"
    bad.write_text("[admin_assist\n", encoding="utf-8")
    with pytest.raises(ConfigError, match="not valid TOML"):
        read_cvars(bad)
