#!/usr/bin/env python3
"""
Generates Nebula UI icons from the Material Symbols Rounded font.

For every glyph name this writes, into the output directory:
  <name>.svg                   white glyph on a 960 unit viewBox
  <name>.svg.192dpi.png        48x48 render made with rsvg-convert
  <name>.svg.192dpi.png.yml    texture metadata with filtering on

Usage:
  python3 Tools/_Nebula/svg_icons.py Resources/Textures/_Nebula/Interface/Lobby group check_circle
  python3 Tools/_Nebula/svg_icons.py --check ghost location_on
  python3 Tools/_Nebula/svg_icons.py --search pin

Needs rsvg-convert (librsvg) and the Material Symbols variable font
(Arch: ttf-material-symbols-variable-git). fontTools is installed into a cached venv on first use.
"""

import argparse
import os
import shutil
import subprocess
import sys
import venv
from pathlib import Path

DEFAULT_FONT = "/usr/share/fonts/ttf-material-symbols-variable/MaterialSymbolsRounded[FILL,GRAD,opsz,wght].ttf"
PNG_SIZE = 48
PNG_META = "sample:\n  filter: true\n"
SVG_TEMPLATE = (
    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 -960 960 960">'
    '<path fill="#ffffff" transform="scale(1,-1)" d="{path}"/></svg>\n'
)


def ensure_fonttools() -> None:
    try:
        import fontTools  # noqa: F401
        return
    except ImportError:
        pass

    cache = Path(os.environ.get("XDG_CACHE_HOME", Path.home() / ".cache")) / "nebula-icons"
    python = cache / "venv" / "bin" / "python"

    if Path(sys.executable).resolve() == python.resolve():
        sys.exit("fontTools is missing even inside the cached venv, delete it and retry: " + str(cache))

    if not python.exists():
        print(f"Installing fontTools into {cache / 'venv'} ...", file=sys.stderr)
        venv.create(cache / "venv", with_pip=True)
        subprocess.run([str(python), "-m", "pip", "install", "-q", "--disable-pip-version-check", "fonttools"], check=True)

    os.execv(str(python), [str(python), "-I", str(Path(__file__).resolve()), *sys.argv[1:]])


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate Nebula icons from Material Symbols Rounded.")
    parser.add_argument("out", nargs="?", help="output directory")
    parser.add_argument("names", nargs="*", help="glyph names, e.g. group check_circle")
    parser.add_argument("--check", action="store_true", help="only report which names exist in the font")
    parser.add_argument("--search", metavar="TEXT", help="list glyph names containing TEXT")
    parser.add_argument("--force", action="store_true", help="overwrite existing icons")
    parser.add_argument("--font", default=os.environ.get("NEBULA_ICON_FONT", DEFAULT_FONT))
    args = parser.parse_args()

    if not Path(args.font).exists():
        sys.exit(f"Font not found: {args.font}\nInstall Material Symbols or pass --font.")

    ensure_fonttools()
    from fontTools.pens.svgPathPen import SVGPathPen
    from fontTools.ttLib import TTFont

    font = TTFont(args.font)
    glyph_names = set(font.getGlyphOrder())

    if args.search:
        matches = sorted(n for n in glyph_names if args.search in n and not n.endswith(".fill"))
        print("\n".join(matches) if matches else "no matches")
        return 0

    names = args.names
    if args.check:
        names = [args.out, *names] if args.out else names
        for name in names:
            print(f"{name}: {'ok' if name in glyph_names else 'MISSING'}")
        return 0 if all(n in glyph_names for n in names) else 1

    if not args.out or not names:
        parser.error("give an output directory and at least one glyph name")

    missing = [n for n in names if n not in glyph_names]
    if missing:
        sys.exit("Not in the font: " + ", ".join(missing) + "\nTry --search to find a similar name.")

    if shutil.which("rsvg-convert") is None:
        sys.exit("rsvg-convert not found, install librsvg.")

    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)

    existing = [n for n in names if (out / f"{n}.svg").exists()]
    if existing and not args.force:
        sys.exit("Already exist (use --force to overwrite): " + ", ".join(existing))

    glyphs = font.getGlyphSet()

    for name in names:
        pen = SVGPathPen(glyphs)
        glyphs[name].draw(pen)

        svg = out / f"{name}.svg"
        png = out / f"{name}.svg.192dpi.png"
        svg.write_text(SVG_TEMPLATE.format(path=pen.getCommands()))
        subprocess.run(["rsvg-convert", "-w", str(PNG_SIZE), "-h", str(PNG_SIZE), str(svg), "-o", str(png)], check=True)
        (out / f"{name}.svg.192dpi.png.yml").write_text(PNG_META)
        print(f"{png}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
