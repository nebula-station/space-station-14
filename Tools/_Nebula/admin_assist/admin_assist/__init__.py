import sys
from pathlib import Path

_lib = Path(__file__).resolve().parent.parent / "lib"
_vendor = _lib / f"python{sys.version_info.major}.{sys.version_info.minor}"

if _vendor.is_dir():
    if str(_vendor) not in sys.path:
        sys.path.insert(0, str(_vendor))
elif _lib.is_dir():
    _bundled = ", ".join(sorted(p.name for p in _lib.iterdir() if p.is_dir())) or "none"
    raise SystemExit(
        f"admin_assist: no bundled libraries for Python {sys.version_info.major}.{sys.version_info.minor} "
        f"(bundled: {_bundled}); point admin_assist.sidecar_python at one of those"
    )
