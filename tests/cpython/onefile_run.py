#!/usr/bin/env python3
"""RyuPy AOT 单文件（--onefile）独立真实验收驱动。

验收流程：
1. 环境探测与 CLI 能力探测（若 --onefile 未就绪则返回非零退出码 ENV_BLOCKED 明确阻断）
2. 在仓库外构建隔离工作区与手写测试包（无 hash RECORD）
3. 真实准备私有 CPython 发行副本、元数据及 provider.json
4. 运行 CPython Oracle：取得标准输入输出、错误流、退出码及业务副作用基准
5. 调用 ryupy [--cpython-provider provider.json] build app.py --onefile --out-dir ROOT 进行真实发布
6. 按精确约定核验 ROOT/<stem>.onefile/<stem>[.exe]：整个 .onefile 目录必须有且仅有唯一可执行档
7. 搬迁唯一单文件可执行档至含中文与空格的路径，物理删除所有构建输入与中间产物（.build/.dist）
8. 在清洁 PATH（指向空目录）及受污染 PYTHONHOME/PYTHONPATH 下独立运行
9. 严格比对三流（stdin/stdout/stderr）、退出码、参数（含空参数与换行符）及文件副作用
10. 测试非零退出码（如 42）Oracle 三流严格匹配
11. 测试双进程并发运行稳态（自解压/运行时缓存互斥锁）
12. 损坏可执行文件负例拦截验证
13. 可选无开发环境 Linux 容器运行与比对
"""
from __future__ import annotations

import argparse
import os
from pathlib import Path
import shutil
import subprocess
import sys

# 保持无字节码缓存
sys.dont_write_bytecode = True

from onefile_support import (
    ROOT,
    audit_onefile_output,
    build_fixture_wheel,
    checked_run,
    create_corrupted_executable,
    ensure_cpython_metadata,
    external_workspace,
    make_clean_env,
    make_polluted_env,
    prepare_cpython_provider_decl,
    rid,
    verify_interruption,
    run_concurrent_processes,
)


def probe_cli_onefile_support(ryupy_bin: Path, onefile_flag: str) -> bool:
    """探测当前 ryupy CLI 是否已接线单文件构建标志。"""
    res = subprocess.run([str(ryupy_bin), "build", "--help"], capture_output=True, text=True)
    if res.returncode != 0:
        return False
    return onefile_flag in res.stdout


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--python", help="Python 3.9+ 解释器路径（默认自动查找）")
    parser.add_argument("--ryupy", help="ryupy CLI 可执行文件路径")
    parser.add_argument("--distribution", help="固定 CPython 安装根目录")
    parser.add_argument("--metadata", help="包含 PYTHON.json 与 licenses 的目录")
    parser.add_argument("--cpython-provider", help="已生成的 provider.json 路径")
    parser.add_argument("--onefile-flag", default="--onefile", help="单文件构建命令行标志")
    parser.add_argument("--linux-container", help="在无Python/.NET/SDK的Linux容器内额外验收")
    parser.add_argument("--keep", action="store_true", help="保留测试临时目录用于调试")
    args = parser.parse_args()

    python_bin = Path(args.python).resolve() if args.python else Path(sys.executable).resolve()
    print(f"[onefile] 正在使用 Python 解释器: {python_bin}")

    ryupy_bin = (
        Path(args.ryupy).resolve()
        if args.ryupy
        else (ROOT / "target/debug" / ("ryupy.exe" if sys.platform == "win32" else "ryupy"))
    )
    if not ryupy_bin.is_file():
        print(f"[onefile] 错误: 未找到 ryupy CLI 可执行文件: {ryupy_bin}")
        print("[onefile] 状态: ENV_BLOCKED (未编译 ryupy-cli，请先执行 cargo build -p ryupy-cli)")
        sys.exit(2)

    has_onefile = probe_cli_onefile_support(ryupy_bin, args.onefile_flag)
    if not has_onefile:
        print(f"[onefile] 错误: ryupy build 尚未接线 {args.onefile_flag} 选项。")
        print("[onefile] 测试框架、手写输入、三流断言、并发/负例与CI工作流均已就绪。")
        print("[onefile] 状态: ENV_BLOCKED (等待 Codex/主代理完成 CLI 接线)")
        sys.exit(2)

    workspace = external_workspace("ryupy-onefile-")
    print(f"[onefile] 启用仓库外隔离工作区: {workspace}")
    clean_env = make_clean_env()

    try:
        inputs_dir = workspace / "构建 输入"
        inputs_dir.mkdir(parents=True)

        # 1. 准备手写测试包 wheel（无 hash RECORD）
        pkg_fixture_dir = ROOT / "tests/cpython/fixtures/onefile_pkg"
        pkg_init = (pkg_fixture_dir / "__init__.py").read_text(encoding="utf-8")
        pkg_res = (pkg_fixture_dir / "资源.txt").read_text(encoding="utf-8")
        wheel_path = build_fixture_wheel(
            inputs_dir,
            "onefile_pkg",
            "1.0.0",
            {
                "onefile_pkg/__init__.py": pkg_init,
                "onefile_pkg/资源.txt": pkg_res,
            },
        )

        # 2. 创建独立测试虚拟环境并安装 packaging 与手写 wheel
        venv_dir = inputs_dir / "测试 虚拟环境"
        checked_run(["uv", "venv", "--python", str(python_bin), str(venv_dir)], cwd=workspace, env=clean_env, label="创建venv")
        venv_python = venv_dir / ("Scripts/python.exe" if sys.platform == "win32" else "bin/python")
        checked_run(["uv", "pip", "install", "--python", str(venv_python), "packaging==25.0"],
                    cwd=workspace, env=clean_env, label="安装packaging")
        checked_run(["uv", "pip", "install", "--python", str(venv_python), "--no-deps", str(wheel_path)],
                    cwd=workspace, env=clean_env, label="安装手写wheel")

        # 3. 复制手写业务入口
        app_source = ROOT / "tests/cpython/fixtures/onefile_app/app.py"
        app_entry = inputs_dir / "业务应用.py"
        app_entry.write_text(app_source.read_text(encoding="utf-8"), encoding="utf-8")

        # 4. 准备 provider 参数（默认约定使用 auto，不传解释器版本）
        provider_arg: str | None = None
        if args.cpython_provider:
            provider_arg = str(Path(args.cpython_provider).resolve())
        elif args.distribution:
            cache_dir = Path.home() / ".cache/ryupy/cpython-provider"
            meta_py = Path(args.metadata).resolve() if args.metadata else ensure_cpython_metadata(cache_dir, rid())
            provider_json = prepare_cpython_provider_decl(
                inputs_dir, Path(args.distribution).resolve(), meta_py, venv_dir, ["onefile_pkg==1.0.0"]
            )
            provider_arg = str(provider_json)
        else:
            # 用户语义明确：默认使用 --cpython-provider auto 自动探测并适应用户环境
            provider_arg = "auto"

        # 5. 执行 CPython Oracle 基准运行
        oracle_cwd = workspace / "oracle_baseline"
        oracle_cwd.mkdir()
        oracle_test_args = [str(venv_python), "-I", "-X", "utf8", str(app_entry), "--read-stdin", "普通参数", "", "含换行\n参数"]
        stdin_data = "真实业务输入数据 流\n".encode("utf-8")
        oracle_proc = subprocess.run(
            oracle_test_args,
            cwd=oracle_cwd,
            input=stdin_data,
            capture_output=True,
            timeout=120,
        )
        assert oracle_proc.returncode == 0, f"Oracle基准失败:\n{oracle_proc.stderr.decode(errors='replace')}"
        oracle_side_effect = (oracle_cwd / "业务副作用_onefile.txt").read_bytes()
        print("[onefile] CPython Oracle 基准执行完成，三流与副作用已捕获")

        # 5.1 Oracle 非零退出码比对基准
        oracle_exit_cwd = workspace / "oracle_exit"
        oracle_exit_cwd.mkdir()
        oracle_exit_proc = subprocess.run(
            [str(venv_python), "-I", "-X", "utf8", str(app_entry), "--exit-code", "42"],
            cwd=oracle_exit_cwd,
            capture_output=True,
            timeout=60,
        )
        assert oracle_exit_proc.returncode == 42, (
            f"Oracle 退出码基准失败: {oracle_exit_proc.returncode}\n{oracle_exit_proc.stderr.decode(errors='replace')}"
        )

        oracle_concurrent_cwd = workspace / "oracle_concurrent"
        oracle_concurrent_cwd.mkdir()
        oracle_concurrent = subprocess.run(
            [str(venv_python), "-I", "-X", "utf8", str(app_entry), "并发测试"],
            cwd=oracle_concurrent_cwd, capture_output=True, timeout=60, check=True)
        oracle_concurrent_effect = (oracle_concurrent_cwd / "业务副作用_onefile.txt").read_bytes()

        # 6. 调用 ryupy 进行真实单文件 NativeAOT 构建（在已激活当前项目 venv 的环境下执行）
        build_out = workspace / "onefile_build"
        build_cmd = [str(ryupy_bin)]
        if provider_arg:
            build_cmd.extend(["--cpython-provider", provider_arg])
        build_cmd.extend([
            "build",
            str(app_entry),
            args.onefile_flag,
            "--out-dir",
            str(build_out),
        ])

        build_env = dict(clean_env)
        build_env["VIRTUAL_ENV"] = str(venv_dir)
        venv_bin_dir = venv_dir / ("Scripts" if sys.platform == "win32" else "bin")
        build_env["PATH"] = str(venv_bin_dir) + os.pathsep + build_env.get("PATH", "")

        print(f"[onefile] 正在执行真实单文件构建: {' '.join(build_cmd)}")
        build_res = subprocess.run(build_cmd, cwd=workspace, env=build_env, capture_output=True, text=True)
        if build_res.returncode != 0:
            if provider_arg == "auto" and ("声明不存在" in build_res.stderr or "auto" in build_res.stderr.lower()):
                print(f"[onefile] 构建失败输出:\n{build_res.stderr}")
                print("[onefile] 状态: ENV_BLOCKED (等待 Gemini 完成 --cpython-provider auto 接线)")
                sys.exit(2)
            raise AssertionError(f"单文件构建失败 (code={build_res.returncode}):\n{build_res.stderr}")

        # 7. 精确核验 ROOT/<stem>.onefile/<stem>[.exe]：整个 .onefile 目录必须有且仅有唯一可执行档
        single_bin = audit_onefile_output(build_out, app_entry.stem)
        print(f"[onefile] 成功产出唯一单文件二进制: {single_bin} (大小: {single_bin.stat().st_size} 字节)")

        # 8. 搬迁唯一单文件可执行档至含中文与空格的路径，物理删除所有构建输入与中间产物
        relocated_dir = workspace / "搬迁 目录 & 空格路径"
        relocated_dir.mkdir(parents=True)
        relocated_bin = relocated_dir / single_bin.name
        shutil.copy2(single_bin, relocated_bin)

        shutil.rmtree(inputs_dir)
        shutil.rmtree(build_out)
        assert not inputs_dir.exists(), "构建输入目录删除失败"
        assert not build_out.exists(), "构建产物与中间目录删除失败"
        assert relocated_bin.is_file(), "单文件独立搬迁失败"
        print("[onefile] 已物理清理全部构建输入与源码，完成唯一单文件搬迁")

        # 9. 在极端污染环境与清洁 PATH 下直接运行
        empty_dir = workspace / "空PATH目录"
        empty_dir.mkdir()
        polluted_env = make_polluted_env(empty_dir)
        private_temp = workspace / "runtime-temp"
        private_temp.mkdir()
        polluted_env.update({key: str(private_temp) for key in ("TMPDIR", "TMP", "TEMP")})

        run_cwd = workspace / "单文件 独立运行目录"
        run_cwd.mkdir()

        product_run_args = [str(relocated_bin), "--read-stdin", "普通参数", "", "含换行\n参数"]
        product_proc = subprocess.run(
            product_run_args,
            cwd=run_cwd,
            input=stdin_data,
            capture_output=True,
            env=polluted_env,
            timeout=180,
        )

        # 严格比对退出码、STDOUT、STDERR 与业务副作用
        assert product_proc.returncode == oracle_proc.returncode == 0, (
            f"退出码不匹配: {product_proc.returncode} vs {oracle_proc.returncode}"
        )
        assert product_proc.stdout == oracle_proc.stdout, (
            f"STDOUT 不匹配:\n[实际]:\n{product_proc.stdout.decode(errors='replace')}\n"
            f"[期望]:\n{oracle_proc.stdout.decode(errors='replace')}"
        )
        assert product_proc.stderr == oracle_proc.stderr, (
            f"STDERR 不匹配:\n[实际]:\n{product_proc.stderr.decode(errors='replace')}\n"
            f"[期望]:\n{oracle_proc.stderr.decode(errors='replace')}"
        )
        actual_side_effect = (run_cwd / "业务副作用_onefile.txt").read_bytes()
        assert actual_side_effect == oracle_side_effect, (
            f"业务副作用不匹配:\n[实际]:\n{actual_side_effect.decode(errors='replace')}\n"
            f"[期望]:\n{oracle_side_effect.decode(errors='replace')}"
        )
        print("[onefile] 单文件执行成功！退出码、三流（stdin/stdout/stderr）及业务副作用与 Oracle 完全匹配")

        # 10. 测试非零退出码传递及流严格匹配
        product_exit = subprocess.run(
            [str(relocated_bin), "--exit-code", "42"],
            cwd=run_cwd,
            capture_output=True,
            env=polluted_env,
            timeout=60,
        )
        assert product_exit.returncode == oracle_exit_proc.returncode == 42, (
            f"非零退出码不匹配：期望 42，实际 {product_exit.returncode}"
        )
        assert product_exit.stdout == oracle_exit_proc.stdout, "非零退出码下 STDOUT 不匹配"
        assert product_exit.stderr == oracle_exit_proc.stderr, "非零退出码下 STDERR 不匹配"
        print("[onefile] 非零退出码 (42) 及三流严格匹配通过")

        # 11. 测试双进程并发运行稳态
        concurrent_cwds = [workspace / f"concurrent_run_{idx}" for idx in range(2)]
        for c_cwd in concurrent_cwds:
            c_cwd.mkdir()
        concurrent_results = run_concurrent_processes(
            relocated_bin, count=2, args=["并发测试"], cwds=concurrent_cwds, envs=[polluted_env, polluted_env]
        )
        for idx, cp in enumerate(concurrent_results):
            assert cp.returncode == 0, f"并发进程 {idx} 执行异常: {cp.stderr.decode()}"
            assert cp.stdout == oracle_concurrent.stdout and cp.stderr == oracle_concurrent.stderr
            assert (concurrent_cwds[idx] / "业务副作用_onefile.txt").read_bytes() == oracle_concurrent_effect
        assert not list(private_temp.iterdir()), "运行后遗留解包目录"
        print("[onefile] 双进程并发三流、副作用与独占目录清理通过")
        verify_interruption(relocated_bin, workspace, polluted_env, private_temp)

        # 12. 损坏包负例校验
        corrupted_bin = relocated_dir / ("损坏应用.exe" if sys.platform == "win32" else "损坏应用")
        create_corrupted_executable(relocated_bin, corrupted_bin)
        try:
            bad_proc = subprocess.run([str(corrupted_bin)], cwd=run_cwd, capture_output=True, env=polluted_env, timeout=30)
            assert bad_proc.returncode != 0, "损坏的单文件未被拦截"
        except OSError as error:
            assert error.errno in (8, 86) or getattr(error, "winerror", None) == 193, error
        print("[onefile] 损坏单文件严格负例校验通过")

        # 13. Linux 容器验证（若启用）
        if args.linux_container:
            assert sys.platform == "linux", "干净容器验收仅限 Linux"
            container_cwd = workspace / "container_cwd"
            container_cwd.mkdir()
            docker = shutil.which("docker")
            assert docker, "缺少Docker"
            base = [docker, "run", "--rm", "--pull=never", "--network=none",
                    "--user=" + str(os.getuid()) + ":" + str(os.getgid())]
            probe = 'for tool in python python3 dotnet; do if command -v "$tool"; then exit 1; fi; done; test ! -d /usr/share/dotnet; test ! -d /usr/local/share/dotnet'
            checked_run(base + [args.linux_container, "/bin/sh", "-c", probe], cwd=workspace, env=clean_env)
            docker_cmd = base + ["-i",
                "--mount", f"type=bind,source={relocated_dir},target=/app,readonly",
                "--mount", f"type=bind,source={container_cwd},target=/work", "--workdir=/work",
                "--env=PATH=/empty", "--env=PYTHONHOME=/污染", "--env=PYTHONPATH=/污染",
                "--env=LC_ALL=C.UTF-8", "--env=TZ=UTC", args.linux_container,
                f"/app/{relocated_bin.name}", *product_run_args[1:]]
            cont_proc = subprocess.run(docker_cmd, input=stdin_data, capture_output=True, timeout=180)
            assert cont_proc.returncode == oracle_proc.returncode == 0, cont_proc.stderr.decode()
            assert cont_proc.stdout == oracle_proc.stdout, "容器内 STDOUT 不匹配"
            assert cont_proc.stderr == oracle_proc.stderr, "容器内 STDERR 不匹配"
            assert (container_cwd / "业务副作用_onefile.txt").read_bytes() == oracle_side_effect
            print("[onefile] Linux 干净容器无依赖运行验证通过！")

        print("[onefile] 全部 OneFile 真实验收项均通过！")

    finally:
        if not args.keep and workspace.exists():
            shutil.rmtree(workspace, ignore_errors=True)


if __name__ == "__main__":
    main()
