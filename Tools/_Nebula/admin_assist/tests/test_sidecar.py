from __future__ import annotations

import asyncio
import io
import os
import re
import shutil
import subprocess
import sys
import tomllib
from pathlib import Path

import pytest

import admin_assist
from admin_assist.__main__ import watch_stdin
from admin_assist.config import ConfigError, read_stdin_cvars

ROOT = Path(__file__).resolve().parent.parent


def framed(text: str) -> io.BytesIO:
    data = text.encode("utf-8")
    return io.BytesIO(f"{len(data)}\n".encode("ascii") + data + b"rest stays in the pipe")


def test_stdin_config_is_read_by_its_length():
    stream = framed('[admin_assist]\nkidmesh_key = "a\\"b\\\\c\\nё"\npoll_s = 2.5\n[adminlogs]\nserver_name = "pegas"\n')
    cvars = read_stdin_cvars(stream)
    assert cvars == {"admin_assist.kidmesh_key": 'a"b\\c\nё', "admin_assist.poll_s": 2.5, "adminlogs.server_name": "pegas"}
    assert stream.read() == b"rest stays in the pipe"


@pytest.mark.parametrize(
    ("data", "error"),
    [
        (b"", "expected the config length"),
        (b"abc\n[x]\n", "expected the config length"),
        (b"100\n[x]\n", "cut short"),
        (b"5\n[x]\nb", "not valid TOML"),
    ],
)
def test_bad_stdin_config_is_a_config_error(data, error):
    with pytest.raises(ConfigError, match=error):
        read_stdin_cvars(io.BytesIO(data))


async def test_stop_is_called_once_stdin_closes():
    read_fd, write_fd = os.pipe()
    stopped = asyncio.Event()
    with os.fdopen(read_fd, "rb", buffering=0) as stream:
        thread = watch_stdin(stream, asyncio.get_running_loop(), stopped.set)
        os.write(write_fd, b"still open")
        await asyncio.sleep(0.05)
        assert not stopped.is_set()
        os.close(write_fd)
        await asyncio.wait_for(stopped.wait(), 5)
        thread.join(5)
        assert not thread.is_alive()


def _layout(tmp_path: Path, versions: list[str]) -> Path:
    shutil.copytree(Path(admin_assist.__file__).parent, tmp_path / "admin_assist", ignore=shutil.ignore_patterns("__pycache__"))
    for version in versions:
        (tmp_path / "lib" / version).mkdir(parents=True)
    return tmp_path


def _import(cwd: Path, code: str) -> subprocess.CompletedProcess[str]:
    env = {k: v for k, v in os.environ.items() if k not in ("PYTHONPATH", "PYTHONSAFEPATH")}
    return subprocess.run([sys.executable, "-c", code], cwd=cwd, env=env, capture_output=True, text=True, timeout=30)


def test_bundled_libraries_for_this_python_are_put_on_the_path(tmp_path):
    here = f"python{sys.version_info.major}.{sys.version_info.minor}"
    root = _layout(tmp_path, [here, "python3.0"])
    (root / "lib" / here / "aa_vendor_probe.py").write_text("OK = 1\n")
    result = _import(root, "import admin_assist, aa_vendor_probe; print(aa_vendor_probe.OK)")
    assert result.returncode == 0, result.stderr
    assert result.stdout.strip() == "1"


def test_missing_bundle_for_this_python_names_the_bundled_ones(tmp_path):
    root = _layout(tmp_path, ["python3.0", "python3.1"])
    result = _import(root, "import admin_assist")
    assert result.returncode == 1
    assert "no bundled libraries for Python" in result.stderr
    assert "python3.0, python3.1" in result.stderr


def test_requirements_pin_every_locked_runtime_package_without_markers():
    lock = tomllib.loads((ROOT / "uv.lock").read_text(encoding="utf-8"))
    packages = {p["name"]: p for p in lock["package"]}

    def runtime(name: str, seen: set[str]) -> set[str]:
        if name in seen:
            return seen
        seen.add(name)
        for dep in packages[name].get("dependencies", []):
            runtime(dep["name"], seen)
        return seen

    wanted = {name: packages[name]["version"] for name in runtime("admin-assist", set()) - {"admin-assist"}}
    text = (ROOT / "requirements.txt").read_text(encoding="utf-8")
    pins = dict(re.findall(r"^([A-Za-z0-9_.-]+)==([^\s;\\]+)", text, re.MULTILINE))
    assert pins == wanted
    assert ";" not in text, "markers are judged by the packaging machine's Python, not the server's: drop them"
    for name in pins:
        block = text.split(f"{name}==", 1)[1].split("\n", 1)[1]
        assert block.lstrip().startswith("--hash=sha256:"), name
