"""构建与测量：Release CLI → 每个基准的 DLL → 交替启动 CPython / RyuPy 子进程计时。

# 测量口径

- **墙钟（wall）**：`Popen` 到 `os.wait4` 返回的耗时，包含解释器 / .NET 宿主启动、JIT 与执行；
- **执行（main）**：进程内统计，从MarkMainStart计至统计点，含模块执行/JIT/GC，不含此前host/注册。RyuPy 由 `RYUPY_STATS` 写出
  （`runtime/RyuPy.Runtime/PyStats.cs`），CPython 由 `cpython_harness.py` 写出；
  不是墙钟减去hello，也不声称纯算法时间；
- **峰值内存**：`wait4` 返回的子进程 `ru_maxrss`（macOS 单位是字节，Linux 是 KiB，此处统一为字节）；
- **分配 / GC / JIT**：仅 RyuPy 有精确的托管分配量与 JIT 耗时，CPython 只记录 GC 次数。

每轮按「CPython → RyuPy → 各变体」交替执行，热身轮不计入，减少机器状态漂移对单侧的偏置。
stdout 写入临时文件而不是管道：这样可以用 `wait4` 拿到单个子进程的 rusage。
"""

from __future__ import annotations

import json
import os
import subprocess
import sys
import tempfile
import time
from dataclasses import dataclass, field
from pathlib import Path

from .suite import Benchmark

HARNESS = Path(__file__).resolve().parent / "cpython_harness.py"


@dataclass
class Sample:
    wall_ms: float
    maxrss: int
    stats: dict
    stdout: bytes
    stderr: bytes
    exit_code: int


@dataclass
class Series:
    """同一实现在多次计时中的样本集合。"""

    wall_ms: list[float] = field(default_factory=list)
    main_ms: list[float] = field(default_factory=list)
    maxrss: list[int] = field(default_factory=list)
    # 最后一次运行的完整统计（分配量等在多次运行间几乎不变，只保留一份）
    stats: dict = field(default_factory=dict)

    def add(self, sample: Sample) -> None:
        self.wall_ms.append(sample.wall_ms)
        if "main_ms" in sample.stats:
            self.main_ms.append(float(sample.stats["main_ms"]))
        self.maxrss.append(sample.maxrss)
        self.stats = sample.stats

    def to_json(self) -> dict:
        return {
            "wall_ms": self.wall_ms,
            "main_ms": self.main_ms,
            "maxrss": self.maxrss,
            "stats": self.stats,
        }

    @staticmethod
    def from_json(data: dict) -> "Series":
        return Series(data["wall_ms"], data["main_ms"], data["maxrss"], data["stats"])


@dataclass
class Result:
    bench: Benchmark
    cpython: Series | None = None
    ryupy: Series | None = None
    variants: dict[str, Series] = field(default_factory=dict)
    compile_ms: float | None = None
    # "ok" / "mismatch" / "build-failed" / "run-failed" / "pending" / "ready"
    status: str = "ok"
    note: str = ""
    backend: str = "jit"


def target_dir(root: Path) -> Path:
    """`cargo metadata` 给出的 target 目录（可能被 CARGO_TARGET_DIR 覆盖）。"""
    output = subprocess.run(
        ["cargo", "metadata", "--format-version", "1", "--no-deps"],
        cwd=root,
        check=True,
        capture_output=True,
        text=True,
    ).stdout
    return Path(json.loads(output)["target_directory"])


def build_cli(root: Path, target: Path) -> Path:
    print("构建 Release CLI ...", flush=True)
    subprocess.run(["cargo", "build", "--release", "-q", "-p", "ryupy-cli"], cwd=root, check=True)
    cli = target / "release" / "ryupy"
    # 先空跑一次：首个调用的冷缓存（动态库加载、磁盘页）不应算进第一个基准的编译耗时
    subprocess.run([str(cli), "--help"], cwd=root, capture_output=True)
    return cli


def build_case(root: Path, cli: Path, bench: Benchmark, target: Path, *, backend: str = "jit") -> tuple[Path | None, float, str]:
    """返回本次DLL或已核原生入口；耗时为整次build（AOT含ILC），不计入进程采样。"""
    if backend not in ("jit", "aot"):
        raise ValueError("未知基准backend")
    out_dir = target / "bench" / bench.name
    if backend == "aot":
        # 每次使用全新自有根；构建失败绝不执行上次遗留dist。
        parent = target / "bench" / "aot"
        parent.mkdir(parents=True, exist_ok=True)
        out_dir = Path(tempfile.mkdtemp(prefix=bench.name + "-", dir=parent))
    command = [str(cli), "build", str(bench.path), "-o", str(out_dir)]
    if backend == "aot":
        command.append("--aot")
    start = time.perf_counter()
    result = subprocess.run(
        command,
        cwd=root,
        capture_output=True,
        text=True,
    )
    elapsed = (time.perf_counter() - start) * 1000
    if result.returncode != 0:
        first = next((line for line in result.stderr.splitlines() if line.strip()), "")
        return None, elapsed, first.strip()
    if backend == "aot":
        from .artifact import verified_native
        try:
            return verified_native(out_dir, bench, result.stderr), elapsed, ""
        except (OSError, ValueError, KeyError, TypeError) as error:
            return None, elapsed, "AOT发布验证失败：" + str(error)
    dll = out_dir / f"{bench.name}.dll"
    if not dll.is_file():
        return None, elapsed, "成功构建未产生DLL"
    return dll, elapsed, ""


def _normalize_rss(raw: int) -> int:
    # macOS 的 ru_maxrss 单位是字节，Linux 是 KiB
    return raw if sys.platform == "darwin" else raw * 1024


def run_once(command: list[str], env: dict[str, str], cwd: Path, stats_env: str | None) -> Sample:
    """启动一次子进程并计时。

    `stats_env` 是统计文件路径的环境变量名（RyuPy：`RYUPY_STATS`）；为 None 表示 CPython
    外壳，统计文件路径作为参数插在脚本路径之前。
    """
    with tempfile.TemporaryDirectory() as tmp:
        stats_path = Path(tmp) / "stats.json"
        run_env = dict(env)
        cmd = list(command)
        if stats_env:
            run_env[stats_env] = str(stats_path)
        else:
            # CPython 外壳：python -B harness <stats> <script>
            cmd.insert(-1, str(stats_path))
        with open(Path(tmp) / "out", "w+b") as out, open(Path(tmp) / "err", "w+b") as err:
            start = time.perf_counter()
            proc = subprocess.Popen(cmd, cwd=cwd, env=run_env, stdout=out, stderr=err)
            _, status, usage = os.wait4(proc.pid, 0)
            elapsed = (time.perf_counter() - start) * 1000
            # 告诉 Popen 进程已回收，避免析构时再次 wait
            proc.returncode = os.waitstatus_to_exitcode(status)
            out.seek(0)
            err.seek(0)
            stdout, stderr = out.read(), err.read()
        stats = {}
        if stats_path.exists():
            try:
                stats = json.loads(stats_path.read_text(encoding="utf-8"))
            except json.JSONDecodeError:
                stats = {}
    return Sample(elapsed, _normalize_rss(usage.ru_maxrss), stats, stdout, stderr, proc.returncode)


@dataclass
class Plan:
    """一次 `make bench` 的测量参数。"""

    root: Path
    python: str
    warmup: int
    runs: int
    with_cpython: bool
    include_pending: bool
    backend: str = "jit"


def _base_env() -> dict[str, str]:
    env = dict(os.environ)
    # 用户 shell 里遗留的开关会让结果不可比；变体会显式重新设置
    for key in ("RYUPY_PIC", "RYUPY_STATS", "PYTHONHASHSEED"):
        env.pop(key, None)
    env["PYTHONHASHSEED"] = "0"
    return env


def measure_bench(plan: Plan, cli: Path, target: Path, bench: Benchmark) -> Result:
    result = Result(bench, backend=plan.backend)
    if bench.pending and not plan.include_pending:
        result.status = "pending"
        return result

    env = _base_env()
    cpython_cmd = [plan.python, "-B", str(HARNESS), str(bench.path)]
    dll, compile_ms, error = build_case(plan.root, cli, bench, target, backend=plan.backend)
    result.compile_ms = compile_ms
    if dll is None:
        result.status = "pending" if bench.pending else "build-failed"
        result.note = error
    ryupy_cmd = ([str(dll)] if plan.backend == "aot" else ["dotnet", str(dll)]) if dll else None
    variant_envs = {v.label: {**env, **v.env} for v in bench.variants}

    if plan.with_cpython or bench.pending:
        result.cpython = Series()
    if ryupy_cmd:
        result.ryupy = Series()
        result.variants = {label: Series() for label in variant_envs}

    expected = None
    if result.cpython is None:
        # 不计时 CPython 时仍跑一次拿标准答案，保证 RyuPy 输出照常校验
        expected = run_once(cpython_cmd, env, plan.root, None).stdout
    for round_index in range(plan.warmup + plan.runs):
        record = round_index >= plan.warmup
        if result.cpython is not None:
            sample = run_once(cpython_cmd, env, plan.root, None)
            if sample.exit_code != 0:
                raise RuntimeError(f"{bench.name}: CPython 运行失败\n{sample.stderr.decode(errors='replace')}")
            expected = sample.stdout
            if record:
                result.cpython.add(sample)
        if result.ryupy is None:
            continue
        sample = run_once(ryupy_cmd, env, plan.root, "RYUPY_STATS")
        if sample.exit_code != 0:
            result.status = "run-failed"
            result.note = sample.stderr.decode(errors="replace").strip().splitlines()[-1:]
            result.note = result.note[0] if result.note else f"退出码 {sample.exit_code}"
            result.ryupy = None
            result.variants = {}
            break
        if expected is not None and sample.stdout != expected:
            result.status = "mismatch"
        if record:
            result.ryupy.add(sample)
        for label, variant_env in variant_envs.items():
            variant = run_once(ryupy_cmd, variant_env, plan.root, "RYUPY_STATS")
            if expected is not None and variant.stdout != expected:
                result.status = "mismatch"
            if record:
                result.variants[label].add(variant)

    if bench.pending and result.status == "ok":
        # pending 基准已能编译且输出一致：提示去掉标记转正
        result.status = "ready"
    return result
