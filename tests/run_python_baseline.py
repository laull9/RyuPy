#!/usr/bin/env python3
# 单独验证 Python 用例，不能用此结果宣称 RyuPy 编译通过。
"""在隔离工作目录执行 CPython 基线；Phase 4 需显式选择并安装扩展依赖。"""

import argparse
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile


ROOT = Path(__file__).resolve().parent


def execute(python, path):
    """每个进程独享目录，禁止向导入 fixture 写入 pycache。"""
    environment = os.environ.copy()
    environment["PYTHONDONTWRITEBYTECODE"] = "1"
    environment["PYTHON_COLORS"] = "0"
    with tempfile.TemporaryDirectory(prefix="ryupy-python-case-") as directory:
        return subprocess.run(
            [python, "-B", str(path)],
            cwd=directory,
            env=environment,
            capture_output=True,
            text=True,
            encoding="utf-8",
            timeout=30,
            check=False,
        )


def check_case(python, path, expected=None):
    """成功用例检查退出状态，负例另检查异常类型、消息、cause 与 stdout。"""
    result = execute(python, path)
    if expected is None:
        if result.returncode != 0:
            raise AssertionError(result.stderr or f"exit={result.returncode}")
        return
    if result.returncode != 1 or result.stdout != expected["stdout"]:
        raise AssertionError(f"exit={result.returncode}, stdout={result.stdout!r}")
    last = result.stderr.strip().splitlines()[-1]
    if not last.startswith(expected["exception"] + ":") or expected["message"] not in last:
        raise AssertionError(result.stderr)
    if "cause" in expected and expected["cause"] not in result.stderr:
        raise AssertionError("异常链丢失：\n" + result.stderr)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--python", default=os.environ.get("PYTHON", sys.executable))
    parser.add_argument("--phase", action="append", choices=["phase1", "phase2", "phase3", "phase4"])
    options = parser.parse_args()
    phases = options.phase or ["phase1", "phase2", "phase3"]
    version = subprocess.run(
        [options.python, "--version"], capture_output=True, text=True, check=True
    ).stdout.strip()
    print(f"CPython 基线：{version}；阶段：{', '.join(phases)}")
    cases = []
    for phase in dict.fromkeys(phases):
        for path in sorted((ROOT / "e2e" / phase).rglob("*.py")):
            if "fixtures" not in path.relative_to(ROOT / "e2e" / phase).parts:
                cases.append((path, None))
    if "phase2" in phases:
        directory = ROOT / "fixtures" / "errors"
        for expected in json.loads((directory / "cases.json").read_text(encoding="utf-8")):
            cases.append((directory / expected["file"], expected))
    failures = 0
    for path, expected in cases:
        try:
            check_case(options.python, path, expected)
        except (AssertionError, OSError, subprocess.TimeoutExpired) as error:
            failures += 1
            print(f"失败 {path.relative_to(ROOT)}：{error}", file=sys.stderr)
    print(f"基线：{len(cases) - failures} 通过，{failures} 失败；不代表 RyuPy 支持。")
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
