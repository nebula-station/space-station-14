#!/usr/bin/env python3

# Import future so people on py2 still get the clear error that they need to upgrade.
from __future__ import print_function
import os
import subprocess
import sys

from pathlib import Path

ROOT = Path(__file__).resolve().parent
CLIENT = "Content.Trauma.Client"
SERVER = "Content.Trauma.Server"
CONFIGS = ["Debug", "Release", "Tools"]
WINDOWS = os.name == "nt"

if WINDOWS:
    os.system("")

RESET = "\033[0m"
TITLE = "\033[1;38;5;183m"
ACCENT = "\033[1;38;5;141m"
TEXT = "\033[38;5;189m"
DIM = "\033[38;5;97m"
FAIL = "\033[1;38;5;204m"

STRINGS = {
    "ru": {
        "not_found": "{} не найден в PATH.",
        "exit_code": "Команда завершилась с кодом {}.",
        "pull_failed": "Pull не удался: закоммить/спрячь локальные изменения или сделай rebase вручную.",
        "with_build": "со сборкой",
        "no_build": "без сборки",
        "config": "конфиг",
        "mode": "режим",
        "language": "язык",
        "run": "запуск",
        "client": "Клиент",
        "server": "Сервер",
        "both": "Сервер + клиент",
        "build_only": "Только сборка",
        "settings": "настройки",
        "switch_config": "Сменить конфиг",
        "toggle_build": "Сборка перед запуском вкл/выкл",
        "switch_language": "Сменить язык (language)",
        "update": "обновление",
        "update_repo": "Обновить репозиторий (git pull + движок)",
        "update_engine": "Обновить двигатель (закреплённая версия)",
        "quit": "Выход",
        "done": "✦ готово",
        "failed": "✘ ошибка",
        "press_key": "нажми любую клавишу...",
    },
    "en": {
        "not_found": "{} not found in PATH.",
        "exit_code": "Command exited with code {}.",
        "pull_failed": "Pull failed: commit/stash local changes or rebase manually.",
        "with_build": "with build",
        "no_build": "no build",
        "config": "config",
        "mode": "mode",
        "language": "language",
        "run": "run",
        "client": "Client",
        "server": "Server",
        "both": "Server + client",
        "build_only": "Build only",
        "settings": "settings",
        "switch_config": "Switch config",
        "toggle_build": "Toggle build before run",
        "switch_language": "Switch language (язык)",
        "update": "update",
        "update_repo": "Update repo (git pull + engine)",
        "update_engine": "Update engine (pinned version)",
        "quit": "Quit",
        "done": "✦ done",
        "failed": "✘ failed",
        "press_key": "press any key...",
    },
}
LANGUAGES = {"ru": "Русский", "en": "English"}
lang = "ru"

CYRILLIC_KEYS = dict(zip("йцукенгшщзфывапролдячсмить", "qwertyuiopasdfghjklzxcvbnm"))


def t(key) -> str:
    return STRINGS[lang][key]


def getch() -> str:
    if WINDOWS:
        import msvcrt
        ch = msvcrt.getwch()
        if ch in ("\x00", "\xe0"):
            msvcrt.getwch()
            return ""
        return ch

    import termios
    import tty
    fd = sys.stdin.fileno()
    old = termios.tcgetattr(fd)
    try:
        tty.setraw(fd)
        return sys.stdin.read(1)
    finally:
        termios.tcsetattr(fd, termios.TCSADRAIN, old)


def run(cmd, cwd=ROOT) -> bool:
    print("{}$ {}{}".format(DIM, " ".join(cmd), RESET))
    sys.stdout.flush()
    try:
        code = subprocess.run(cmd, cwd=str(cwd)).returncode
    except KeyboardInterrupt:
        return False
    except FileNotFoundError:
        print("{}{}{}".format(FAIL, t("not_found").format(cmd[0]), RESET))
        return False
    if code != 0:
        print("{}{}{}".format(FAIL, t("exit_code").format(code), RESET))
    return code == 0


def setup() -> bool:
    return run([sys.executable, "git_helper.py"], cwd=ROOT / "BuildChecker")


def update_repo() -> bool:
    if not run(["git", "pull", "--ff-only"]):
        print("{}{}{}".format(FAIL, t("pull_failed"), RESET))
        return False
    return setup()


def build(config) -> bool:
    return run(["dotnet", "build", "-c", config])


def launch(projects, config, rebuild) -> bool:
    if rebuild and not build(config):
        return False

    procs = []
    for project in projects:
        cmd = ["dotnet", "run", "--no-build", "--project", project, "-c", config]
        print("{}$ {}{}".format(DIM, " ".join(cmd), RESET))
        flags = subprocess.CREATE_NEW_CONSOLE if WINDOWS and len(projects) > 1 and project == SERVER else 0
        procs.append(subprocess.Popen(cmd, cwd=str(ROOT), creationflags=flags))

    try:
        for proc in procs:
            proc.wait()
    except KeyboardInterrupt:
        for proc in procs:
            proc.terminate()
        for proc in procs:
            proc.wait()
    return all(proc.returncode == 0 for proc in procs)


def draw(config, rebuild):
    configs = "  ".join(
        "{}[{}]{}".format(ACCENT, c, RESET) if c == config else "{} {} {}".format(DIM, c, RESET)
        for c in CONFIGS)
    mode = "{}{}{}".format(ACCENT, t("with_build"), RESET) if rebuild else "{}{}{}".format(DIM, t("no_build"), RESET)
    languages = "  ".join(
        "{}[{}]{}".format(ACCENT, name, RESET) if code == lang else "{} {} {}".format(DIM, name, RESET)
        for code, name in LANGUAGES.items())

    def header(label):
        return "{}  ── {} {}{}".format(DIM, label, "─" * (44 - len(label)), RESET)

    def field(label, value):
        return "  {}{:<9}{} {}".format(TEXT, label, RESET, value)

    def item(key, label):
        return "  {}{:>3}{}  {}{}{}".format(ACCENT, key, RESET, TEXT, label, RESET)

    lines = [
        "\033[2J\033[H",
        "{}  ✦ ─────────────── N E B U L A ─────────────── ✦{}".format(TITLE, RESET),
        "",
        field(t("config"), configs),
        field(t("mode"), mode),
        field(t("language"), languages),
        "",
        header(t("run")),
        item("1", t("client")),
        item("2", t("server")),
        item("3", t("both")),
        item("4", t("build_only")),
        "",
        header(t("settings")),
        item("c", t("switch_config")),
        item("b", t("toggle_build")),
        item("l", t("switch_language")),
        "",
        header(t("update")),
        item("u", t("update_repo")),
        item("e", t("update_engine")),
        "",
        item("q", t("quit")),
        "",
    ]
    print("\n".join(lines))


def tui():
    global lang
    config = "Debug"
    rebuild = True
    actions = {
        "1": lambda: launch([CLIENT], config, rebuild),
        "2": lambda: launch([SERVER], config, rebuild),
        "3": lambda: launch([SERVER, CLIENT], config, rebuild),
        "4": lambda: build(config),
        "u": update_repo,
        "e": setup,
    }

    while True:
        draw(config, rebuild)
        key = getch()
        key = CYRILLIC_KEYS.get(key.lower(), key)
        if key in ("q", "Q", "\x1b", "\x03", "\x04"):
            print("\033[2J\033[H", end="")
            return
        if key == "c":
            config = CONFIGS[(CONFIGS.index(config) + 1) % len(CONFIGS)]
        elif key == "b":
            rebuild = not rebuild
        elif key == "l":
            codes = list(LANGUAGES)
            lang = codes[(codes.index(lang) + 1) % len(codes)]
        elif key in actions:
            print("\033[2J\033[H", end="")
            ok = actions[key]()
            status = "{}{}{}".format(ACCENT, t("done"), RESET) if ok else "{}{}{}".format(FAIL, t("failed"), RESET)
            print("\n{}  {}{}{}".format(status, DIM, t("press_key"), RESET))
            sys.stdout.flush()
            getch()


if __name__ == "__main__":
    if sys.stdin.isatty() and sys.stdout.isatty():
        tui()
    else:
        sys.exit(0 if setup() else 1)
