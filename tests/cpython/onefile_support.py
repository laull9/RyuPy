"""OneFile AOT 单文件真实验收支持模块。

负责工作区生命周期、私有 CPython 发行与 provider.json 声明准备、
无哈希手写 wheel 构建、被污染环境构造、单文件精确产物审计、
进程并发执行与负例数据损坏注入等支撑能力。
严格遵守规范：每文件<=600行、无普通文件hash校验、外部清理。
"""
from __future__ import annotations

import csv
import io
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys
import tempfile
import urllib.request
import zipfile

# 保持无字节码写入
sys.dont_write_bytecode = True

ROOT = Path(__file__).resolve().parents[2]
PBS_BUILD_ID = "20260623"
PBS_TAG_URL = "https://github.com/astral-sh/python-build-standalone/releases/tag/20260623"

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


def rid() -> str:
    """获取当前系统架构对应的 RID 标识。"""
    system = {"darwin": "osx", "linux": "linux", "win32": "win"}[sys.platform]
    machine = platform.machine().lower()
    assert machine in ("arm64", "aarch64", "x86_64", "amd64"), f"不支持的架构: {machine}"
    arch = "arm64" if machine in ("arm64", "aarch64") else "x64"
    return f"{system}-{arch}"


def external_workspace(prefix: str, parent: Path | None = None) -> Path:
    """创建位于仓库外的临时工作区，严格遵循 ADR-0182。"""
    base_dir = Path(parent or tempfile.gettempdir()).resolve()
    assert not base_dir.is_relative_to(ROOT), "测试临时目录禁止位于源码仓库内"
    base_dir.mkdir(parents=True, exist_ok=True)
    return Path(tempfile.mkdtemp(prefix=prefix, dir=base_dir))


def execute_cmd(command: list[str | Path], *, cwd: Path, env: dict[str, str], timeout: int = 300) -> subprocess.CompletedProcess:
    """有界执行外部命令并返回结果。"""
    return subprocess.run(
        [str(c) for c in command],
        cwd=cwd,
        env=env,
        capture_output=True,
        timeout=timeout,
    )


def checked_run(command: list[str | Path], *, cwd: Path, env: dict[str, str], label: str = "命令") -> subprocess.CompletedProcess:
    """执行命令并严格校验退出码为 0。"""
    res = execute_cmd(command, cwd=cwd, env=env)
    if res.returncode != 0:
        stdout_txt = res.stdout.decode("utf-8", errors="replace")
        stderr_txt = res.stderr.decode("utf-8", errors="replace")
        raise AssertionError(
            f"[{label}] 执行失败 (code={res.returncode}):\n"
            f"命令行: {' '.join(str(c) for c in command)}\n"
            f"--- STDOUT ---\n{stdout_txt}\n"
            f"--- STDERR ---\n{stderr_txt}\n"
        )
    return res


def make_clean_env() -> dict[str, str]:
    """构建消除了本地特定 Python 及动态链接器环境变量的基础环境。"""
    clean_env = {
        k: v for k, v in os.environ.items()
        if not k.startswith(("PYTHON", "RYUPY_", "LD_", "DYLD_"))
    }
    clean_env.update({
        "PYTHONDONTWRITEBYTECODE": "1",
        "PYTHONHASHSEED": "0",
        "LC_ALL": "C.UTF-8",
        "LANG": "C.UTF-8",
        "TZ": "UTC",
    })
    return clean_env


def make_polluted_env(empty_path_dir: Path) -> dict[str, str]:
    """构建清洁 PATH（指向空目录）及受污染 PYTHONHOME/PYTHONPATH 的极端环境。"""
    env = make_clean_env()
    env = {k: v for k, v in env.items() if k.upper() != "PATH"}
    env["PATH"] = str(empty_path_dir)

    if sys.platform == "win32":
        env["PYTHONHOME"] = "C:\\污染环境\\不存在的Python"
        env["PYTHONPATH"] = "C:\\污染环境\\虚假模块包"
    else:
        env["PYTHONHOME"] = "/污染环境/不存在的Python"
        env["PYTHONPATH"] = "/污染环境/虚假模块包"
    return env


def build_fixture_wheel(output_dir: Path, name: str, version: str, package_files: dict[str, str | bytes]) -> Path:
    """构建手写 wheel 输入包，RECORD 仅保留文件相对路径，禁止写入普通文件 hash。"""
    info_dir = f"{name}-{version}.dist-info"
    files: dict[str, str | bytes] = dict(package_files)
    files[f"{info_dir}/METADATA"] = (
        f"Metadata-Version: 2.3\n"
        f"Name: {name}\n"
        f"Version: {version}\n"
        f"Summary: RyuPy OneFile fixture package\n"
        f"License: MIT\n"
    )
    files[f"{info_dir}/WHEEL"] = (
        "Wheel-Version: 1.0\n"
        "Generator: ryupy-onefile-test\n"
        "Root-Is-Purelib: true\n"
        "Tag: py3-none-any\n"
    )
    files[f"{info_dir}/licenses/LICENSE"] = "MIT License fixture\n"

    record_buffer = io.StringIO()
    writer = csv.writer(record_buffer)
    for p in list(files.keys()) + [f"{info_dir}/RECORD"]:
        writer.writerow([p, "", ""])
    files[f"{info_dir}/RECORD"] = record_buffer.getvalue()

    wheel_filename = f"{name}-{version}-py3-none-any.whl"
    wheel_path = output_dir / wheel_filename
    with zipfile.ZipFile(wheel_path, "w") as zf:
        for fpath, content in files.items():
            if isinstance(content, str):
                zf.writestr(fpath, content.encode("utf-8"))
            else:
                zf.writestr(fpath, content)
    return wheel_path


def ensure_cpython_metadata(cache_dir: Path, target_rid: str) -> Path:
    """复用固定发行的按RID隔离缓存、安全解包及zstd回退，不跨平台混用旧缓存。"""
    from standalone_run import ensure_metadata
    return ensure_metadata(cache_dir, target_rid)


def prepare_cpython_provider_decl(inputs_dir: Path, dist_root: Path, metadata_python: Path,
                                  venv_dir: Path, reqs: list[str]) -> Path:
    """准备私有 CPython 发行副本、桥与 provider.json 声明。"""
    dist_copy = inputs_dir / "python"
    shutil.copytree(dist_root, dist_copy, symlinks=False)
    for m in ["PYTHON.json", "licenses"]:
        src = metadata_python / m
        dst = dist_copy / m
        if src.is_dir():
            shutil.copytree(src, dst, dirs_exist_ok=True)
        else:
            shutil.copy2(src, dst)

    current_rid = rid()
    shim_name = {"win-x64": "ryupy_cpython.dll", "osx-arm64": "libryupy_cpython.dylib",
                 "linux-x64": "libryupy_cpython.so"}[current_rid]
    bridge_src = ROOT / "runtime/RyuPy.CPython/bin/Release/net10.0"
    bridge_dir = inputs_dir / "bridge"
    shutil.copytree(bridge_src, bridge_dir, symlinks=False)
    assert (bridge_dir / "RyuPy.CPython.dll").is_file(), bridge_dir
    assert (bridge_dir / shim_name).is_file(), bridge_dir

    licenses_list = []
    for cand in ("lib/python3.12/LICENSE.txt", "Lib/LICENSE.txt", "LICENSE.txt"):
        if (dist_copy / cand).is_file():
            licenses_list.append(cand)
            break
    if not licenses_list:
        licenses_list.append("lib/python3.12/LICENSE.txt")
    for lic in sorted((dist_copy / "licenses").iterdir()):
        if lic.is_file():
            licenses_list.append(str(lic.relative_to(dist_copy)).replace("\\", "/"))

    decl = {
        "schema": 1,
        "python_version": "3.12.13",
        "distribution_root": str(dist_copy),
        "source": {
            "project": "python-build-standalone",
            "build_id": PBS_BUILD_ID,
            "source_url": PBS_TAG_URL,
            "licenses": licenses_list,
        },
        "container_transfer": "synchronized",
        "bridge_directory": str(bridge_dir),
        "shim": str(bridge_dir / shim_name),
        "dependencies": {
            "environment": str(venv_dir),
            "requirements": reqs,
        },
    }
    decl_file = inputs_dir / "provider.json"
    decl_file.write_text(json.dumps(decl, ensure_ascii=False, indent=2), encoding="utf-8")
    return decl_file


def audit_onefile_output(build_out: Path, stem: str) -> Path:
    """按精确约定核验 .onefile 目录：整个目录必须有且仅有唯一可执行文件。"""
    onefile_dir = build_out / f"{stem}.onefile"
    assert onefile_dir.is_dir(), f"单文件输出目录不存在: {onefile_dir}"

    entries = list(onefile_dir.iterdir())
    assert len(entries) == 1, (
        f"单文件目录包含多余文件！期望唯一可执行档，实际文件数 {len(entries)}: {[e.name for e in entries]}"
    )
    single_exe = entries[0]
    expected_name = f"{stem}.exe" if sys.platform == "win32" else stem
    assert single_exe.name == expected_name, (
        f"单文件名称不匹配：期望 {expected_name}，实际 {single_exe.name}"
    )
    assert single_exe.is_file() and single_exe.stat().st_size > 0, f"单文件可执行档无效: {single_exe}"
    return single_exe


def run_concurrent_processes(executable: Path, count: int = 2, args: list[str] | None = None,
                           cwds: list[Path] | None = None, envs: list[dict[str, str]] | None = None,
                           timeout: int = 60) -> list[subprocess.CompletedProcess]:
    """同时并发拉起多个单文件执行实例，验证并发自解压/运行时缓存互斥锁不发生死锁或写入冲突。"""
    args = args or []
    processes: list[subprocess.Popen] = []
    try:
        for idx in range(count):
            cwd = cwds[idx] if cwds and idx < len(cwds) else None
            env = envs[idx] if envs and idx < len(envs) else None
            p = subprocess.Popen(
                [str(executable), *args],
                cwd=str(cwd) if cwd else None,
                env=env,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
            )
            processes.append(p)

        results = []
        for p in processes:
            stdout, stderr = p.communicate(timeout=timeout)
            results.append(subprocess.CompletedProcess(
                args=p.args,
                returncode=p.returncode,
                stdout=stdout,
                stderr=stderr,
            ))
        return results
    finally:
        for p in processes:
            if p.poll() is None:
                p.kill()


def create_corrupted_executable(source_exe: Path, target_exe: Path) -> Path:
    """破坏原生文件头，跨平台稳定验证OS拒绝；压缩流破坏另由解析器源码测试覆盖。"""
    data = source_exe.read_bytes()
    assert len(data) > 1024, "可执行文件体量异常，无法制作负例样本"
    target_exe.write_bytes(bytes(16) + data[16:])
    if sys.platform != "win32":
        target_exe.chmod(source_exe.stat().st_mode)
    return target_exe


def verify_interruption(executable: Path, cwd: Path, env: dict[str, str], temp: Path) -> None:
    """等业务进程明确就绪后中断外层，断言子进程退出且私有解包目录清空。"""
    import queue
    import signal
    import threading
    choices = [signal.SIGINT, signal.SIGTERM, signal.SIGHUP] if sys.platform != "win32" else [signal.CTRL_BREAK_EVENT]
    for number in choices:
        arm = cwd / f"signal-{number}"
        arm.mkdir()
        options = {"creationflags": subprocess.CREATE_NEW_PROCESS_GROUP} if sys.platform == "win32" else {}
        process = subprocess.Popen([str(executable), "--hold"], cwd=arm, env=env,
                                   stdout=subprocess.PIPE, stderr=subprocess.PIPE, **options)
        ready = queue.Queue()
        def read_ready():
            for line in iter(process.stdout.readline, b""):
                if line.strip() == b"[app:hold]":
                    ready.put(True)
                    return
            ready.put(False)
        reader = threading.Thread(target=read_ready, daemon=True)
        reader.start()
        try:
            assert ready.get(timeout=120), "中断臂未进入业务等待状态"
            reader.join(timeout=5)
            process.send_signal(number)
            _, stderr = process.communicate(timeout=45)
            if sys.platform == "win32":
                assert process.returncode not in (0, 125), (process.returncode, stderr)
            else:
                assert process.returncode == 128 + number, (process.returncode, stderr)
            assert not list(temp.iterdir()), f"中断后遗留解包目录：{list(temp.iterdir())}"
        finally:
            if process.poll() is None:
                process.kill()
                process.wait(timeout=10)
            reader.join(timeout=5)
    print("[onefile] 中断转发、子进程退出与独占目录清理通过")
