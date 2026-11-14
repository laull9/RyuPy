"""RyuPy 基准测量核心。

负责：
1. 编译 Python 源码为托管 DLL 或已核验证的 NativeAOT 原生入口（输出至本次独占仓库外工作根）；
2. 采样协议：直接脚本 CPython 与无 stats 注入 RyuPy 计真实墙钟与峰值 RSS，单独 instrumented 轮收集内部指标；
3. 子进程通过 os.wait4 采集真实墙钟与 ru_maxrss 峰值内存；
4. 严格校验退出码与 stdout/stderr 双流；统计 JSON 损坏坚决报错，拒绝吞空；
5. 依 ADR-0182，基准构建与执行产物放在仓库外独占临时目录并清理，不污染 target/bench。
"""

from __future__ import annotations

from dataclasses import dataclass, field
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import time

from .suite import Benchmark

HARNESS = Path(__file__).resolve().parent / "cpython_harness.py"


@dataclass
class Sample:
    """单次运行的采样值。"""

    wall_ms: float
    max_rss_bytes: int
    stats: dict
    stdout: bytes
    stderr: bytes
    exit_code: int = 0


@dataclass
class Series:
    """一个基准在同一配置下的多次采样。"""

    wall_ms: list[float] = field(default_factory=list)
    main_ms: list[float] = field(default_factory=list)
    maxrss: list[int] = field(default_factory=list)
    stats: dict = field(default_factory=dict)

    def add(self, sample: Sample) -> None:
        self.wall_ms.append(sample.wall_ms)
        self.maxrss.append(sample.max_rss_bytes)
        if "main_ms" in sample.stats:
            self.main_ms.append(sample.stats["main_ms"])

    def to_json(self) -> dict:
        return {
            "wall_ms": self.wall_ms,
            "main_ms": self.main_ms,
            "maxrss": self.maxrss,
            "stats": self.stats,
        }

    @classmethod
    def from_json(cls, data: dict) -> Series:
        return cls(
            wall_ms=list(data.get("wall_ms", [])),
            main_ms=list(data.get("main_ms", [])),
            maxrss=list(data.get("maxrss", data.get("max_rss", []))),
            stats=dict(data.get("stats", {})),
        )


@dataclass
class Result:
    bench: Benchmark
    compile_ms: float = 0.0
    cpython: Series | None = None
    ryupy: Series | None = None
    variants: dict[str, Series] = field(default_factory=dict)
    # "ok" / "mismatch" / "build-failed" / "artifact-failed" / "run-failed" / "variant-failed" / "pending" / "ready"
    status: str = "ok"
    note: str = ""
    backend: str = "jit"
    native: bool = True
    cpython_direct_ms: float | None = None


def get_safe_external_dir(repo_root: Path) -> Path:
    """获取真正的仓库外临时目录，强制防止 gettempdir 位于 repo 内部。"""
    repo = repo_root.resolve()
    for candidate in (tempfile.gettempdir(), "/tmp", "/var/tmp"):
        tmp = Path(candidate).resolve()
        if not tmp.is_relative_to(repo) and tmp.is_dir() and os.access(tmp, os.W_OK):
            return tmp
    raise ValueError("未找到可写的仓库外临时根")


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


def build_cli(root: Path, target: Path, cli_override: Path | None = None) -> Path:
    """获取或构建 Release CLI。若显式传入 cli_override 则直接使用冻结副本。"""
    if cli_override is not None:
        cli = cli_override.resolve()
        if not cli.is_file() or not os.access(cli, os.X_OK):
            raise FileNotFoundError(f"指定的 CLI 路径不存在或无执行权限：{cli}")
        print(f"使用指定 CLI：{cli}", flush=True)
        # 空跑一次预热
        subprocess.run([str(cli), "--help"], cwd=root, capture_output=True)
        return cli

    cli = target / "release" / "ryupy"
    # 普通入口始终让 Cargo 检查源码新鲜度；冻结二进制仅由 --cli 显式选择。
    print("构建 Release CLI ...", flush=True)
    subprocess.run(["cargo", "build", "--release", "-q", "-p", "ryupy-cli"], cwd=root, check=True)
    subprocess.run([str(cli), "--help"], cwd=root, capture_output=True)
    return cli


def build_case(
    root: Path,
    cli: Path,
    bench: Benchmark,
    work_root: Path,
    *,
    backend: str = "jit",
    native: bool = True,
) -> tuple[Path | None, float, str, str]:
    """返回本次编译产物入口、耗时、错误日志与状态。

    返回：(产物路径或None, 编译耗时ms, 错误详细日志, 状态字符串)
    产物输出严格限制于本次独占 work_root，构建失败保留完整 stderr。
    """
    if backend not in ("jit", "aot"):
        raise ValueError("未知基准 backend")

    if backend == "aot":
        # 每次使用独占父目录；构建失败绝不复用上次产物
        parent = work_root / "bench" / "aot"
        parent.mkdir(parents=True, exist_ok=True)
        out_dir = Path(tempfile.mkdtemp(prefix=bench.name + "-", dir=parent))
    else:
        out_dir = work_root / "bench" / bench.name
        out_dir.mkdir(parents=True, exist_ok=True)

    command = [str(cli), "build", str(bench.path), "-o", str(out_dir)]
    if backend == "aot":
        command.append("--aot")
    if native:
        command.append("--native")

    start = time.perf_counter()
    result = subprocess.run(
        command,
        cwd=root,
        capture_output=True,
        text=True,
    )
    elapsed = (time.perf_counter() - start) * 1000

    if result.returncode != 0:
        # 保存完整 stderr 信息，不再仅截取末 3 行
        full_err = result.stderr.strip() or f"构建以非零退出码 {result.returncode} 终止"
        return None, elapsed, full_err, "build-failed"

    if backend == "aot":
        from .artifact import verified_native

        try:
            native_path = verified_native(out_dir, bench, result.stderr, mode="standalone")
            return native_path, elapsed, "", "ok"
        except Exception as error:
            # 原生识别/核验失败单列状态 artifact-failed
            return None, elapsed, f"AOT发布验证失败：{error}\n完整CLI输出：\n{result.stderr}", "artifact-failed"

    dll = out_dir / f"{bench.name}.dll"
    if not dll.is_file():
        return None, elapsed, "成功构建未产生 DLL", "build-failed"
    return dll, elapsed, "", "ok"


def _normalize_rss(raw: int) -> int:
    # macOS 的 ru_maxrss 单位是字节，Linux 是 KiB
    return raw if sys.platform == "darwin" else raw * 1024


def run_once(command: list[str], env: dict[str, str], cwd: Path, stats_env: str | None) -> Sample:
    """启动一次子进程并计时。

    `stats_env` 是统计文件路径的环境变量名（RyuPy：`RYUPY_STATS`）；为 None 表示直接纯净运行或外壳。
    统计 JSON 解析失败坚决抛错，拒绝吞空。
    """
    with tempfile.TemporaryDirectory(dir=get_safe_external_dir(cwd)) as tmp:
        stats_path = Path(tmp) / "stats.json"
        run_env = dict(env)
        cmd = list(command)
        if stats_env:
            run_env[stats_env] = str(stats_path)
        elif len(cmd) >= 3 and str(HARNESS) in cmd:
            # CPython 外壳：python -B harness <stats> <script>
            cmd.insert(-1, str(stats_path))

        with open(Path(tmp) / "out", "w+b") as out, open(Path(tmp) / "err", "w+b") as err:
            start = time.perf_counter()
            proc = subprocess.Popen(cmd, cwd=cwd, env=run_env, stdout=out, stderr=err)
            _, status, usage = os.wait4(proc.pid, 0)
            elapsed = (time.perf_counter() - start) * 1000
            proc.returncode = os.waitstatus_to_exitcode(status)
            out.seek(0)
            err.seek(0)
            stdout, stderr = out.read(), err.read()

        stats = {}
        if stats_path.exists() and stats_path.stat().st_size > 0:
            content = stats_path.read_text(encoding="utf-8")
            if content.strip():
                try:
                    stats = json.loads(content)
                except Exception as exc:
                    raise ValueError(f"统计文件 JSON 损坏：{exc}，内容：\n{content[:500]}") from exc

    return Sample(elapsed, _normalize_rss(usage.ru_maxrss), stats, stdout, stderr, proc.returncode)


@dataclass
class Plan:
    """一次基准测试运行参数。"""

    root: Path
    python: str
    warmup: int
    runs: int
    with_cpython: bool
    include_pending: bool
    work_root: Path
    backend: str = "jit"
    native: bool = True


def _base_env() -> dict[str, str]:
    env = dict(os.environ)
    for key in ("RYUPY_PIC", "RYUPY_STATS", "PYTHONHASHSEED"):
        env.pop(key, None)
    env["PYTHONHASHSEED"] = "0"
    return env


def measure_bench(plan: Plan, cli: Path, target: Path, bench: Benchmark) -> Result:
    result = Result(bench, backend=plan.backend, native=plan.native)
    if bench.pending and not plan.include_pending:
        result.status = "pending"
        return result

    env = _base_env()
    # 编译当前基准
    dll, compile_ms, error, build_status = build_case(
        plan.root,
        cli,
        bench,
        plan.work_root,
        backend=plan.backend,
        native=plan.native,
    )
    result.compile_ms = compile_ms
    if dll is None:
        result.status = "pending" if bench.pending else build_status
        result.note = error
        return result

    ryupy_clean_cmd = ([str(dll)] if plan.backend == "aot" else ["dotnet", str(dll)])
    cpython_clean_cmd = [plan.python, "-B", str(bench.path)]
    variant_envs = {v.label: {**env, **v.env} for v in bench.variants}

    # 1. 严格检查 CPython Oracle（即使 --no-cpython 也要运行一次获取预期）
    oracle_sample = run_once(cpython_clean_cmd, env, plan.root, None)
    if oracle_sample.exit_code != 0:
        result.status = "cpython-failed"
        result.note = f"CPython 退出异常 (code {oracle_sample.exit_code})：\n{oracle_sample.stderr.decode(errors='replace')}"
        return result
    expected_stdout = oracle_sample.stdout
    expected_stderr = oracle_sample.stderr

    if plan.with_cpython:
        result.cpython = Series()
    result.ryupy = Series()
    result.variants = {label: Series() for label in variant_envs}

    # 2. 主采样轮：纯净无外壳直接运行，测量真实墙钟与峰值 RSS，交替采样
    total_rounds = plan.warmup + plan.runs
    for round_index in range(total_rounds):
        record = round_index >= plan.warmup

        # CPython 纯净测量
        if plan.with_cpython:
            cp_sample = run_once(cpython_clean_cmd, env, plan.root, None)
            if cp_sample.exit_code != 0:
                result.status = "cpython-failed"
                result.note = f"CPython 采样失败 (code {cp_sample.exit_code})：\n{cp_sample.stderr.decode(errors='replace')}"
                return result
            if (cp_sample.stdout, cp_sample.stderr) != (expected_stdout, expected_stderr):
                result.status = "mismatch"
                result.note = "CPython 多次执行的双流不稳定"
                return result
            if record:
                result.cpython.add(cp_sample)

        # RyuPy 纯净测量（无 RYUPY_STATS 注入）
        ryu_sample = run_once(ryupy_clean_cmd, env, plan.root, None)
        if ryu_sample.exit_code != 0:
            result.status = "run-failed"
            result.note = f"RyuPy 执行失败 (code {ryu_sample.exit_code})：\n{ryu_sample.stderr.decode(errors='replace')}"
            result.ryupy = None
            result.variants = {}
            return result

        if (ryu_sample.stdout, ryu_sample.stderr) != (expected_stdout, expected_stderr):
            result.status = "mismatch"
            result.note = f"输出不一致：预期长度 {len(expected_stdout)}，实际长度 {len(ryu_sample.stdout)}"
            return result

        if record:
            result.ryupy.add(ryu_sample)

        # 变体纯净测量
        for label, variant_env in variant_envs.items():
            var_sample = run_once(ryupy_clean_cmd, variant_env, plan.root, None)
            if var_sample.exit_code != 0:
                result.status = "variant-failed"
                result.note = f"变体 [{label}] 执行失败 (code {var_sample.exit_code})：\n{var_sample.stderr.decode(errors='replace')}"
                return result
            if (var_sample.stdout, var_sample.stderr) != (expected_stdout, expected_stderr):
                result.status = "mismatch"
                result.note = f"变体 [{label}] 输出不匹配"
                return result
            if record:
                result.variants[label].add(var_sample)

    # 3. 独立 instrumented 轮：仅收集内部统计指标（MarkMainStart、分配量、GC 次数、暂停耗时）
    try:
        inst_sample = run_once(ryupy_clean_cmd, env, plan.root, "RYUPY_STATS")
        if inst_sample.exit_code != 0 or (inst_sample.stdout, inst_sample.stderr) != (expected_stdout, expected_stderr):
            raise ValueError(f"统计轮退出或双流错误：{inst_sample.exit_code} {inst_sample.stderr!r}")
        if not isinstance(inst_sample.stats, dict) or "main_ms" not in inst_sample.stats:
            raise ValueError("统计轮缺少有效 main_ms")
        if result.ryupy is not None:
            result.ryupy.stats = inst_sample.stats
            if "main_ms" in inst_sample.stats and not result.ryupy.main_ms:
                result.ryupy.main_ms.append(inst_sample.stats["main_ms"])
    except Exception as exc:
        result.status = "run-failed"
        result.note = f"独立统计轮失败：{exc}"
        return result

    # 若需要 CPython 侧的 GC 计数统计，通过 harness 单跑一次
    if plan.with_cpython and result.cpython is not None:
        try:
            harness_cmd = [plan.python, "-B", str(HARNESS), str(bench.path)]
            cp_inst = run_once(harness_cmd, env, plan.root, None)
            if cp_inst.exit_code != 0 or (cp_inst.stdout, cp_inst.stderr) != (expected_stdout, expected_stderr):
                raise ValueError(f"CPython 统计轮退出或双流错误：{cp_inst.exit_code} {cp_inst.stderr!r}")
            result.cpython.stats = cp_inst.stats
        except Exception as exc:
            result.status = "cpython-failed"
            result.note = f"CPython 独立统计轮失败：{exc}"
            return result

    if bench.pending and result.status == "ok":
        result.status = "ready"

    return result
