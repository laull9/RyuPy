#!/usr/bin/env python3
"""RyuPy 基准入口（`make bench`）：CPython 与 RyuPy Release 产物的逐项对照。

基准输入固定写在 `tests/benchmarks/*.py`，每个文件头部用 `# bench:` 注释声明类别、说明、
变体与 pending 状态（格式见 `tests/benchlib/suite.py`）。`--list` 打印完整清单。

每个基准报告：
- 墙钟中位数（含进程启动）与加速比，按类别与总体给出几何平均；
- 进程内执行耗时（RyuPy 的 `RYUPY_STATS` / CPython 外壳），与启动成本分开；
- RyuPy 的托管分配量、各代 GC 次数与暂停、JIT 耗时与方法数，双方的峰值内存与编译耗时；
- stdout 与 CPython 逐字节比对，任何不一致都让脚本以非零码退出。

用法：
```bash
python3 tests/run_benchmarks.py                    # 全部可运行基准
python3 tests/run_benchmarks.py dynamic strings    # 按名字子串或类别过滤
python3 tests/run_benchmarks.py --quick            # 1 次热身 + 3 次计时
python3 tests/run_benchmarks.py --no-cpython       # 只测 RyuPy（仍校验输出）
python3 tests/run_benchmarks.py --save before      # 另存结果，之后 --compare before 对比
python3 tests/run_benchmarks.py --include-pending  # 连 Phase 3 pending 基准一起尝试
python3 tests/run_benchmarks.py --profile sorting  # dotnet-trace 采样，输出热点函数表
```

不进入 `make check`：基准耗时随机器波动，提交门禁只跑正确性用例。
"""

from __future__ import annotations

import argparse
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "tests"))

from benchlib import measure, profile, report, suite  # noqa: E402

BENCHMARKS = ROOT / "tests" / "benchmarks"


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="RyuPy vs CPython 基准")
    parser.add_argument("filters", nargs="*", help="基准名子串或类别名")
    parser.add_argument("--runs", type=int, default=5, help="计时次数（默认 5）")
    parser.add_argument("--warmup", type=int, default=1, help="热身次数（默认 1）")
    parser.add_argument("--quick", action="store_true", help="快速模式：1 次热身 + 3 次计时")
    parser.add_argument("--no-cpython", action="store_true", help="不计时 CPython（仍跑一次校验输出）")
    parser.add_argument("--include-pending", action="store_true", help="也尝试 pending 基准")
    parser.add_argument("--save", metavar="NAME", help="把结果另存为 NAME.json")
    parser.add_argument("--compare", metavar="NAME", help="与已保存的 NAME.json（或路径）对比")
    parser.add_argument("--detail", action="store_true", help="额外输出资源表（分配 / GC / JIT / 内存）")
    parser.add_argument("--list", action="store_true", help="只列出基准清单")
    parser.add_argument("--profile", action="store_true", help="对选中的基准做 dotnet-trace 采样剖析")
    args = parser.parse_args(argv)
    if args.quick:
        args.runs, args.warmup = 3, 1
    return args


def results_path(target: Path, name: str) -> Path:
    path = Path(name)
    if path.suffix == ".json" and path.exists():
        return path
    return target / "bench" / "results" / f"{name}.json"


def run_profiles(cli: Path, target: Path, benches: list[suite.Benchmark]) -> int:
    """只做剖析，不计时：每个基准一张热点表。"""
    for bench in benches:
        if bench.pending:
            continue
        dll, _, error = measure.build_case(ROOT, cli, bench, target)
        if dll is None:
            print(f"{bench.name}：编译失败 {error}", file=sys.stderr)
            return 1
        print(f"\n## {bench.name}\n", flush=True)
        speedscope = profile.collect(dll, target / "bench" / "profile")
        print("\n".join(profile.summarize(speedscope)))
    return 0


def main(argv: list[str]) -> int:
    args = parse_args(argv)
    benches = suite.select(suite.discover(BENCHMARKS), args.filters)
    if not benches:
        print(f"没有匹配的基准：{' '.join(args.filters)}", file=sys.stderr)
        return 2
    if args.list:
        print("\n".join(report.describe(benches)))
        return 0

    python = os.environ.get("PYTHON", sys.executable)
    target = measure.target_dir(ROOT)
    cli = measure.build_cli(ROOT, target)
    if args.profile:
        return run_profiles(cli, target, benches)
    plan = measure.Plan(
        root=ROOT,
        python=python,
        warmup=args.warmup,
        runs=args.runs,
        with_cpython=not args.no_cpython,
        include_pending=args.include_pending,
    )
    # 先跑启动基准（suite 排序保证 startup 在最前），让后续表格能引用启动成本
    results = []
    for index, bench in enumerate(benches, 1):
        print(f"[{index}/{len(benches)}] {bench.name} ...", flush=True, file=sys.stderr)
        results.append(measure.measure_bench(plan, cli, target, bench))

    env = report.environment(ROOT, python, args.runs, args.warmup)
    lines = [""] + report.time_table(results) + [""]
    lines += report.summary(results) + report.startup_line(results)
    lines += report.environment_lines(env)
    if args.detail:
        lines += ["", *report.resource_table(results)]
    if args.compare:
        baseline_file = results_path(target, args.compare)
        if not baseline_file.exists():
            print(f"找不到基线结果：{baseline_file}", file=sys.stderr)
            return 2
        baseline_env, baseline = report.load(baseline_file)
        lines += ["", *report.compare_table(results, baseline_env, baseline)]
    print("\n".join(lines))

    report.save(results_path(target, "latest"), env, results)
    if args.save:
        saved = results_path(target, args.save)
        report.save(saved, env, results)
        print(f"\n结果已保存：{saved}")

    failed = [r for r in results if r.status in ("mismatch", "build-failed", "run-failed")]
    for result in failed:
        print(f"失败：{result.bench.name}：{report.STATUS_TEXT[result.status]} {result.note}", file=sys.stderr)
    ready = [r.bench.name for r in results if r.status == "ready"]
    if ready:
        print(f"[可转正] 以下 pending 基准已能跑通，去掉 `# bench: pending=`：{', '.join(ready)}")
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
