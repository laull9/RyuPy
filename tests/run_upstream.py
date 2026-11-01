#!/usr/bin/env python3
"""固定上游测试移植入口：同一源码分别由固定 CPython 与 RyuPy 编译执行，不计入额外项目分母。"""
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
CASES = [("wcwidth-core", "wcwidth", "0.2.13", "wcwidth_core.py"),
         ("termcolor-force", "termcolor", "2.4.0", "termcolor_force.py"),
         ("texttable-layout", "texttable", "1.7.0", "texttable_layout.py")]


def run(command, cwd, env):
    return subprocess.run(command, cwd=cwd, env=env, text=True, capture_output=True, timeout=120)


def main():
    expected = (ROOT / "tests/pinned-python.txt").read_text().strip()
    actual = ".".join(map(str, sys.version_info[:3]))
    if actual != expected:
        raise SystemExit(f"上游差分须使用 {expected}，当前为 {actual}")
    results = []
    for name, package, version, filename in CASES:
        source = ROOT / "tests/upstream" / filename
        package_root = ROOT / "third_party/projects" / f"{package}-{version}" / f"{package}-{version}"
        if not package_root.is_dir():
            results.append({"name": name, "status": "missing-source", "detail": str(package_root)})
            continue
        import_root = package_root / "src" if (package_root / "src").is_dir() else package_root
        env = dict(os.environ, PYTHONPATH=str(import_root), PYTHONDONTWRITEBYTECODE="1")
        with tempfile.TemporaryDirectory(prefix="ryupy-upstream-") as scratch:
            baseline = run([sys.executable, str(source)], scratch, env)
            if baseline.returncode:
                results.append({"name": name, "status": "baseline-failed", "detail": baseline.stderr})
                continue
            output = Path(scratch) / "compiled"
            build = run(["cargo", "run", "-q", "-p", "ryupy-cli", "--", "build", str(source),
                         "-I", str(import_root), "-o", str(output)], ROOT, env)
            if build.returncode:
                results.append({"name": name, "status": "compile-failed", "detail": build.stderr})
                continue
            compiled = run(["dotnet", str(output / (source.stem + ".dll"))], scratch, env)
            success = compiled.returncode == baseline.returncode and compiled.stdout == baseline.stdout and compiled.stderr == baseline.stderr
            results.append({"name": name, "status": "passed" if success else "failed",
                            "detail": "" if success else f"CPython={baseline.stdout!r}; RyuPy={compiled.stdout!r}; stderr={compiled.stderr!r}"})
    target = ROOT / "target/upstream/results.jsonl"
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text("".join(json.dumps(item, ensure_ascii=False) + "\n" for item in results))
    for item in results:
        print(item["name"], item["status"], item["detail"])
    print(f"结果：{target}；不改变固定 20 项目分母")
    return int(any(item["status"] != "passed" for item in results))


if __name__ == "__main__":
    raise SystemExit(main())
