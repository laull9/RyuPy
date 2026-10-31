#!/usr/bin/env python3
# 单独验证 Python 用例，不能用此结果宣称 RyuPy 编译通过。
"""在隔离工作目录执行 CPython 基线；Phase 4 需显式选择并安装扩展依赖。

本脚本是「用例本身成立」的自检：
- `tests/e2e/phaseN/**.py` 为单文件成功用例，期望退出码 0；
- `tests/cases/**/case.txt` 为目录用例，清单里的参数、环境、标准输入与错误路径期望
  必须与 CPython 的实际行为一致（清单写错时这里就会失败）。

格式说明见 `tests/README.md`；Rust 侧解析器在 `crates/ryupy-e2e/src/lib.rs`。
"""

import argparse
import os
from pathlib import Path
import shlex
import subprocess
import sys
import tempfile


ROOT = Path(__file__).resolve().parent
CASES = ROOT / "cases"


def parse_manifest(path):
    """解析 case.txt：行式 `key = value`，`#` 开头为注释。"""
    spec = {
        "entry": None,
        "pending": False,
        "args": [],
        "env": {},
        "stdin": None,
        "cwd": None,
        "expect_exit": 0,
        "expect_exception": None,
        "expect_cause": None,
    }
    for raw in path.read_text(encoding="utf-8").splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        key, _, value = line.partition("=")
        key, value = key.strip(), value.strip()
        if key == "entry":
            spec["entry"] = path.parent / value
        elif key == "pending":
            spec["pending"] = value in {"true", "yes"}
        elif key == "args":
            spec["args"] = shlex.split(value)
        elif key == "env":
            name, _, env_value = value.partition("=")
            spec["env"][name.strip()] = env_value.strip()
        elif key == "stdin":
            # 与 Rust 侧 unescape 对齐：只解释常见转义，未知转义原样保留。
            spec["stdin"] = (
                value.replace("\\n", "\n").replace("\\t", "\t").replace("\\r", "\r")
            )
        elif key == "cwd":
            spec["cwd"] = None if value == "case" else path.parent / value
        elif key == "expect_exit":
            spec["expect_exit"] = value if value == "nonzero" else int(value)
        elif key == "expect_exception":
            spec["expect_exception"] = value
        elif key == "expect_cause":
            spec["expect_cause"] = value
        else:
            raise AssertionError(f"{path} 出现未知键 {key}")
    if spec["entry"] is None:
        spec["entry"] = path.parent / "entry.py"
    return spec


def execute(python, entry, spec):
    """每个进程独享目录，禁止向导入 fixture 写入 pycache。"""
    environment = os.environ.copy()
    environment["PYTHONDONTWRITEBYTECODE"] = "1"
    environment["PYTHON_COLORS"] = "0"
    environment.update(spec.get("env") or {})
    with tempfile.TemporaryDirectory(prefix="ryupy-python-case-") as directory:
        cwd = str(spec.get("cwd") or directory)
        return subprocess.run(
            [python, "-B", str(entry), *(spec.get("args") or [])],
            cwd=cwd,
            env=environment,
            input=spec.get("stdin"),
            capture_output=True,
            text=True,
            encoding="utf-8",
            timeout=30,
            check=False,
        )


def check_single(python, path):
    result = execute(python, path, {})
    if result.returncode != 0:
        raise AssertionError(result.stderr or f"exit={result.returncode}")


def check_case(python, path):
    """目录用例：清单声明的退出码、异常类型与消息、cause 必须与 CPython 一致。"""
    spec = parse_manifest(path)
    result = execute(python, spec["entry"], spec)
    expected_exit = spec["expect_exit"]
    if expected_exit == "nonzero":
        if result.returncode == 0:
            raise AssertionError(f"期望非零退出，实际 0；stdout={result.stdout!r}")
    elif result.returncode != expected_exit:
        raise AssertionError(
            f"exit={result.returncode}，期望 {expected_exit}\n{result.stderr}"
        )
    declaration = spec["expect_exception"]
    if declaration is None:
        if result.returncode != 0:
            raise AssertionError(result.stderr or f"exit={result.returncode}")
        return
    exception, _, message = declaration.partition(":")
    last = result.stderr.strip().splitlines()[-1]
    if not last.startswith(exception.strip() + ":") or message.strip() not in last:
        raise AssertionError(f"最后一行 {last!r} 与清单 {declaration!r} 不符\n{result.stderr}")
    cause = spec["expect_cause"]
    if cause:
        cause_type, _, cause_message = cause.partition(":")
        lines = result.stderr.splitlines()
        if not any(
            cause_type.strip() + ":" in line and cause_message.strip() in line
            for line in lines
        ):
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
                cases.append(("single", path))
    # 目录用例包含错误路径与框架自检，只在选择了 phase2 时纳入。
    if "phase2" in phases:
        for manifest in sorted(CASES.rglob("case.txt")):
            cases.append(("case", manifest))

    failures = 0
    for kind, path in cases:
        try:
            if kind == "single":
                check_single(options.python, path)
            else:
                check_case(options.python, path)
        except (AssertionError, OSError, subprocess.TimeoutExpired, ValueError) as error:
            failures += 1
            print(f"失败 {path.relative_to(ROOT)}：{error}", file=sys.stderr)
    print(f"基线：{len(cases) - failures} 通过，{failures} 失败；不代表 RyuPy 支持。")
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
