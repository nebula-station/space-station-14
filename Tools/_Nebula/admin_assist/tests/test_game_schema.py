import re
from pathlib import Path

HERE = Path(__file__).resolve()
FIXTURE = HERE.parent / "fixtures" / "game_schema.sql"
MIGRATIONS = HERE.parents[4] / "Content.Server.Database" / "Migrations" / "Postgres"


def test_fixture_is_at_the_newest_migration():
    newest = max(p.name.removesuffix(".Designer.cs") for p in MIGRATIONS.glob("*.Designer.cs"))
    stamp = re.search(r"^-- Schema as of migration: (\S+)$", FIXTURE.read_text(encoding="utf-8"), re.M)
    assert stamp, "tests/fixtures/game_schema.sql has lost its header; run scripts/gen_game_schema.sh"
    assert stamp.group(1) == newest, "game schema fixture is stale; run scripts/gen_game_schema.sh"
