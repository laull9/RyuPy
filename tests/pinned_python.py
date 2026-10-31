"""解析固定的 CPython 版本。

差分测试的「标准答案」必须来自同一个解释器版本，否则基线会随环境漂移。版本写在
`tests/pinned-python.txt`，优先级如下：

1. 环境变量 `PYTHON`（显式指定，最高优先级）；
2. `uv python find <版本>`（开发机通常有 uv，能拿到精确小版本）；
3. `python3.12` / `python3` 中版本号完全一致者；
4. 都没有时回退 `python3` 并打印警告——结果不能与固定版本的基线混用。
"""

import os
from pathlib import Path
import shutil
import subprocess
import sys


PIN = Path(__file__).resolve().parent / "pinned-python.txt"


def pinned_version():
    return PIN.read_text(encoding="utf-8").strip() if PIN.exists() else ""


def _version_of(executable):
    try:
        out = subprocess.run(
            [executable, "-c", "import sys;print('%d.%d.%d' % sys.version_info[:3])"],
            capture_output=True,
            text=True,
            check=True,
        )
    except (OSError, subprocess.SubprocessError):
        return None
    return out.stdout.strip()


def resolve():
    explicit = os.environ.get("PYTHON")
    if explicit:
        return explicit

    version = pinned_version()
    if not version:
        return sys.executable

    uv = shutil.which("uv")
    if uv:
        found = subprocess.run(
            [uv, "python", "find", version], capture_output=True, text=True
        )
        if found.returncode == 0 and found.stdout.strip():
            return found.stdout.strip()

    minor = ".".join(version.split(".")[:2])
    for name in (f"python{minor}", f"python{version}", "python3"):
        candidate = shutil.which(name)
        if not candidate:
            continue
        if _version_of(candidate) == version:
            return candidate
        if name == "python3":
            print(
                f"警告：未找到 CPython {version}，回退 {candidate}（{_version_of(candidate)}）；"
                "基线结果不可与固定版本混用",
                file=sys.stderr,
            )
            return candidate
    return sys.executable
