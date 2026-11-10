#!/usr/bin/env python3
"""Fixes tile variants in map files after an upstream (Trauma) merge without resaving them, so later merges don't conflict.

A saved tile variant >= the tile prototype's `variants` is drawn as the missing tile texture.
Out-of-range cells get a variant re-picked by `placementVariants`, like the `variantize` command.
Only the tile chunk lines change.

Usage, from the repo root:
  python3 Tools/_Nebula/fix_maps.py [--check] [path ...]
    path          map file or folder under Resources/, default Maps
    --check       report only, exit 1 if anything needs fixing
"""
import argparse
import base64
import collections
import glob
import os
import random
import re
import struct
import sys

import yaml

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
RES = os.path.join(ROOT, "Resources")


class Loader(yaml.SafeLoader):
    pass


Loader.add_multi_constructor("!", lambda l, s, n: l.construct_mapping(n) if isinstance(n, yaml.MappingNode) else None)


# Tiles

def load_tiles():
    tiles, alias = {}, {}
    for f in glob.glob(os.path.join(RES, "Prototypes", "**", "*.yml"), recursive=True):
        try:
            docs = yaml.load(open(f, encoding="utf-8-sig"), Loader=Loader)
        except yaml.YAMLError:
            continue
        for d in docs if isinstance(docs, list) else []:
            if not isinstance(d, dict) or not isinstance(d.get("id"), str):
                continue
            if d.get("type") == "tile":
                tiles[d["id"]] = d
            elif d.get("type") == "tileAlias":
                alias[d["id"]] = d.get("target")
    return tiles, alias


def tile_field(tiles, tid, key, seen=()):
    d = tiles.get(tid)
    if not d or tid in seen:
        return None
    if key in d:
        return d[key]
    parents = d.get("parent")
    for p in parents if isinstance(parents, list) else [parents] if parents else []:
        v = tile_field(tiles, p, key, seen + (tid,))
        if v is not None:
            return v
    return None


def pick_variant(weights, count, rnd):
    if not weights:
        return rnd.randrange(count)
    r = rnd.random() * sum(weights)
    acc = 0.0
    for i, w in enumerate(weights):
        acc += w
        if acc >= r:
            return i
    return 0


CHUNK = re.compile(r"(tiles: )([A-Za-z0-9+/=]+)(\n\s+version: (\d+))")


def fix_tiles(text, path, tiles, alias, fix):
    m = re.search(r"^tilemap:\n((?:  .*\n)+)", text, re.M)
    if not m:
        return text, 0
    tilemap = {int(a): b.strip().strip('"') for a, b in re.findall(r"^\s+(-?\d+): (.+)$", m.group(1), re.M)}
    rnd = random.Random(path)
    count = 0

    def repl(cm):
        nonlocal count
        version = int(cm.group(4))
        raw = bytearray(base64.b64decode(cm.group(2)))
        step = 7 if version >= 7 else 6 if version >= 6 else 4
        var_off = 5 if version >= 6 else 3
        for o in range(0, len(raw) - step + 1, step):
            tid = struct.unpack_from("<i" if version >= 6 else "<H", raw, o)[0]
            name = tilemap.get(tid)
            name = alias.get(name, name)
            if name not in tiles:
                continue
            variants = tile_field(tiles, name, "variants") or 1
            if raw[o + var_off] < variants:
                continue
            count += 1
            if fix:
                raw[o + var_off] = pick_variant(tile_field(tiles, name, "placementVariants"), variants, rnd)
        return cm.group(1) + base64.b64encode(bytes(raw)).decode() + cm.group(3)

    new = CHUNK.sub(repl, text)
    return (new if fix else text), count


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("paths", nargs="*", default=["Maps"])
    ap.add_argument("--check", action="store_true")
    args = ap.parse_args()
    paths = [p.strip("/") for p in args.paths]
    fix = not args.check

    files = set()
    for p in paths:
        full = os.path.join(RES, p)
        if full.endswith(".yml"):
            files.add(full)
        else:
            files.update(glob.glob(os.path.join(full, "**", "*.yml"), recursive=True))
    tiles, alias = load_tiles()

    total = collections.Counter()
    for path in sorted(files):
        res_path = "/" + os.path.relpath(path, RES).replace(os.sep, "/")
        text = open(path, encoding="utf-8-sig").read()
        new, bad_tiles = fix_tiles(text, path, tiles, alias, fix)
        if bad_tiles:
            print(f"{res_path}: tiles {bad_tiles}")
            total.update(files=1, tiles=bad_tiles)
        if fix and new != text:
            with open(path, "w", encoding="utf-8") as f:
                f.write(new)

    verb = "fixed" if fix else "need fixing"
    print(f"{verb}: {total['tiles']} tiles in {total['files']} files")
    sys.exit(1 if args.check and total["files"] else 0)


if __name__ == "__main__":
    main()
