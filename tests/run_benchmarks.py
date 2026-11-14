#!/usr/bin/env python3
"""RyuPy 基准入口（`make bench` / `make bench-aot`）：CPython 与 RyuPy Release 产物的逐项对照。

基准输入固定写在 `tests/benchmarks/*.py`，每个文件头部用 `# bench:` 注释声明类别、说明、
变体与 pending 状态（格式见 `tests/benchlib/suite.py`）。`--list` 打印完整清单。

每个基准报告：
- 墙钟中位数（含进程启动）与加速比，按类别与总体给出几何平均；主表为纯净无外壳直接脚本 vs 原生二进制；
- 独立 instrumented 轮测量进程内执行耗时（MarkMainStart 至统计点），与启动成本分开；
- RyuPy 托管分配量、各代 GC 次数与暂停、JIT 耗时与方法数（AOT 路径为 N/A），双方峰值内存与编译耗时；
- stdout 与 CPython 逐字节比对，任何不一致都让脚本以非零码退出。

依 ADR-0182，基准编译产物与中间目录放仓库外独立独占临时目录并在退出时仅清理本轮根，不污染 target/bench，不影响他人。

用法：
```bash
python3 tests/run_benchmarks.py                    # JIT 全部可运行基准
python3 tests/run_benchmarks.py --aot              # NativeAOT 全部可运行基准
python3 tests/run_benchmarks.py --aot hello        # AOT smoke 验证单个基准
python3 tests/run_benchmarks.py --quick            # 1 次热身 + 3 次计时
python3 tests/run_benchmarks.py --no-cpython       # 只测 RyuPy（仍严格校验输出）
python3 tests/run_benchmarks.py --include-pending  # 连 pending 基准一起尝试
python3 tests/run_benchmarks.py --cli /path/ryupy  # 显式指定冻结的 CLI 路径
```
"""

from __future__ import annotations

import argparse
import os
import sys
import tempfile
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
    parser.add_argument("--save", metavar="NAME", help="把结果显式保存至外部 NAME.json")
    parser.add_argument("--compare", metavar="NAME", help="与已保存的外部 NAME.json 对比")
    parser.add_argument("--detail", action="store_true", help="额外输出资源表（分配 / GC / JIT / 内存）")
    parser.add_argument("--list", action="store_true", help="只列出基准清单")
    parser.add_argument("--aot", action="store_true", help="NativeAOT：每例 ILC 构建后直接运行原生程序（POSIX）")
    parser.add_argument("--native", action=argparse.BooleanOptionalAction, default=True, help="强制纯 RyuPy 原生执行域（默认开启）")
    parser.add_argument("--cli", metavar="PATH", help="指定预编译的 CLI 二进制路径（跳过 cargo build）")
    parser.add_argument("--profile", action="store_true", help="对选中的基准做 dotnet-trace 采样剖析")
    args = parser.parse_args(argv)
    if args.aot and args.profile:
        parser.error("--aot 与 --profile 不兼容；当前 profile 只支持 JIT")
    if args.runs < 1 or args.warmup < 0:
        parser.error("runs 必须为正数，warmup 不能为负")
    if args.quick:
        args.runs, args.warmup = 3, 1
    return args


def external_results_path(name: str) -> Path:
    """获取仓库外的持久化基准结果路径，依 ADR-0182 严禁在仓库内落测试结果。"""
    path = Path(name)
    if path.is_absolute() or path.parent != Path("."):
        candidate = path.resolve()
    else:
        external_base = measure.get_safe_external_dir(ROOT)
        candidate = (external_base / "ryupy-bench-results" / path.with_suffix(".json")).resolve()
    if candidate.is_relative_to(ROOT.resolve()):
        raise ValueError("基准结果必须位于仓库外")
    return candidate


def run_profiles(cli: Path, work_root: Path, benches: list[suite.Benchmark]) -> int:
    """只做剖析，不计时：每个基准一张热点表。"""
    for bench in benches:
        if bench.pending:
            continue
        dll, _, error, status = measure.build_case(ROOT, cli, bench, work_root)
        if dll is None:
            print(f"{bench.name}：编译失败 [{status}] {error}", file=sys.stderr)
            return 1
        print(f"\n## {bench.name}\n", flush=True)
        profile_out = work_root / "bench" / "profile"
        speedscope = profile.collect(dll, profile_out)
        print("\n".join(profile.summarize(speedscope)))
    return 0


def main(argv: list[str]) -> int:
    args = parse_args(argv)
    all_benches = suite.discover(BENCHMARKS)
    benches = suite.select(all_benches, args.filters)
    if not benches:
        print(f"没有匹配的基准：{' '.join(args.filters)}", file=sys.stderr)
        return 2
    if args.list:
        print("\n".join(report.describe(benches)))
        return 0

    if args.aot:
        from benchlib.artifact import host_rid

        try:
            host_rid()
        except ValueError as error:
            print(str(error), file=sys.stderr)
            return 2

    python = os.environ.get("PYTHON", sys.executable)
    target = measure.target_dir(ROOT)
    backend = "aot" if args.aot else "jit"
    cli_path = Path(args.cli) if args.cli else None

    baseline_env, baseline = None, None
    if args.compare:
        baseline_file = external_results_path(args.compare)
        if not baseline_file.is_file():
            print(f"基线文件不存在：{baseline_file}", file=sys.stderr)
            return 2
        try:
            baseline_env, baseline = report.load(baseline_file)
            report.validate_comparison(backend, args.native, baseline_env, baseline)
            print(f"载入基线对比文件：{baseline_file}", file=sys.stderr)
        except Exception as error:
            print(f"基线不可比较：{error}", file=sys.stderr)
            return 2

    if args.save:
        saved_file = external_results_path(args.save)
        if saved_file.exists():
            try:
                saved_env, saved_results = report.load(saved_file)
                if not isinstance(saved_env, dict):
                    raise ValueError("保存目标 environment 不是有效对象")
                report.validate_comparison(backend, args.native, saved_env, saved_results)
            except Exception as error:
                print(f"保存目标不可覆盖：{error}", file=sys.stderr)
                return 2

    # 先验证比较/保存合同，再允许构建；失败不能产生无关构建和运行。
    cli = measure.build_cli(ROOT, target, cli_override=cli_path)
    # 创建本轮独占且完全位于仓库外的临时工作根，退出时仅清理本轮独占根
    external_base = measure.get_safe_external_dir(ROOT)
    with tempfile.TemporaryDirectory(prefix=f"ryupy-bench-run-{os.getpid()}-", dir=external_base) as temp_dir:
        work_root = Path(temp_dir).resolve()
        if args.profile:
            return run_profiles(cli, work_root, benches)

        plan = measure.Plan(
            root=ROOT,
            python=python,
            warmup=args.warmup,
            runs=args.runs,
            with_cpython=not args.no_cpython,
            include_pending=args.include_pending,
            work_root=work_root,
            backend=backend,
            native=args.native,
        )

        fatal_statuses = {"build-failed", "artifact-failed", "run-failed", "mismatch", "cpython-failed"}

        # 基础设施 smoke 预检（针对 AOT 模式）：
        # 若基准列表不含 hello，先极轻预检 hello，若失败立即阻断；若包含 hello 则由其在正式运行首位承接
        contains_hello = any(b.name == "hello" for b in benches)
        if args.aot and not contains_hello:
            hello_bench = next((b for b in all_benches if b.name == "hello"), None)
            if hello_bench:
                print("[预检] 正在对 AOT 基础设施进行 hello smoke 自检 ...", flush=True, file=sys.stderr)
                smoke_plan = measure.Plan(
                    root=ROOT,
                    python=python,
                    warmup=1,
                    runs=1,
                    with_cpython=False,
                    include_pending=False,
                    work_root=work_root,
                    backend="aot",
                    native=args.native,
                )
                smoke_res = measure.measure_bench(smoke_plan, cli, target, hello_bench)
                if smoke_res.status in fatal_statuses:
                    print(
                        f"\n[阻断] hello AOT 基础设施自检失败（{smoke_res.status}），终止后续测试：\n{smoke_res.note}",
                        file=sys.stderr,
                    )
                    return 1

        results = []
        for index, bench in enumerate(benches, 1):
            print(f"[{index}/{len(benches)}] {bench.name} ...", flush=True, file=sys.stderr)
            res = measure.measure_bench(plan, cli, target, bench)
            results.append(res)
            # 若首个基准 hello 遭遇致命失败，立即阻断，避免重复失败浪费资源
            if args.aot and bench.name == "hello" and res.status in fatal_statuses and len(benches) > 1:
                print(
                    f"\n[阻断] hello AOT 自检致命失败（{res.status}），终止后续基准以避免重复错误：\n{res.note}",
                    file=sys.stderr,
                )
                break

    env = report.environment(ROOT, python, args.runs, args.warmup, backend, native=args.native)
    env["cli_path"] = str(cli.resolve())
    env["cli_origin"] = "explicit-frozen" if args.cli else "current-source-cargo"
    lines = [""] + report.time_table(results) + [""]
    lines += report.summary(results) + report.startup_line(results)
    lines += report.environment_lines(env)
    if args.detail:
        lines += ["", *report.resource_table(results)]
    if args.compare:
        lines += ["", *report.compare_table(results, baseline_env, baseline)]
    print("\n".join(lines))

    # 仅当显式指定 --save 时才持久化保存结果至仓库外部，默认不永久遗留机器产物（ADR-0182）
    if args.save:
        saved = external_results_path(args.save)
        report.save(saved, env, results)
        print(f"\n结果已显式保存至：{saved}")

    failed = [r for r in results if r.status in ("mismatch", "build-failed", "artifact-failed", "run-failed", "cpython-failed", "variant-failed")]
    for result in failed:
        print(f"失败：{result.bench.name}：{report.STATUS_TEXT[result.status]} {result.note}", file=sys.stderr)
    ready = [r.bench.name for r in results if r.status == "ready"]
    if ready:
        print(f"[可转正] 以下 pending 基准已能跑通，去掉 `# bench: pending=`：{', '.join(ready)}")
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
