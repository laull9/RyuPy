#!/usr/bin/env python3
"""RyuPy 基准脚本（M2.9）。

对比 CPython 与 RyuPy Release 产物的稳态耗时。基准输入固定写在 `tests/benchmarks/*.py`：

- `fib.py`：`fib_rec(30)` + 20 万次 `fib_iter(90)`（静态数值路径）；
- `mandelbrot.py`：Mandelbrot 内部区域 240×120、1000 次上限（静态浮点路径）；
- `dynamic.py`：300 万次动态加法 + 200 万次方法调用（PIC 目标路径）。

脚本先构建 Release CLI 与每个基准的 DLL，再用 `python3` / `dotnet` 各跑 5 次取中位数。
动态基准额外对比 `RYUPY_PIC=0`，用于确认 PIC 的收益；收益不足时应把默认值改回关闭。

用法：
```bash
python3 tests/run_benchmarks.py            # 全部
python3 tests/run_benchmarks.py dynamic    # 只跑某个基准
```

不进入 `make check`：基准耗时随机器波动，提交门禁只跑正确性用例。
"""

import os
from pathlib import Path
import statistics
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parent.parent
BENCHMARKS = ROOT / "tests" / "benchmarks"
RUNS = 5


def target_dir() -> Path:
    """`cargo metadata` 给出的 target 目录（可能被 CARGO_TARGET_DIR 覆盖）。"""
    output = subprocess.run(
        ["cargo", "metadata", "--format-version", "1", "--no-deps"],
        cwd=ROOT,
        check=True,
        capture_output=True,
        text=True,
    ).stdout
    import json

    return Path(json.loads(output)["target_directory"])


def build_cli(target: Path) -> Path:
    print("构建 Release CLI ...", flush=True)
    subprocess.run(
        ["cargo", "build", "--release", "-p", "ryupy-cli"],
        cwd=ROOT,
        check=True,
    )
    return target / "release" / "ryupy"


def build_case(cli: Path, case: Path, target: Path) -> Path:
    out_dir = target / "bench" / case.stem
    subprocess.run(
        [str(cli), "build", str(case), "-o", str(out_dir)],
        cwd=ROOT,
        check=True,
        capture_output=True,
    )
    return out_dir / f"{case.stem}.dll"


def measure(command: list[str], env: dict[str, str]) -> tuple[float, bytes]:
    """跑一次预热 + RUNS 次计时，返回中位数（秒）与最后一次 stdout。"""
    stdout = b""
    subprocess.run(command, cwd=ROOT, env=env, check=True, capture_output=True)
    samples = []
    for _ in range(RUNS):
        start = time.perf_counter()
        result = subprocess.run(command, cwd=ROOT, env=env, check=True, capture_output=True)
        samples.append(time.perf_counter() - start)
        stdout = result.stdout
    return statistics.median(samples), stdout


def main() -> int:
    only = sys.argv[1] if len(sys.argv) > 1 else None
    target = target_dir()
    cli = build_cli(target)
    cases = sorted(BENCHMARKS.glob("*.py"))
    if only:
        cases = [case for case in cases if case.stem == only]
    if not cases:
        print(f"没有匹配的基准：{only}", file=sys.stderr)
        return 2

    print()
    print("| 基准 | CPython (ms) | RyuPy (ms) | 相对 CPython | 输出一致 |")
    print("| --- | ---: | ---: | ---: | --- |")
    for case in cases:
        dll = build_case(cli, case, target)
        python_env = dict(os.environ)
        ryupy_env = dict(os.environ)
        ryupy_env.pop("RYUPY_PIC", None)
        python = os.environ.get("PYTHON", sys.executable)
        cpython_time, expected = measure([python, "-B", str(case)], python_env)
        ryupy_time, actual = measure(["dotnet", str(dll)], ryupy_env)
        ratio = cpython_time / ryupy_time if ryupy_time else 0.0
        same = "是" if expected == actual else "否"
        print(
            f"| {case.stem} | {cpython_time * 1000:.1f} | {ryupy_time * 1000:.1f} "
            f"| {ratio:.2f}x | {same} |"
        )

        if case.stem == "dynamic":
            # 同一 DLL、只改运行期开关，排除编译差异。
            off_env = dict(os.environ)
            off_env["RYUPY_PIC"] = "0"
            off_time, off_output = measure(["dotnet", str(dll)], off_env)
            print(
                f"| dynamic（PIC 关闭） | {cpython_time * 1000:.1f} "
                f"| {off_time * 1000:.1f} | {cpython_time / off_time:.2f}x "
                f"| {'是' if expected == off_output else '否'} |"
            )

    print()
    print("口径：`python3 -B` 与 `dotnet <dll>`，各 1 次预热 + 5 次计时取中位数；")
    print("宿主为 macOS arm64，Release 配置；未包含 `cargo build` 与 JIT 编译时间。")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
