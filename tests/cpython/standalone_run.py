#!/usr/bin/env python3
"""CPython standalone 多平台独立交付真构建与运行验收驱动。
使用固定 CPython 3.12.13 / PBS 20260623，构建桥与 shim，通过自动依赖闭包打包含 sqlite3、
ssl、默认 UTF-8 文件 IO、第三方包与 metadata 资源的手写小应用，经 NativeAOT 真实编译发布；
搬迁至中文与空格路径，物理删除所有构建输入，并在清洁 PATH 及受污染 PYTHONHOME/PYTHONPATH
环境下直接运行 NativeAOT 可执行文件，严格比对三流与文件副作用，并完成缺失依赖负例验证。
"""
import argparse
import csv
import io
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import urllib.request
import zipfile

# 保持无字节码缓存
sys.dont_write_bytecode = True

from support import (
    ROOT,
    VERSION,
    audit_native_paths,
    environment,
    external_workspace,
    require_python,
    rid,
)

PBS_BUILD_ID = "20260623"
PBS_TAG_URL = "https://github.com/astral-sh/python-build-standalone/releases/tag/20260623"

# 各平台完整上游归档（含官方完整 PYTHON.json 与 licenses 目录）
FULL_ARCHIVE_URLS = {
    "osx-arm64": (
        "https://github.com/astral-sh/python-build-standalone/releases/download/20260623/"
        "cpython-3.12.13%2B20260623-aarch64-apple-darwin-pgo%2Blto-full.tar.zst"
    ),
    "linux-x64": (
        "https://github.com/astral-sh/python-build-standalone/releases/download/20260623/"
        "cpython-3.12.13%2B20260623-x86_64-unknown-linux-gnu-pgo%2Blto-full.tar.zst"
    ),
    "win-x64": (
        "https://github.com/astral-sh/python-build-standalone/releases/download/20260623/"
        "cpython-3.12.13%2B20260623-x86_64-pc-windows-msvc-pgo-full.tar.zst"
    ),
}


def run_cmd(command, *, cwd, env, check=True):
    """有界执行外部命令，严格保留退出码与输出。"""
    res = subprocess.run(
        list(map(str, command)),
        cwd=cwd,
        env=env,
        capture_output=True,
        timeout=600,
    )
    if check and res.returncode != 0:
        stdout_msg = res.stdout.decode(errors="replace")
        stderr_msg = res.stderr.decode(errors="replace")
        raise AssertionError(
            f"命令执行失败: {command}\n退出码: {res.returncode}\nSTDOUT:\n{stdout_msg}\nSTDERR:\n{stderr_msg}"
        )
    return res


def extract_tar_zst(archive_path, extract_dir, members):
    """先使用系统tar；回退解压工具须由构建环境显式安装，且启用安全成员过滤。"""
    extract_dir.mkdir(parents=True, exist_ok=True)
    try:
        result = subprocess.run(["tar", "-xf", str(archive_path), "-C", str(extract_dir), *members],
                                capture_output=True, timeout=600)
        if result.returncode == 0:
            return
    except FileNotFoundError:
        pass
    import tarfile
    try:
        import zstandard
    except ImportError as error:
        raise RuntimeError("tar缺少zstd支持，请显式准备构建工具zstandard==0.25.0") from error
    with archive_path.open("rb") as source:
        with zstandard.ZstdDecompressor().stream_reader(source) as reader:
            with tarfile.open(fileobj=reader, mode="r|") as archive:
                for member in archive:
                    if any(member.name == name or member.name.startswith(name + "/") for name in members):
                        archive.extract(member, extract_dir, filter="data")


def ensure_metadata(cache_dir, target_rid):
    """确保仓库外缓存中存在官方完整 PYTHON.json 与 licenses 目录。"""
    cache_dir = cache_dir / target_rid
    expected = cache_dir / "pbs-20260623-metadata" / "python"
    if (expected / "PYTHON.json").is_file() and (expected / "licenses").is_dir():
        return expected

    archive_url = FULL_ARCHIVE_URLS.get(target_rid)
    if not archive_url:
        raise ValueError(f"未知平台 RID: {target_rid}")

    cache_dir.mkdir(parents=True, exist_ok=True)
    archive_name = f"pbs-{target_rid}-full.tar.zst"
    archive_file = cache_dir / archive_name
    if not archive_file.is_file():
        download_tmp = cache_dir / (archive_name + ".download")
        print(f"[standalone] 正在下载官方完整发行元数据: {archive_url}")
        try:
            with urllib.request.urlopen(archive_url, timeout=60) as response, download_tmp.open("wb") as target:
                shutil.copyfileobj(response, target)
            download_tmp.replace(archive_file)
        finally:
            download_tmp.unlink(missing_ok=True)

    meta_target = cache_dir / "pbs-20260623-metadata"
    extract_tar_zst(archive_file, meta_target, ["python/PYTHON.json", "python/licenses"])
    if not ((expected / "PYTHON.json").is_file() and (expected / "licenses").is_dir()):
        raise RuntimeError(f"解压后未找到预期元数据: {expected}")
    return expected


def make_fixture_wheel(output_dir, name, ver, package_files):
    """构建手写 wheel 输入包，RECORD 仅保留路径，禁止普通文件 hash 校验。"""
    info_dir = f"{name}-{ver}.dist-info"
    files = dict(package_files)
    files[f"{info_dir}/METADATA"] = (
        f"Metadata-Version: 2.3\nName: {name}\nVersion: {ver}\n"
        "Summary: Standalone fixture package\nLicense: MIT\n"
    )
    files[f"{info_dir}/WHEEL"] = (
        "Wheel-Version: 1.0\nGenerator: ryupy-standalone\nRoot-Is-Purelib: true\nTag: py3-none-any\n"
    )
    files[f"{info_dir}/licenses/LICENSE"] = "MIT License fixture\n"

    record_io = io.StringIO()
    writer = csv.writer(record_io)
    for file_path in list(files.keys()) + [f"{info_dir}/RECORD"]:
        writer.writerow([file_path, "", ""])
    files[f"{info_dir}/RECORD"] = record_io.getvalue()

    wheel_path = output_dir / f"{name}-{ver}-py3-none-any.whl"
    with zipfile.ZipFile(wheel_path, "w") as zf:
        for fpath, fcontent in files.items():
            zf.writestr(fpath, fcontent)
    return wheel_path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--python", required=True, help="固定 CPython 3.12.13 可执行文件路径")
    parser.add_argument("--distribution", required=True, help="固定 CPython 安装根目录")
    parser.add_argument("--metadata", help="包含 PYTHON.json 与 licenses 的目录")
    parser.add_argument("--ryupy", help="ryupy CLI 可执行文件路径")
    parser.add_argument("--keep", action="store_true", help="失败或调试时保留测试临时目录")
    args = parser.parse_args()

    current_rid = rid()
    ryupy_bin = (
        Path(args.ryupy).resolve()
        if args.ryupy
        else (ROOT / "target/debug" / ("ryupy.exe" if sys.platform == "win32" else "ryupy"))
    )
    if not ryupy_bin.is_file():
        raise FileNotFoundError(f"未找到 ryupy CLI 可执行文件: {ryupy_bin}")

    env = environment()
    require_python(args.python, ROOT, env)

    cache_dir = Path.home() / ".cache/ryupy/cpython-provider"
    if args.metadata:
        metadata_python = Path(args.metadata).resolve()
    else:
        metadata_python = ensure_metadata(cache_dir, current_rid)

    workspace = external_workspace("ryupy-standalone-")
    print(f"[standalone] 启用隔离工作区: {workspace}")

    try:
        inputs_dir = workspace / "构建 输入"
        inputs_dir.mkdir(parents=True)

        # 1. 准备私有发行副本
        dist_copy = inputs_dir / "python"
        shutil.copytree(args.distribution, dist_copy, symlinks=False)
        for m_name in ["PYTHON.json", "licenses"]:
            src = metadata_python / m_name
            dst = dist_copy / m_name
            if src.is_dir():
                shutil.copytree(src, dst, dirs_exist_ok=True)
            else:
                shutil.copy2(src, dst)

        build_marker = dist_copy / "BUILD"
        assert build_marker.read_text(encoding="utf-8").strip() == PBS_BUILD_ID, "须使用固定PBS20260623基础发行"

        # 2. 准备虚拟环境并安装 packaging==25.0 与手写 fixture wheel
        venv_dir = inputs_dir / "虚拟 环境"
        run_cmd(["uv", "venv", "--python", args.python, str(venv_dir)], cwd=workspace, env=env)
        venv_python = venv_dir / ("Scripts/python.exe" if sys.platform == "win32" else "bin/python")
        run_cmd(["uv", "pip", "install", "--python", str(venv_python), "packaging==25.0"], cwd=workspace, env=env)

        fixture_src = ROOT / "tests/cpython/fixtures/standalone_pkg"
        pkg_init = (fixture_src / "__init__.py").read_text(encoding="utf-8")
        pkg_res = (fixture_src / "资源.txt").read_text(encoding="utf-8")
        wheel_path = make_fixture_wheel(
            inputs_dir,
            "standalone_pkg",
            "1.0.0",
            {
                "standalone_pkg/__init__.py": pkg_init,
                "standalone_pkg/资源.txt": pkg_res,
            },
        )
        run_cmd(["uv", "pip", "install", "--python", str(venv_python), "--no-deps", str(wheel_path)], cwd=workspace, env=env)

        # 3. 准备手写小应用源码与分发声明
        app_entry = inputs_dir / "项目 入口.py"
        app_entry.write_text((ROOT / "tests/cpython/standalone_app.py").read_text(encoding="utf-8"), encoding="utf-8")

        # 收集许可证清单
        licenses_list = []
        for candidate in ("lib/python3.12/LICENSE.txt", "Lib/LICENSE.txt", "LICENSE.txt"):
            if (dist_copy / candidate).is_file():
                licenses_list.append(candidate)
                break
        if not licenses_list:
            licenses_list.append("lib/python3.12/LICENSE.txt")
        for lic_file in sorted((dist_copy / "licenses").iterdir()):
            if lic_file.is_file():
                licenses_list.append(str(lic_file.relative_to(dist_copy)).replace("\\", "/"))

        decl_data = {
            "schema": 1,
            "python_version": VERSION,
            "distribution_root": str(dist_copy),
            "source": {
                "project": "python-build-standalone",
                "build_id": PBS_BUILD_ID,
                "source_url": PBS_TAG_URL,
                "licenses": licenses_list,
            },
            "container_transfer": "synchronized",
            "dependencies": {
                "environment": str(venv_dir),
                "requirements": ["standalone_pkg==1.0.0"],
            },
        }
        decl_file = inputs_dir / "provider.json"
        decl_file.write_text(json.dumps(decl_data, ensure_ascii=False, indent=2), encoding="utf-8")

        # 4. 同次 CPython 运行取得 oracle 基准
        oracle_cwd = workspace / "oracle"
        oracle_cwd.mkdir()
        oracle_res = run_cmd([str(venv_python), "-I", str(app_entry)], cwd=oracle_cwd, env=env)
        oracle_side_effect = (oracle_cwd / "业务 副作用.txt").read_bytes()
        print("[standalone] CPython oracle 基准执行完成，三流与副作用已捕获")

        # 5. 执行 ryupy build --aot 真实发布
        aot_out = workspace / "aot_build"
        build_cmd = [
            str(ryupy_bin),
            "--cpython-provider",
            str(decl_file),
            "build",
            str(app_entry),
            "--out-dir",
            str(aot_out),
            "--aot",
        ]
        print(f"[standalone] 执行 NativeAOT 构建: {' '.join(build_cmd)}")
        run_cmd(build_cmd, cwd=workspace, env=env)

        # 定位生成的发布目录 (.dist)
        configs = [p for p in aot_out.rglob("ryupy-cpython.json") if p.parent.name.endswith(".dist")]
        assert len(configs) == 1, f"未找到唯一发布目录产物: {configs}"
        product_dir = configs[0].parent

        report = json.loads((product_dir / "cpython-provider.json").read_text(encoding="utf-8"))
        dist_names = {d["name"] for d in report["dependency_collection"]["distributions"]}
        assert "standalone-pkg" in dist_names, f"依赖收集缺少 standalone-pkg: {dist_names}"

        # 6. 搬迁产物至中文与空格路径
        relocated_dir = workspace / "独立发布 目录 & 空格路径"
        shutil.move(str(product_dir), str(relocated_dir))
        shutil.rmtree(aot_out)

        # 7. 物理删除所有构建输入（基础发行、venv、项目源码、wheel），确保运行时完全隔离
        hidden_inputs = workspace / "不可访问 输入"
        inputs_dir.rename(hidden_inputs)
        shutil.rmtree(hidden_inputs)
        assert not inputs_dir.exists() and not hidden_inputs.exists(), "构建输入删除失败"
        print("[standalone] 构建输入已全部删除，进入清洁PATH搬迁验证")

        # 8. 在清洁 PATH 与受污染 PYTHONHOME/PYTHONPATH 环境下直接执行 NativeAOT 入口
        run_cwd = workspace / "run-aot"
        run_cwd.mkdir()
        binary_name = "项目 入口.exe" if sys.platform == "win32" else "项目 入口"
        binary_path = relocated_dir / binary_name
        assert binary_path.is_file(), f"未找到 NativeAOT 入口程序: {binary_path}"

        polluted_env = {k: v for k, v in env.items() if not k.startswith(("PYTHON", "DYLD_", "LD_"))}
        # PATH仅指向空目录；按绝对路径启动产品，运行臂无法按名称找到开发工具。
        empty_path = workspace / "empty-path"
        empty_path.mkdir()
        polluted_env = {key: value for key, value in polluted_env.items() if key.upper() != "PATH"}
        polluted_env["PATH"] = str(empty_path)
        if sys.platform == "win32":
            polluted_env["PYTHONHOME"] = "C:\\不可访问\\Python"
            polluted_env["PYTHONPATH"] = "C:\\不可访问\\包"
        else:
            polluted_env["PYTHONHOME"] = "/不可访问 Python"
            polluted_env["PYTHONPATH"] = "/不可访问 包"

        aot_res = run_cmd([str(binary_path)], cwd=run_cwd, env=polluted_env)
        assert aot_res.returncode == oracle_res.returncode == 0, f"退出码不匹配: {aot_res.returncode}"
        assert aot_res.stdout == oracle_res.stdout, f"STDOUT 不匹配:\n实际:\n{aot_res.stdout}\n期望:\n{oracle_res.stdout}"
        assert aot_res.stderr == oracle_res.stderr, f"STDERR 不匹配: {aot_res.stderr}"
        actual_side_effect = (run_cwd / "业务 副作用.txt").read_bytes()
        assert actual_side_effect == oracle_side_effect, "业务副作用文件内容与 oracle 不匹配"
        print("[standalone] NativeAOT 直接运行成功：标准库 sqlite3/ssl、中文默认 IO、第三方包与资源完全一致")

        # 9. 校验原生加载边
        audit_native_paths(relocated_dir)

        # 10. 缺失关键组件运行负例验证
        # 10.1 缺 libpython
        if sys.platform == "win32":
            py_lib = relocated_dir / "python/python312.dll"
        elif sys.platform == "darwin":
            py_lib = relocated_dir / "python/lib/libpython3.12.dylib"
        else:
            py_lib = relocated_dir / "python/lib/libpython3.12.so.1.0"
        assert py_lib.is_file(), f"未在产物中找到 libpython: {py_lib}"

        py_lib_bak = py_lib.with_suffix(".bak")
        py_lib.rename(py_lib_bak)
        try:
            bad_lib = run_cmd([str(binary_path)], cwd=run_cwd, env=polluted_env, check=False)
            assert bad_lib.returncode != 0 and b"CPython" in bad_lib.stderr, "缺少 libpython 时程序未按预期退出失败"
        finally:
            py_lib_bak.rename(py_lib)

        # 10.2 缺 shim
        if sys.platform == "win32":
            shim_lib = relocated_dir / "ryupy_cpython.dll"
        elif sys.platform == "darwin":
            shim_lib = relocated_dir / "libryupy_cpython.dylib"
        else:
            shim_lib = relocated_dir / "libryupy_cpython.so"
        assert shim_lib.is_file(), f"未在产物中找到 shim: {shim_lib}"

        shim_lib_bak = shim_lib.with_suffix(".bak")
        shim_lib.rename(shim_lib_bak)
        try:
            bad_shim = run_cmd([str(binary_path)], cwd=run_cwd, env=polluted_env, check=False)
            assert bad_shim.returncode != 0 and b"CPython" in bad_shim.stderr, "缺少 shim 共享库时程序未按预期退出失败"
        finally:
            shim_lib_bak.rename(shim_lib)

        # 10.3 缺包资源
        if sys.platform == "win32":
            res_file = relocated_dir / "python/Lib/site-packages/standalone_pkg/资源.txt"
        else:
            res_file = relocated_dir / "python/lib/python3.12/site-packages/standalone_pkg/资源.txt"
        assert res_file.is_file(), f"未在产物中找到第三方包资源文件: {res_file}"

        res_file_bak = res_file.with_suffix(".bak")
        res_file.rename(res_file_bak)
        try:
            bad_res = run_cmd([str(binary_path)], cwd=run_cwd, env=polluted_env, check=False)
            assert bad_res.returncode != 0 and b"FileNotFoundError" in bad_res.stderr, (
                f"缺少包资源时未报告 FileNotFoundError: 退出码={bad_res.returncode}, stderr={bad_res.stderr}"
            )
        finally:
            res_file_bak.rename(res_file)

        print("[standalone] 负例验证通过：缺 libpython、缺 shim、缺包资源均正确拒绝并报告错误")
        print("[standalone] 全流程独立发布验证完成！")

    finally:
        if not args.keep and workspace.exists():
            shutil.rmtree(workspace, ignore_errors=True)


if __name__ == "__main__":
    main()
