#!/usr/bin/env python3
"""ADR-0208 C0/C1 产品真实验证驱动：同次 CPython oracle、搬迁执行、生命周期审计与执行门拦截。

本驱动为 RyuPy 全 CIL 执行与 libpython 底层化（ADR-0208）提供可复用的端到端真实产品验证：
1. 固定 CPython 3.12.13 同次运行获取 oracle 基准（退出码、stdout、stderr）；
2. 准备仓库外隔离工作区与私有发行副本（不污染用户环境与源发行）；
3. 支持 JIT、Standalone（NativeAOT 目录发布）与 OneFile 单文件三种发布形态；
4. 构建完成后物理删除私有源码输入与构建中间目录，产物搬迁至含中文与空格的路径执行；
5. 在清洁 PATH 及被污染 PYTHONHOME/PYTHONPATH 下运行，注入显式审计环境变量：
   RYUPY_CPYTHON_EXECUTION_AUDIT=1 与 RYUPY_CPYTHON_REPORT=<路径>；
6. 严格比对三流与退出码（不吞 stderr，不掩盖未捕获异常）；
7. 详细核验宿主审计报告：
   - 宿主状态关闭（state=closed）、零丢帧（dropped_frames=0）、零覆盖失败（coverage_failures=0）；
   - startup / runtime / shutdown 三阶段均存在真实 Python 帧，且明细聚合与阶段计数一致；
   - shutdown 阶段包含真实 atexit 回调执行；
   - 确认 C1 候选已移除的四个 helper（_rp_object_new, _rp_object_init, _rp_object_setattr, _rp_object_delattr）帧数为 0；
   - 确认保留的活跃 helper（_rp_make_class, _rp_wrap_method 等）帧数非零，证明 PEP 523 钩子真实有效；
   - 严格门（check_audit.py）对当前 complete=false / startup_complete=false 明确预期拒绝；
8. 验证结束后仓库外临时目录自动彻底清理（不做 hash，不留脏文件）。
"""
from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

# 禁止驱动自身生成字节码缓存
sys.dont_write_bytecode = True

CURRENT_DIR = Path(__file__).resolve().parent
ROOT = CURRENT_DIR.parents[1]

# 引入复用支撑模块
for path in (ROOT / "tests/cpython", CURRENT_DIR):
    if str(path) not in sys.path:
        sys.path.insert(0, str(path))

from check_audit import PHASES, audit_failures
from onefile_support import audit_onefile_output
from support import (
    VERSION,
    audit_native_paths,
    checked,
    declaration,
    environment,
    execute,
    external_workspace,
    inspect_host_report,
    private_distribution,
    require_python,
    rid,
    standalone_layout,
    write_json,
)

DEFAULT_PROBE = CURRENT_DIR / "product_fixtures/audited_app.py"
REMOVED_HELPERS = (
    "_rp_object_new",
    "_rp_object_init",
    "_rp_object_setattr",
    "_rp_object_delattr",
)
ACTIVE_HELPERS = ("_rp_make_class", "_rp_wrap_method", "_rp_calculate_meta")
COMMON_MODULES = [
    "sys", "base64", "json", "sqlite3", "zlib", "collections",
    "contextlib", "atexit", "numpy", "urllib", "io", "logging",
]


def find_default_python() -> Path:
    """寻找固定 CPython 3.12.13 解释器。"""
    try:
        res = subprocess.run(["uv", "python", "find", VERSION], capture_output=True, text=True)
        if res.returncode == 0 and res.stdout.strip():
            candidate = Path(res.stdout.strip()).resolve()
            if candidate.is_file():
                return candidate
    except OSError:
        pass
    uv_pool = Path.home() / ".local/share/uv/python"
    if uv_pool.is_dir():
        for candidate in sorted(uv_pool.glob(f"cpython-{VERSION}-*/bin/python3.12")):
            if candidate.is_file():
                return candidate.resolve()
    pinned_link = Path.home() / ".local/bin/python3.12"
    if pinned_link.is_file():
        return pinned_link.resolve()
    return Path(sys.executable).resolve()


def find_default_metadata() -> Path | None:
    """寻找固定 PBS 20260623 metadata 目录。"""
    meta_dir = Path.home() / ".cache/ryupy/cpython-provider/pbs-20260623-metadata/python"
    if meta_dir.is_dir() and (meta_dir / "PYTHON.json").is_file():
        return meta_dir
    return None


def run_oracle(python_bin: Path, fixture: Path, cwd: Path, env: dict[str, str]) -> subprocess.CompletedProcess:
    """运行固定 CPython 解释器获取同次 oracle 基准结果。"""
    res = execute([python_bin, "-B", fixture], cwd=cwd, env=env, timeout=120)
    checked(res, "CPython 3.12.13 Oracle 运行")
    assert not res.stderr, f"Oracle 产生非预期 stderr: {res.stderr.decode(errors='replace')}"
    return res


def assert_output_matching(actual: subprocess.CompletedProcess, expected: subprocess.CompletedProcess, label: str) -> None:
    """严格比对三流与退出码，不得吞掉任何非预期 stderr 或异常。"""
    assert actual.returncode == expected.returncode, (
        f"[{label}] 退出码不匹配：期望 {expected.returncode}，实际 {actual.returncode}\n"
        f"STDERR: {actual.stderr.decode(errors='replace')}"
    )
    assert actual.stdout == expected.stdout, (
        f"[{label}] stdout 不匹配：\n"
        f"--- 期望 ---\n{expected.stdout.decode(errors='replace')}\n"
        f"--- 实际 ---\n{actual.stdout.decode(errors='replace')}"
    )
    assert actual.stderr == expected.stderr, (
        f"[{label}] stderr 不匹配：\n"
        f"--- 期望 ---\n{expected.stderr.decode(errors='replace')}\n"
        f"--- 实际 ---\n{actual.stderr.decode(errors='replace')}"
    )


def verify_audit_report(report_path: Path, expected_stdout: bytes, actual_stdout: bytes) -> dict:
    """全面校验生命周期审计报告：关闭状态、丢帧、覆盖、三阶段帧、已移除/保留 helper 及严格门拒绝。"""
    assert report_path.is_file(), f"未生成审计报告文件: {report_path}"
    report = json.loads(report_path.read_text(encoding="utf-8"))

    # 1. 宿主状态与审计开关
    assert report.get("state") == "closed", f"宿主未正常关闭，实际状态: {report.get('state')}"
    assert "execution_audit" in report, "报告中缺少 execution_audit 字段"
    audit = report["execution_audit"]
    assert audit.get("enabled") is True, "execution_audit.enabled 必须为 True"
    assert audit.get("hook_installed") is True, "execution_audit.hook_installed 必须为 True"
    assert audit.get("finalized") is True, "execution_audit.finalized 必须为 True"

    # 2. 覆盖与丢帧校验
    assert audit.get("dropped_frames") == 0, f"存在丢帧: {audit.get('dropped_frames')}"
    assert audit.get("coverage_failures") == 0, f"存在覆盖失败: {audit.get('coverage_failures')}"

    # 3. 三阶段真实 Python 帧校验
    for phase in PHASES:
        count = audit.get(f"{phase}_frames")
        assert isinstance(count, int) and count > 0, (
            f"{phase}_frames 必须为正整数（存在真实 Python 执行帧），实际为: {count}"
        )

    # 4. 明细聚合核验
    frames = audit.get("frames")
    assert isinstance(frames, list), "frames 必须为数组"
    for phase in PHASES:
        phase_sum = sum(row["calls"] for row in frames if row.get("phase") == phase)
        expected_count = audit.get(f"{phase}_frames")
        assert phase_sum == expected_count, (
            f"{phase} 阶段总计数 ({expected_count}) 与 frames 明细聚合和 ({phase_sum}) 不一致"
        )

    # 5. atexit 回调验证
    assert b"AUDIT_ATEXIT_SHUTDOWN_CONFIRMED" in actual_stdout, "stdout 中缺少 atexit 确认标记"
    shutdown_funcs = [row.get("function") for row in frames if row.get("phase") == "shutdown"]
    assert len(shutdown_funcs) > 0, "shutdown 阶段缺少执行帧明细"

    # 6. 四个已移除 helper 帧计数必须为 0
    for helper in REMOVED_HELPERS:
        helper_calls = sum(row["calls"] for row in frames if row.get("function") == helper)
        assert helper_calls == 0, (
            f"已移除的 helper [{helper}] 仍有执行帧：{helper_calls} 次，违背 C1 候选规范"
        )

    # 7. 保留的活跃 helper 帧计数必须非零以确认探针与求值钩子生效
    for helper in ACTIVE_HELPERS:
        calls = sum(row["calls"] for row in frames if row.get("function") == helper)
        assert calls > 0, (
            f"活跃 helper [{helper}] 调用计数必须大于 0 以确认求值钩子有效，实际为 {calls}"
        )

    # 8. 严格门对当前 complete=false / startup_complete=false 明确预期拒绝
    assert audit.get("complete") is False, "当前 C0/C1 报告 complete 必须为 false"
    assert audit.get("startup_complete") is False, "当前 C0/C1 报告 startup_complete 必须为 false"
    gate_failures = audit_failures(report)
    assert gate_failures, "严格执行门必须拒绝当前 complete=false 的报告"
    assert any("complete 必须为 true" in f for f in gate_failures), (
        f"严格门拒绝原因必须包含 'complete 必须为 true'，实际失败原因: {gate_failures}"
    )
    assert any("startup_complete 必须为 true" in f for f in gate_failures), (
        f"严格门拒绝原因必须包含 'startup_complete 必须为 true'，实际失败原因: {gate_failures}"
    )

    return report


def run_jit_validation(
    ryupy_bin: Path,
    fixture_py: Path,
    provider_file: Path | None,
    workspace: Path,
    env: dict[str, str],
    oracle: subprocess.CompletedProcess,
    distribution_hidden: tuple[Path, Path] | None,
) -> None:
    """JIT 模式编译、搬迁与审计核验。"""
    source_dir = fixture_py.parent
    build_out = workspace / "jit 构建产物"
    common = [ryupy_bin]
    if provider_file:
        common.extend(["--cpython-provider", str(provider_file)])
    common.extend(["build", str(fixture_py), "-o", str(build_out)])

    res_build = execute(common, cwd=workspace, env=env, timeout=180)
    checked(res_build, "JIT 模式构建")
    dlls = [
        Path(line.removeprefix("已生成 "))
        for line in res_build.stderr.decode().splitlines()
        if line.startswith("已生成 ")
    ]
    assert len(dlls) == 1, f"JIT 构建未报告唯一 dll: {dlls}"
    target_dll_name = dlls[0].name

    # 搬迁至含中文与空格的路径
    relocated_dir = workspace / "JIT 搬迁 目录 & 中文 空格"
    shutil.move(build_out, relocated_dir)
    shutil.rmtree(source_dir)

    # 隐藏私有基础发行以验证搬迁自洽性
    if distribution_hidden:
        dist_dir, hidden_dir = distribution_hidden
        dist_dir.rename(hidden_dir)

    empty_cwd = workspace / "jit 空工作目录"
    empty_cwd.mkdir()
    clean_env = environment()
    clean_env["PATH"] = str(empty_cwd)
    clean_env["PYTHONHOME"] = clean_env["PYTHONPATH"] = str(empty_cwd / "不存在")
    report_file = empty_cwd / "host.json"
    clean_env["RYUPY_CPYTHON_EXECUTION_AUDIT"] = "1"
    clean_env["RYUPY_CPYTHON_REPORT"] = str(report_file)

    try:
        dotnet_bin = shutil.which("dotnet")
        assert dotnet_bin, "未找到 dotnet 命令"
        res_run = execute([dotnet_bin, str(relocated_dir / target_dll_name)], cwd=empty_cwd, env=clean_env, timeout=120)
        assert_output_matching(res_run, oracle, "JIT 搬迁执行")
        inspect_host_report(report_file, relocated_dir)
        verify_audit_report(report_file, oracle.stdout, res_run.stdout)
    finally:
        if distribution_hidden:
            hidden_dir.rename(dist_dir)


def run_standalone_validation(
    ryupy_bin: Path,
    fixture_py: Path,
    provider_file: Path | None,
    workspace: Path,
    env: dict[str, str],
    oracle: subprocess.CompletedProcess,
    distribution_hidden: tuple[Path, Path] | None,
) -> None:
    """Standalone（NativeAOT 目录发布）编译、搬迁与审计核验。"""
    source_dir = fixture_py.parent
    publish_dir = workspace / "standalone 构建产物"
    common = [ryupy_bin]
    if provider_file:
        common.extend(["--cpython-provider", str(provider_file)])
    common.extend(["build", str(fixture_py), "--aot", "-o", str(publish_dir)])

    res_build = execute(common, cwd=workspace, env=env, timeout=600)
    checked(res_build, "Standalone NativeAOT 构建")
    layout = standalone_layout(res_build.stderr, publish_dir, fixture_py.stem, rid())
    native_name = layout.native.name

    # 搬迁至含中文与空格的路径
    relocated_dir = workspace / "AOT 搬迁 目录 & 中文 空格"
    shutil.move(layout.dist, relocated_dir)
    shutil.rmtree(layout.build)
    shutil.rmtree(source_dir)

    # 隐藏私有基础发行
    if distribution_hidden:
        dist_dir, hidden_dir = distribution_hidden
        dist_dir.rename(hidden_dir)

    empty_cwd = workspace / "aot 空工作目录"
    empty_cwd.mkdir()
    clean_env = environment()
    clean_env["PATH"] = str(empty_cwd)
    clean_env["PYTHONHOME"] = clean_env["PYTHONPATH"] = str(empty_cwd / "不存在")
    report_file = empty_cwd / "host.json"
    clean_env["RYUPY_CPYTHON_EXECUTION_AUDIT"] = "1"
    clean_env["RYUPY_CPYTHON_REPORT"] = str(report_file)

    try:
        audit_native_paths(relocated_dir)
        res_run = execute([str(relocated_dir / native_name)], cwd=empty_cwd, env=clean_env, timeout=120)
        assert_output_matching(res_run, oracle, "Standalone NativeAOT 搬迁执行")
        inspect_host_report(report_file, relocated_dir)
        verify_audit_report(report_file, oracle.stdout, res_run.stdout)
    finally:
        if distribution_hidden:
            hidden_dir.rename(dist_dir)


def run_onefile_validation(
    ryupy_bin: Path,
    fixture_py: Path,
    provider_file: Path | None,
    workspace: Path,
    env: dict[str, str],
    oracle: subprocess.CompletedProcess,
    distribution_hidden: tuple[Path, Path] | None,
) -> None:
    """OneFile 单文件发布形态编译、搬迁与审计核验。"""
    source_dir = fixture_py.parent
    publish_dir = workspace / "onefile 构建产物"
    common = [ryupy_bin]
    if provider_file:
        common.extend(["--cpython-provider", str(provider_file)])
    common.extend(["build", str(fixture_py), "--onefile", "-o", str(publish_dir)])

    res_build = execute(common, cwd=workspace, env=env, timeout=600)
    checked(res_build, "OneFile 单文件构建")
    exe = audit_onefile_output(publish_dir, fixture_py.stem)

    # 搬迁单文件可执行档至含中文与空格的路径
    relocated_dir = workspace / "OneFile 搬迁 目录 & 中文 空格"
    relocated_dir.mkdir()
    relocated_exe = relocated_dir / exe.name
    shutil.move(exe, relocated_exe)
    shutil.rmtree(publish_dir)
    shutil.rmtree(source_dir)

    # 隐藏私有基础发行
    if distribution_hidden:
        dist_dir, hidden_dir = distribution_hidden
        dist_dir.rename(hidden_dir)

    empty_cwd = workspace / "onefile 空工作目录"
    empty_cwd.mkdir()
    clean_env = environment()
    clean_env["PATH"] = str(empty_cwd)
    clean_env["PYTHONHOME"] = clean_env["PYTHONPATH"] = str(empty_cwd / "不存在")
    report_file = empty_cwd / "host.json"
    clean_env["RYUPY_CPYTHON_EXECUTION_AUDIT"] = "1"
    clean_env["RYUPY_CPYTHON_REPORT"] = str(report_file)

    try:
        res_run = execute([str(relocated_exe)], cwd=empty_cwd, env=clean_env, timeout=120)
        assert_output_matching(res_run, oracle, "OneFile 搬迁执行")
        verify_audit_report(report_file, oracle.stdout, res_run.stdout)
    finally:
        if distribution_hidden:
            hidden_dir.rename(dist_dir)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--python", type=Path, help="固定 CPython 3.12.13 可执行文件路径")
    parser.add_argument("--ryupy", type=Path, help="ryupy CLI 可执行文件路径")
    parser.add_argument("--distribution", type=Path, help="CPython 3.12.13 基础发行根目录")
    parser.add_argument("--distribution-metadata", type=Path, help="PBS 20260623 metadata 根目录")
    parser.add_argument("--cpython-provider", help="显式 provider.json 路径，或 'auto'")
    parser.add_argument(
        "--mode",
        choices=["jit", "standalone", "aot", "onefile", "all"],
        default="all",
        help="发布模式：jit（轻量自检）、standalone/aot（目录AOT）、onefile（单文件）或 all",
    )
    parser.add_argument("--fixture", type=Path, help="自定义手写探针输入；默认使用 product_fixtures/audited_app.py")
    parser.add_argument("--out", type=Path, help="仓库外临时工作区根目录")
    parser.add_argument("--keep", action="store_true", help="调试时保留临时工作区")
    args = parser.parse_args()

    python_bin = args.python or find_default_python()
    ryupy_bin = (
        args.ryupy.resolve()
        if args.ryupy
        else (ROOT / "target/debug" / ("ryupy.exe" if sys.platform == "win32" else "ryupy"))
    )
    assert ryupy_bin.is_file(), f"未找到 ryupy CLI 可执行文件: {ryupy_bin}，请先执行 cargo build -p ryupy-cli"

    distribution_dir = args.distribution
    metadata_dir = args.distribution_metadata
    if not distribution_dir and not args.cpython_provider:
        # 默认使用与 python_bin 匹配的固定 CPython 3.12.13 基础发行及 PBS 20260623 metadata
        candidate_dist = python_bin.parents[1]
        if (candidate_dist / "lib/python3.12").is_dir():
            distribution_dir = candidate_dist
        if not metadata_dir:
            metadata_dir = find_default_metadata()

    fixture_src = (args.fixture or DEFAULT_PROBE).resolve()
    assert fixture_src.is_file(), f"未找到探针输入文件: {fixture_src}"

    workspace = external_workspace("ryupy-product-validation-", args.out)
    env = environment()

    print(f"=== RyuPy ADR-0208 C0/C1 产品真实验证 ===")
    print(f"工作区: {workspace}")
    print(f"Python: {python_bin}")
    print(f"CLI:    {ryupy_bin}")
    print(f"发行:   {distribution_dir}")
    print(f"探针:   {fixture_src.name}")
    print(f"模式:   {args.mode}")

    try:
        require_python(python_bin, workspace, env)

        # 1. 运行固定 CPython 获取同次 oracle
        oracle_cwd = workspace / "cpython-oracle-cwd"
        oracle_cwd.mkdir()
        oracle = run_oracle(python_bin, fixture_src, oracle_cwd, env)
        print("✓ CPython 3.12.13 同次 Oracle 执行成功")

        # 2. 准备私有发行与 provider.json
        dist_dir = None
        hidden_dist = None
        provider_file = None

        if args.cpython_provider:
            if args.cpython_provider != "auto":
                provider_file = Path(args.cpython_provider).resolve()
                assert provider_file.is_file(), f"指定 provider 文件不存在: {provider_file}"
        elif distribution_dir:
            dist_dir = private_distribution(distribution_dir, workspace, metadata_dir)
            hidden_dist = workspace / "distribution-hidden"

        modes = ["jit", "standalone", "onefile"] if args.mode == "all" else [
            "standalone" if args.mode == "aot" else args.mode
        ]

        for current_mode in modes:
            print(f"\n--- 正在执行模式: {current_mode} ---")
            source_dir = workspace / f"source-{current_mode}"
            source_dir.mkdir()
            current_fixture = source_dir / fixture_src.name
            shutil.copy2(fixture_src, current_fixture)

            current_provider = provider_file
            if not current_provider and dist_dir:
                current_provider = source_dir / "provider.json"
                write_json(current_provider, declaration(dist_dir, [], COMMON_MODULES))

            dist_tuple = (dist_dir, hidden_dist) if dist_dir else None

            if current_mode == "jit":
                run_jit_validation(ryupy_bin, current_fixture, current_provider, workspace, env, oracle, dist_tuple)
                print("✓ JIT 模式：构建、搬迁、执行匹配、审计核验及严格门拦截全部通过")
            elif current_mode == "standalone":
                run_standalone_validation(ryupy_bin, current_fixture, current_provider, workspace, env, oracle, dist_tuple)
                print("✓ Standalone NativeAOT 模式：构建、搬迁、路径审计、执行匹配、审计核验及严格门拦截全部通过")
            elif current_mode == "onefile":
                run_onefile_validation(ryupy_bin, current_fixture, current_provider, workspace, env, oracle, dist_tuple)
                print("✓ OneFile 模式：构建、搬迁、单文件核验、执行匹配、审计核验及严格门拦截全部通过")

        print(f"\n=== 全部验证模式通过（{', '.join(modes)}）===")
        return 0
    finally:
        if args.keep:
            print(f"保留临时工作区供调试: {workspace}")
        else:
            shutil.rmtree(workspace)


if __name__ == "__main__":
    raise SystemExit(main())
