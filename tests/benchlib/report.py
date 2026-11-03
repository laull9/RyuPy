"""汇总与报告：Markdown 表格、几何平均、环境信息、JSON 保存与历史对比。

# 统计量

- 主数值取**中位数**，对偶发的调度抖动不敏感；
- 波动用 `(max - min) / median` 表示，超过 10% 时提示结果不稳定，应加大 `--runs`；
- 加速比 = CPython 墙钟 / RyuPy 墙钟（>1 表示 RyuPy 更快）；汇总用几何平均，
  `startup` 与 pending 基准不计入。

# 结果文件

每次运行写入 `<target>/bench/results/latest.json`；`--save NAME` 另存一份 `NAME.json`，
`--compare NAME` 读取后逐项对比 RyuPy 的墙钟时间。结果包含机器、工具链、提交号与参数，
不同机器之间的结果不应直接比较。
"""

from __future__ import annotations

import json
import math
import platform
import statistics
import subprocess
import sys
from datetime import datetime
from pathlib import Path

from .measure import Result, Series
from .suite import Benchmark, category_rank

UNSTABLE_SPREAD = 0.10

STATUS_TEXT = {
    "ok": "✓",
    "mismatch": "✗ 输出不一致",
    "build-failed": "✗ 编译失败",
    "run-failed": "✗ 运行失败",
    "pending": "待实现",
    "ready": "可转正",
}


def median(values: list[float]) -> float | None:
    return statistics.median(values) if values else None


def spread(values: list[float]) -> float | None:
    if len(values) < 2:
        return None
    mid = statistics.median(values)
    return (max(values) - min(values)) / mid if mid else None


def fmt_ms(value: float | None) -> str:
    if value is None:
        return "—"
    return f"{value:.1f}" if value < 100 else f"{value:.0f}"


def fmt_mb(value: float | None) -> str:
    return "—" if value is None else f"{value / (1024 * 1024):.1f}"


def speedup(result: Result) -> float | None:
    if result.status not in ("ok", "ready") or not result.cpython or not result.ryupy:
        return None
    base = median(result.cpython.wall_ms)
    ours = median(result.ryupy.wall_ms)
    return base / ours if base and ours else None


def geomean(values: list[float]) -> float | None:
    values = [v for v in values if v and v > 0]
    if not values:
        return None
    return math.exp(sum(math.log(v) for v in values) / len(values))


def _status(result: Result) -> str:
    text = STATUS_TEXT.get(result.status, result.status)
    if result.bench.pending and result.status in ("pending", "ready"):
        text += f"（{result.bench.pending}）"
    if result.ryupy and (s := spread(result.ryupy.wall_ms)) and s > UNSTABLE_SPREAD:
        text += " ⚠波动"
    return text


def time_table(results: list[Result]) -> list[str]:
    lines = [
        "| 基准 | 类别 | CPython ms | RyuPy ms | 加速比 | RyuPy 执行 ms | 波动 | 状态 |",
        "| --- | --- | ---: | ---: | ---: | ---: | ---: | --- |",
    ]
    for result in results:
        bench = result.bench
        cpy = median(result.cpython.wall_ms) if result.cpython else None
        ours = median(result.ryupy.wall_ms) if result.ryupy else None
        main = median(result.ryupy.main_ms) if result.ryupy else None
        ratio = speedup(result)
        swing = spread(result.ryupy.wall_ms) if result.ryupy else None
        lines.append(
            f"| {bench.name} | {bench.category} | {fmt_ms(cpy)} | {fmt_ms(ours)} "
            f"| {'—' if ratio is None else f'{ratio:.2f}x'} | {fmt_ms(main)} "
            f"| {'—' if swing is None else f'{swing * 100:.0f}%'} | {_status(result)} |"
        )
        for label, series in result.variants.items():
            variant = median(series.wall_ms)
            ratio = cpy / variant if result.status in ("ok", "ready") and cpy and variant else None
            lines.append(
                f"| ↳ {label} | | {fmt_ms(cpy)} | {fmt_ms(variant)} "
                f"| {'—' if ratio is None else f'{ratio:.2f}x'} | {fmt_ms(median(series.main_ms))} | | |"
            )
    return lines


def resource_table(results: list[Result]) -> list[str]:
    lines = [
        "| 基准 | RyuPy 分配 MiB | GC 0/1/2 | GC 暂停 ms | JIT ms（方法数） | 峰值内存 MiB CPython / RyuPy | 编译 ms |",
        "| --- | ---: | ---: | ---: | ---: | ---: | ---: |",
    ]
    for result in results:
        if not result.ryupy:
            continue
        stats = result.ryupy.stats
        gcs = "/".join(str(int(stats.get(k, 0))) for k in ("gc0", "gc1", "gc2"))
        jit = "N/A（NativeAOT）" if result.backend == "aot" else f"{stats.get('jit_ms', 0):.0f}（{int(stats.get('jit_methods', 0))}）"
        cpy_rss = median(result.cpython.maxrss) if result.cpython else None
        lines.append(
            f"| {result.bench.name} | {fmt_mb(stats.get('alloc_bytes'))} | {gcs} "
            f"| {stats.get('gc_pause_ms', 0):.1f} | {jit} "
            f"| {fmt_mb(cpy_rss)} / {fmt_mb(median(result.ryupy.maxrss))} | {fmt_ms(result.compile_ms)} |"
        )
    return lines


def summary(results: list[Result]) -> list[str]:
    """几何平均：总体与按类别。"""
    counted = [r for r in results if not r.bench.is_startup and not r.bench.pending]
    by_category: dict[str, list[float]] = {}
    for result in counted:
        if (ratio := speedup(result)) is not None:
            by_category.setdefault(result.bench.category, []).append(ratio)
    overall = geomean([v for values in by_category.values() for v in values])
    if overall is None:
        return []
    parts = [
        f"{category} {geomean(values):.2f}x"
        for category, values in sorted(by_category.items(), key=lambda kv: category_rank(kv[0]))
    ]
    return [f"几何平均加速比（CPython / RyuPy，>1 更快）：**{overall:.2f}x**；按类别：" + "，".join(parts)]


def startup_line(results: list[Result]) -> list[str]:
    for result in results:
        if result.bench.is_startup and result.cpython and result.ryupy:
            return [
                f"启动成本（{result.bench.name}，墙钟中位数）：CPython {fmt_ms(median(result.cpython.wall_ms))} ms，"
                f"RyuPy {fmt_ms(median(result.ryupy.wall_ms))} ms；「执行 ms」从MarkMainStart至统计点，含模块执行/JIT/GC，不含此前host/注册，不能直接减去hello墙钟。"
            ]
    return []


def _command_output(command: list[str]) -> str:
    try:
        return subprocess.run(command, capture_output=True, text=True, check=True).stdout.strip()
    except (OSError, subprocess.CalledProcessError):
        return "unknown"


def environment(root: Path, python: str, runs: int, warmup: int, backend: str = "jit") -> dict:
    cpu = platform.processor() or platform.machine()
    if sys.platform == "darwin":
        cpu = _command_output(["sysctl", "-n", "machdep.cpu.brand_string"])
    dirty = _command_output(["git", "-C", str(root), "status", "--porcelain"])
    return {
        "date": datetime.now().isoformat(timespec="seconds"),
        "platform": platform.platform(),
        "cpu": cpu,
        "python": _command_output([python, "-c", "import sys; print(sys.version.split()[0])"]),
        "dotnet": _command_output(["dotnet", "--version"]),
        "commit": _command_output(["git", "-C", str(root), "rev-parse", "--short", "HEAD"])
        + ("+dirty" if dirty and dirty != "unknown" else ""),
        "runs": runs,
        "warmup": warmup,
        "backend": backend,
        "jit_metrics_applicable": backend == "jit",
        "resource_aggregation": "last_timed_process",
    }


def environment_lines(env: dict) -> list[str]:
    return [
        f"口径：{env['cpu']}，{env['platform']}；CPython {env['python']}，.NET {env['dotnet']}，"
        f"后端 {env.get('backend', 'jit')}；提交 {env['commit']}；{env['warmup']} 次热身 + {env['runs']} 次计时取中位数（各次为新进程，墙钟含启动）。",
        "执行ms为MarkMainStart至统计点，含模块执行/JIT/GC、不含此前host/注册；资源分配/GC/JIT取最后计时进程，RSS取各进程峰值中位数；MiB=2^20字节。",
        "编译ms是整次build耗时，NativeAOT包含ILC；AOT原stats保留，但JIT指标不适用。"
    ]


def save(path: Path, env: dict, results: list[Result]) -> None:
    payload = {"environment": env, "results": []}
    for result in results:
        payload["results"].append(
            {
                "name": result.bench.name,
                "category": result.bench.category,
                "status": result.status,
                "backend": result.backend,
                "compile_ms": result.compile_ms,
                "cpython": result.cpython.to_json() if result.cpython else None,
                "ryupy": result.ryupy.to_json() if result.ryupy else None,
                "variants": {k: v.to_json() for k, v in result.variants.items()},
            }
        )
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=1), encoding="utf-8")


def load(path: Path) -> tuple[dict, dict[str, dict]]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    return payload["environment"], {item["name"]: item for item in payload["results"]}


def validate_backend(backend: str, baseline_env: dict, baseline: dict[str, dict]) -> None:
    """旧结果无backend字段按历史JIT解释；不同后端禁止产生性能比较。"""
    old = baseline_env.get("backend", "jit")
    if old not in ("jit", "aot") or old != backend:
        raise ValueError(f"不能比较不同backend：基线{old}，当前{backend}")
    if any(item.get("backend", old) != old for item in baseline.values()):
        raise ValueError("基线结果backend与environment不一致")


def compare_table(results: list[Result], baseline_env: dict, baseline: dict[str, dict]) -> list[str]:
    """与历史结果对比 RyuPy 墙钟与分配量；变化超过 ±5% 才标记方向。"""
    for result in results:
        validate_backend(result.backend, baseline_env, baseline)
    lines = [
        f"对比基线（{baseline_env.get('backend', 'jit')}，旧缺字段按JIT）：提交 {baseline_env.get('commit')}（{baseline_env.get('date')}）",
        "",
        "| 基准 | 基线 RyuPy ms | 当前 RyuPy ms | 变化 | 基线分配 MiB | 当前分配 MiB |",
        "| --- | ---: | ---: | ---: | ---: | ---: |",
    ]
    ratios = []
    for result in results:
        old = baseline.get(result.bench.name)
        if result.status not in ("ok", "ready") or not old or old.get("status") not in ("ok", "ready") or not result.ryupy or not old.get("ryupy"):
            continue
        old_series = Series.from_json(old["ryupy"])
        before = median(old_series.wall_ms)
        after = median(result.ryupy.wall_ms)
        change = after / before - 1 if before else 0.0
        if not result.bench.is_startup:
            ratios.append(before / after)
        mark = "🟢" if change < -0.05 else ("🔴" if change > 0.05 else "")
        lines.append(
            f"| {result.bench.name} | {fmt_ms(before)} | {fmt_ms(after)} | {change * 100:+.1f}% {mark} "
            f"| {fmt_mb(old_series.stats.get('alloc_bytes'))} | {fmt_mb(result.ryupy.stats.get('alloc_bytes'))} |"
        )
    if (mean := geomean(ratios)) is not None:
        lines += ["", f"相对基线的几何平均加速：**{mean:.2f}x**"]
    return lines


def describe(benches: list[Benchmark]) -> list[str]:
    lines = ["| 基准 | 类别 | 说明 | 变体 | 状态 |", "| --- | --- | --- | --- | --- |"]
    for bench in benches:
        variants = "、".join(v.label for v in bench.variants) or "—"
        state = f"pending（{bench.pending}）" if bench.pending else "可运行"
        lines.append(f"| {bench.name} | {bench.category} | {bench.desc} | {variants} | {state} |")
    return lines
