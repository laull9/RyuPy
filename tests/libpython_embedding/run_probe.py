#!/usr/bin/env python3
"""编译并运行原版 libpython 胶水探针；结果只输出到终端。

查询、编译、执行均有超时，构建产物位于仓库外并自动清理。
逐帧观测与计时分进程；本工具的局部小样不作为产品性能门。
"""

import argparse
import json
import platform
import re
import shutil
import statistics
import subprocess
import sys
import tempfile
from pathlib import Path


QUERY = """
import json, platform, sys, sysconfig
print(json.dumps({
    "home": sys.base_prefix,
    "include": sysconfig.get_path("include"),
    "lib": sysconfig.get_config_var("LIBDIR"),
    "library": sysconfig.get_config_var("LDLIBRARY"),
    "version": list(sys.version_info[:2]),
    "implementation": sys.implementation.name,
    "platform": sys.platform,
    "machine": platform.machine(),
    "free_threaded": bool(sysconfig.get_config_var("Py_GIL_DISABLED")),
}))
"""


def run(command, *, label, timeout, cwd):
    """失败时保留 stdout/stderr；超时也输出已经收到的内容。"""
    try:
        result = subprocess.run(
            command,
            cwd=cwd,
            text=True,
            capture_output=True,
            timeout=timeout,
            check=False,
        )
    except subprocess.TimeoutExpired as error:
        print(f"FAIL {label}: 超过 {timeout} 秒", file=sys.stderr)
        for content in (error.stdout, error.stderr):
            if content:
                if isinstance(content, bytes):
                    content = content.decode(errors="replace")
                print(content, file=sys.stderr)
        raise SystemExit(1) from error
    if result.stderr:
        print(f"STDERR {label}:\n{result.stderr}", file=sys.stderr)
    if result.returncode:
        print(f"FAIL {label}: exit={result.returncode}", file=sys.stderr)
        print(result.stdout, file=sys.stderr)
        raise SystemExit(1)
    return result.stdout


def validate_output(output, *, profile):
    expected = f"RESULT failures=0 finalize=0 profile={int(profile)}"
    if expected not in output:
        raise ValueError(f"探针缺少成功终态：{expected}\n{output}")
    observation = re.search(
        r"OBSERVATION total=(\d+) overflow=0 truncation=0 lookup_failure=0", output
    )
    if not observation:
        raise ValueError(f"逐帧观测不完整\n{output}")
    if profile and int(observation.group(1)) == 0:
        raise ValueError(f"codec 正向操作没有观测到帧\n{output}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--python", required=True, type=Path)
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--iterations", type=int, default=10000)
    parser.add_argument("--repeats", type=int, default=4)
    args = parser.parse_args()
    if args.iterations <= 0 or args.repeats <= 0 or args.repeats % 2:
        parser.error("iterations 必须为正；repeats 必须是正偶数以平衡次序")
    # 首切只开放真实验收过的平台与 minor；其它平台独立补验证后再扩展。
    if sys.platform != "darwin" or platform.machine() != "arm64":
        parser.error("当前探针只验收 macOS arm64")
    compiler = shutil.which("clang")
    if not compiler:
        parser.error("需要本机 clang")
    repo = args.repo.resolve()
    include = repo / "runtime/RyuPy.CPython/native"
    if not (include / "import_hooks.h").is_file():
        parser.error("repo 中缺少产品 native/import_hooks.h")
    interpreter = args.python.resolve()
    # 即使调用者把 TMPDIR 放进仓库，也不向仓库生成编译或运行产物。
    temporary_parent = Path(tempfile.gettempdir()).resolve()
    if temporary_parent == repo or repo in temporary_parent.parents:
        temporary_parent = repo.parent
    with tempfile.TemporaryDirectory(prefix="ryupy-stock-glue-", dir=temporary_parent) as temp:
        output_dir = Path(temp)
        query = run(
            [str(interpreter), "-I", "-B", "-c", QUERY],
            label="query", timeout=10, cwd=output_dir,
        )
        config = json.loads(query)
        if (config["implementation"] != "cpython"
                or config["version"] not in ([3, 12], [3, 13])
                or config["platform"] != "darwin"
                or config["machine"] != "arm64"
                or config["free_threaded"]):
            parser.error("只支持已验证的 macOS arm64、常规 GIL CPython 3.12/3.13")
        if not config["lib"] or not config["library"]:
            parser.error("解释器未提供共享 libpython 路径")
        library = Path(config["lib"]) / config["library"]
        if library.suffix != ".dylib" or not library.is_file():
            parser.error("解释器必须提供已安装的共享 libpython dylib")
        executable = output_dir / "probe"
        source = Path(__file__).with_name("stock_glue_probe.c")
        run(
            [compiler, "-g", "-O2", "-std=c11", f'-I{config["include"]}',
             f"-I{include}", str(source), str(library),
             f'-Wl,-rpath,{config["lib"]}', "-o", str(executable)],
            label="compile", timeout=60, cwd=output_dir,
        )
        for mode in ("native", "a2"):
            output = run(
                [str(executable), config["home"], mode, "1", "1"],
                label=f"profile {mode}", timeout=20, cwd=output_dir,
            )
            validate_output(output, profile=True)
            print(f"PROFILE {mode}\n{output}")
        samples = {}
        for index in range(args.repeats):
            order = ("native", "a2") if index % 2 == 0 else ("a2", "native")
            for mode in order:
                output = run(
                    [str(executable), config["home"], mode, str(args.iterations), "0"],
                    label=f"timing {index} {mode}", timeout=20, cwd=output_dir,
                )
                validate_output(output, profile=False)
                timings = re.findall(r"TIME ([a-z_]+)(?: n=\d+)? seconds=([0-9.]+)", output)
                if {label for label, _ in timings} != {
                    "startup", "cold_import", "cached_plain", "cached_fromlist"
                }:
                    raise ValueError(f"计时结果缺失\n{output}")
                for label, seconds in timings:
                    samples.setdefault((mode, label), []).append(float(seconds))
        for (mode, label), values in sorted(samples.items()):
            print(f"MEDIAN {mode} {label} seconds={statistics.median(values):.9f} samples={values}")
    print("CLEANUP 仓库外临时构建目录已清理")


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, KeyError) as error:
        print(f"FAIL {error}", file=sys.stderr)
        raise SystemExit(1) from error
