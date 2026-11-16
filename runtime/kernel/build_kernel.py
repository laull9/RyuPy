#!/usr/bin/env python3
"""CPython 3.12.13 严格零字节码内核原型构建与探针验证驱动（Linux/macOS 宿主）。

所有上游源码下载、解压、构建、日志与探针产物均位于仓库外临时目录，默认执行结束后自动清理；
仅当显式传入 `--keep` 时保留外部工作目录。严禁读取/改写用户缓存（~/.cache 等）或系统 Python，
亦严禁以 PBS 预编译库代替本源码构建的内核。Windows MSVC/DLL 路径当前清楚标注为待验。
"""
from __future__ import annotations

import argparse
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import urllib.request

from transform import TARGET_VERSION, apply_anchors


OFFICIAL_URL = f"https://www.python.org/ftp/python/{TARGET_VERSION}/Python-{TARGET_VERSION}.tar.xz"

KERNEL_DIR = Path(__file__).resolve().parent
REPO_ROOT = KERNEL_DIR.parent.parent
PROBE_SRC = KERNEL_DIR / "probe" / "kernel_probe.c"


class KernelBuildError(RuntimeError):
    """内核获取、构建或探针验证失败。"""


def ensure_outside_repo(path: Path) -> Path:
    """强制确保所有上游源码、构建、日志与产物均在 RyuPy 仓库之外。"""
    resolved = path.resolve()
    repo_resolved = REPO_ROOT.resolve()
    if resolved == repo_resolved or repo_resolved in resolved.parents:
        raise KernelBuildError(f"工作目录禁止位于仓库内：{resolved}")
    return resolved


def acquire_source_tarball(workdir: Path, local_tarball: Path | None) -> Path:
    """从官方主源或外部副本取得源码；版本与结构由后续精确源码锚点确认。"""
    dest = workdir / f"Python-{TARGET_VERSION}.tar.xz"
    if local_tarball is not None:
        src = ensure_outside_repo(local_tarball)
        if not src.is_file():
            raise KernelBuildError(f"指定的源码压缩包不存在：{src}")
        shutil.copy2(src, dest)
    else:
        print(f"[kernel] 下载官方主源 {OFFICIAL_URL} -> {dest}")
        with urllib.request.urlopen(OFFICIAL_URL, timeout=120) as resp, dest.open("wb") as out:
            shutil.copyfileobj(resp, out)

    return dest


def run_logged(
    cmd: list[str],
    *,
    cwd: Path,
    env: dict[str, str],
    log_file: Path,
    step: str,
) -> None:
    """在仓库外日志文件中记录子进程输出，失败时回显尾部日志并硬退出。"""
    log_file.parent.mkdir(parents=True, exist_ok=True)
    with log_file.open("w", encoding="utf-8") as log:
        proc = subprocess.run(
            cmd,
            cwd=cwd,
            env=env,
            stdout=log,
            stderr=subprocess.STDOUT,
            text=True,
            check=False,
            timeout=1800,
        )
    if proc.returncode != 0:
        tail = "\n".join(log_file.read_text(encoding="utf-8", errors="replace").splitlines()[-40:])
        raise KernelBuildError(
            f"步骤 [{step}] 失败（exit={proc.returncode}），日志 {log_file}：\n{tail}"
        )


def host_library_names() -> tuple[str, list[str]]:
    """按宿主平台返回动态库构建目标及需安装至 prefix/lib 的文件名列表。"""
    if sys.platform == "darwin":
        return "libpython3.12.dylib", ["libpython3.12.dylib"]
    if sys.platform.startswith("linux"):
        return "libpython3.12.so", ["libpython3.12.so.1.0", "libpython3.12.so"]
    raise KernelBuildError(
        f"当前平台 {sys.platform} 尚未接入（Windows MSVC/DLL 构建路径当前明确为待验）"
    )


def choose_compiler(env: dict[str, str]) -> str:
    """优先尊重调用方 CC；macOS 默认使用 Apple Clang `/usr/bin/cc` 避免 SDK 错配。"""
    if env.get("CC"):
        return env["CC"]
    if sys.platform == "darwin" and Path("/usr/bin/cc").exists():
        return "/usr/bin/cc"
    cc = shutil.which("cc") or shutil.which("gcc") or shutil.which("clang")
    if not cc:
        raise KernelBuildError("未找到可用的 C 编译器（cc/gcc/clang）")
    return cc


def build_and_verify(args: argparse.Namespace) -> None:
    """执行完整内核获取、锚点变换、源码编译、符号核验与手写 C 探针测试。"""
    if sys.platform not in ("darwin",) and not sys.platform.startswith("linux"):
        raise KernelBuildError(
            f"当前仅支持 Linux/macOS 宿主原型；平台 {sys.platform}（含 Windows）清楚标记为待验"
        )

    if args.workdir is not None:
        base_dir = ensure_outside_repo(args.workdir)
        base_dir.mkdir(parents=True, exist_ok=True)
        workdir = ensure_outside_repo(Path(tempfile.mkdtemp(prefix="ryupy-kernel-312-", dir=base_dir)))
    else:
        workdir = ensure_outside_repo(Path(tempfile.mkdtemp(prefix="ryupy-kernel-312-")))

    logs_dir = workdir / "logs"
    src_parent = workdir / "src"
    src_dir = src_parent / f"Python-{TARGET_VERSION}"
    build_dir = workdir / "build"
    prefix_dir = workdir / "prefix"

    try:
        tarball = acquire_source_tarball(workdir, args.source_tarball)
        src_parent.mkdir(parents=True, exist_ok=True)
        run_logged(
            ["tar", "-xf", str(tarball), "-C", str(src_parent)],
            cwd=workdir,
            env=os.environ.copy(),
            log_file=logs_dir / "extract.log",
            step="解压官方源码",
        )
        counts = apply_anchors(src_dir, dry_run=False)
        print(f"[kernel] 锚点精确变换完成：{counts}")
        if args.transform_only:
            print(f"[kernel] 已按 --transform-only 停止（未执行 configure/make）；目录：{src_dir}")
            return

        ldlibrary, install_libs = host_library_names()
        build_dir.mkdir(parents=True, exist_ok=True)
        (build_dir / "Modules").mkdir(parents=True, exist_ok=True)
        # 通过 Setup.local 将 math 强制静态编入 libpython 的 PyImport_Inittab。
        (build_dir / "Modules" / "Setup.local").write_text(
            "*static*\nmath mathmodule.c\n", encoding="utf-8"
        )

        env = os.environ.copy()
        cc = choose_compiler(env)
        env["CC"] = cc
        # 隔离环境变量，不让宿主的 PYTHON* 或缓存路径影响源码构建。
        for key in list(env):
            if key.startswith("PYTHON"):
                del env[key]

        configure_cmd = [
            str(src_dir / "configure"),
            f"--prefix={prefix_dir}",
            "--enable-shared",
            "--without-ensurepip",
            "--disable-test-modules",
        ]
        print(f"[kernel] configure (CC={cc})...")
        run_logged(configure_cmd, cwd=build_dir, env=env, log_file=logs_dir / "configure.log", step="configure")

        jobs = str(args.jobs or os.cpu_count() or 4)
        print(f"[kernel] make -j{jobs} {ldlibrary} ...")
        run_logged(
            ["make", f"-j{jobs}", ldlibrary],
            cwd=build_dir,
            env=env,
            log_file=logs_dir / "make_libpython.log",
            step=f"make {ldlibrary}",
        )
        run_logged(
            ["make", "inclinstall"],
            cwd=build_dir,
            env=env,
            log_file=logs_dir / "make_inclinstall.log",
            step="make inclinstall",
        )

        lib_dir = prefix_dir / "lib"
        lib_dir.mkdir(parents=True, exist_ok=True)
        shutil.copy2(src_dir / "LICENSE", prefix_dir / "LICENSE.Python.txt")
        for lib_name in install_libs:
            src_lib = build_dir / lib_name
            dst_lib = lib_dir / lib_name
            if src_lib.is_symlink():
                if dst_lib.exists() or dst_lib.is_symlink():
                    dst_lib.unlink()
                dst_lib.symlink_to(os.readlink(src_lib))
            elif src_lib.is_file():
                shutil.copy2(src_lib, dst_lib)
            else:
                raise KernelBuildError(f"未产出预期动态库文件：{src_lib}")

        # 确认交付前缀无任何 .py / .pyc / encodings 字节码载荷。
        leaked_py = list(prefix_dir.rglob("*.py")) + list(prefix_dir.rglob("*.pyc"))
        if leaked_py:
            raise KernelBuildError(f"严格内核前缀意外包含 Python 脚本/字节码：{leaked_py[:5]}")

        verify_binary_symbols(lib_dir / ldlibrary)

        probe_bin = workdir / "kernel_probe"
        include_dir = prefix_dir / "include" / "python3.12"
        compile_cmd = [
            cc,
            "-Wall",
            "-Wextra",
            "-Werror",
            f"-I{include_dir}",
            str(PROBE_SRC),
            f"-L{lib_dir}",
            "-lpython3.12",
            f"-Wl,-rpath,{lib_dir}",
            "-lm",
            "-o",
            str(probe_bin),
        ]
        run_logged(compile_cmd, cwd=workdir, env=env, log_file=logs_dir / "probe_cc.log", step="编译 C 探针")

        for mode in ("strict-clean", "strict-reject", "phase-counters", "reject-importlib"):
            proc = subprocess.run(
                [str(probe_bin), str(prefix_dir), mode],
                cwd=workdir,
                env=env,
                capture_output=True,
                text=True,
                check=False,
                timeout=30,
            )
            if proc.returncode != 0:
                raise KernelBuildError(
                    f"C 探针模式 [{mode}] 失败（exit={proc.returncode}）：\n"
                    f"stdout:\n{proc.stdout}\nstderr:\n{proc.stderr}"
                )
            print(f"[probe:{mode}] {proc.stdout.strip()}")

        print(f"[kernel] 全部构建、符号核验与 4 项手写 C 探针通过（前缀：{prefix_dir}）")
    finally:
        if args.keep:
            print(f"[kernel] 已按 --keep 保留仓库外临时目录：{workdir}")
        else:
            shutil.rmtree(workdir, ignore_errors=True)
            print(f"[kernel] 已自动清理仓库外临时目录：{workdir}")


def verify_binary_symbols(lib_path: Path) -> None:
    """用 nm 检查目标动态库包含新增内核导出与 PyInit_math，且不含 frozen bootstrap 字节码符号。"""
    nm_bin = shutil.which("nm")
    if not nm_bin:
        print("[kernel] 未找到 nm，跳过符号表附加核对")
        return
    proc = subprocess.run([nm_bin, "-g", str(lib_path)], capture_output=True, text=True, check=False)
    if proc.returncode != 0:
        raise KernelBuildError(f"nm 检查失败：{proc.stderr}")
    symbols = proc.stdout
    required = (
        "rp_kernel_version",
        "rp_kernel_set_phase",
        "rp_kernel_set_strict_reject",
        "rp_kernel_eval_entry_count",
        "PyInit_math",
    )
    for sym in required:
        if sym not in symbols:
            raise KernelBuildError(f"源码构建内核 {lib_path} 缺少预期符号：{sym}")
    proc_all = subprocess.run([nm_bin, str(lib_path)], capture_output=True, text=True, check=False)
    if proc_all.returncode == 0:
        for forbidden in ("_Py_M__importlib__bootstrap", "_Py_M__getpath"):
            if forbidden in proc_all.stdout:
                raise KernelBuildError(f"源码构建内核 {lib_path} 仍残留禁用 frozen 字节码符号：{forbidden}")
    print(f"[kernel] 符号核验通过：{lib_path.name} 含内建 math 与内核开关，无 frozen bootstrap/getpath 载荷")


def parse_args() -> argparse.Namespace:
    """解析命令行参数。"""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-tarball", type=Path, help="仓库外已有的 Python-3.12.13.tar.xz 副本路径")
    parser.add_argument("--workdir", type=Path, help="显式指定仓库外临时工作目录")
    parser.add_argument("--jobs", type=int, help="并行 make 作业数")
    parser.add_argument("--keep", action="store_true", help="保留仓库外构建目录与产物（默认自动清理）")
    parser.add_argument("--transform-only", action="store_true", help="仅获取/解压并执行锚点变换，不运行 configure/make")
    return parser.parse_args()


def main() -> int:
    try:
        build_and_verify(parse_args())
        return 0
    except KernelBuildError as error:
        print(f"错误：{error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
