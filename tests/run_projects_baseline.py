#!/usr/bin/env python3
"""记录固定项目集的 CPython 基线（M2.0）。

对每个项目运行 `entries/<id>.py`，把 stdout、退出码与最后一行 stderr 存到
`tests/projects/baselines/<id>/`，供 RyuPy 项目 runner 对照。基线是「答案」，
不是 RyuPy 的能力声明；这里失败说明入口样例本身需要修，或环境缺依赖。

前置：先运行 `python3 tests/fetch_projects.py` 抓取源码。
"""

import argparse
import datetime
import os
from pathlib import Path
import subprocess
import sys
import tempfile

from pinned_python import resolve as resolve_python


ROOT = Path(__file__).resolve().parent.parent
PROJECTS = ROOT / "tests" / "projects"
STORE = ROOT / "third_party" / "projects"


def read_manifest(projects=PROJECTS):
    rows = []
    manifest = projects / "manifest.tsv"
    for raw in manifest.read_text(encoding="utf-8-sig").splitlines():
        line = raw.rstrip("\n")
        if not line.strip() or line.startswith("#") or line.startswith("id\t"):
            continue
        pid, category, pypi, version, import_name, entry, scope, deps = line.split("\t")[:8]
        rows.append(
            {
                "id": pid,
                "category": category,
                "pypi": pypi,
                "version": version,
                "import": import_name,
                "entry": entry,
                "scope": scope,
                "deps": deps,
            }
        )
    return rows


def package_root(dest):
    """sdist 解开后通常含一层顶层目录；`src/` 布局则再进一层。

    返回值要能直接用于 `PYTHONPATH`，也就是「包目录的父目录」。
    """
    children = list(dest.iterdir())
    dirs = [c for c in children if c.is_dir()]
    files = [c for c in children if c.is_file()]
    root = dirs[0] if len(dirs) == 1 and not files else dest
    if (root / "src").is_dir():
        return root / "src"
    return root


def source_dirs(rows, store=STORE):
    """所有已抓取的源码目录（含传递依赖），用作 PYTHONPATH。"""
    dirs = []
    for row in rows:
        candidates = [f"{row['pypi']}-{row['version']}"]
        if row["deps"] != "-":
            for dep in row["deps"].split(","):
                name, _, version = dep.strip().partition("==")
                candidates.append(f"{name}-{version}")
        for name in candidates:
            path = store / name
            if not path.is_dir():
                raise SystemExit(
                    f"缺少源码 {path}，先运行 python3 tests/fetch_projects.py"
                )
            dirs.append(str(package_root(path)))
    return list(dict.fromkeys(dirs))


def run(python, entry, python_path):
    environment = os.environ.copy()
    environment["PYTHONPATH"] = os.pathsep.join(python_path)
    environment["PYTHONDONTWRITEBYTECODE"] = "1"
    environment["PYTHON_COLORS"] = "0"
    environment["NO_COLOR"] = "1"
    with tempfile.TemporaryDirectory(prefix="ryupy-project-") as directory:
        return subprocess.run(
            [python, "-B", str(entry)],
            cwd=directory,
            env=environment,
            capture_output=True,
            text=True,
            encoding="utf-8",
            timeout=60,
            check=False,
        )


def write_baseline(project, result, python_version):
    target = PROJECTS / "baselines" / project["id"]
    target.mkdir(parents=True, exist_ok=True)
    (target / "stdout.txt").write_text(result.stdout, encoding="utf-8")
    stderr_lines = [line for line in result.stderr.splitlines() if line.strip()]
    last = stderr_lines[-1] if stderr_lines else ""
    meta = {
        "id": project["id"],
        "pypi": project["pypi"],
        "version": project["version"],
        "python": python_version,
        "recorded": datetime.date.today().isoformat(),
        "exit": str(result.returncode),
        "stderr_last": last,
    }
    (target / "meta.txt").write_text(
        "".join(f"{k}={v}\n" for k, v in meta.items()), encoding="utf-8"
    )


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--python", default=resolve_python())
    parser.add_argument("--only", help="只跑指定 id（逗号分隔）")
    parser.add_argument("--check", action="store_true", help="只校验已有基线，不重写")
    options = parser.parse_args()

    rows = read_manifest()
    if options.only:
        wanted = set(options.only.split(","))
        rows = [row for row in rows if row["id"] in wanted]
    python_path = source_dirs(read_manifest())
    version = subprocess.run(
        [options.python, "--version"], capture_output=True, text=True, check=True
    ).stdout.strip()
    print(f"CPython 基线：{version}；项目数 {len(rows)}")

    failures = 0
    for row in rows:
        entry = PROJECTS / row["entry"]
        if not entry.exists():
            failures += 1
            print(f"失败 {row['id']}：入口样例缺失 {entry}", file=sys.stderr)
            continue
        result = run(options.python, entry, python_path)
        status = "ok" if result.returncode == 0 else f"exit={result.returncode}"
        if options.check:
            target = PROJECTS / "baselines" / row["id"] / "stdout.txt"
            expected = target.read_text(encoding="utf-8") if target.exists() else None
            if expected != result.stdout or result.returncode != 0:
                failures += 1
                print(f"失败 {row['id']}：基线已过期（{status}）", file=sys.stderr)
            continue
        write_baseline(row, result, version)
        if result.returncode != 0:
            failures += 1
            last = result.stderr.strip().splitlines()[-1] if result.stderr.strip() else ""
            print(f"失败 {row['id']}：{status} {last}", file=sys.stderr)
        else:
            print(f"记录 {row['id']}（{row['pypi']}=={row['version']}）：{len(result.stdout)} 字节 stdout")

    print(f"基线：{len(rows) - failures} 通过，{failures} 失败；不代表 RyuPy 支持。")
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
