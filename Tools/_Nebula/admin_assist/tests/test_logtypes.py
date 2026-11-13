import importlib.util
from pathlib import Path

from admin_assist.logtypes import LogImpact, LogType

SCRIPT = Path(__file__).resolve().parents[1] / "scripts" / "gen_logtypes.py"


def _gen():
    spec = importlib.util.spec_from_file_location("gen_logtypes", SCRIPT)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def test_generated_file_matches_the_csharp_enums():
    gen = _gen()
    assert gen.OUT.read_text(encoding="utf-8") == gen.render(), "stale: run scripts/gen_logtypes.py"


def test_known_values():
    assert LogType.Damaged == 2
    assert LogType.Chat.name == "Chat"
    assert (LogImpact.Low, LogImpact.Medium, LogImpact.High, LogImpact.Extreme) == (-1, 0, 1, 2)
    assert len(LogType) >= 112
