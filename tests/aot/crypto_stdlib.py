#!/usr/bin/env python3
"""四个二进制/密码标准库的串行 NativeAOT 发布与搬迁；只比较执行结果，不做文件摘要校验。"""
import argparse
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys


def capture(command, log, *, cwd=None, env=None, timeout=600):
    result = subprocess.run(command, cwd=cwd, env=env, capture_output=True, timeout=timeout)
    log.with_suffix(".stdout").write_bytes(result.stdout)
    log.with_suffix(".stderr").write_bytes(result.stderr)
    log.with_suffix(".exit").write_text(str(result.returncode) + "\n", encoding="utf-8")
    return result


def same_execution(expected, actual, name):
    if (expected.returncode, expected.stdout, expected.stderr) != (actual.returncode, actual.stdout, actual.stderr):
        raise AssertionError(f"{name}：退出码或双流与固定 CPython 不一致，查看本轮日志")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ryupy", required=True, type=Path)
    parser.add_argument("--python", default=os.environ.get("PYTHON", sys.executable))
    parser.add_argument("--out", required=True, type=Path)
    args = parser.parse_args()
    compiler = args.ryupy.resolve(strict=True)
    root = args.out.resolve()
    root.mkdir(parents=True, exist_ok=False)
    repository = Path(__file__).resolve().parents[2]
    pinned = (repository / "tests/pinned-python.txt").read_text().strip()
    version = subprocess.check_output([args.python, "-c", "import platform; print(platform.python_version())"], text=True).strip()
    if version != pinned:
        raise AssertionError(f"需要 CPython {pinned}，当前 {version}")
    metadata = {"platform": platform.platform(), "machine": platform.machine(), "python": version,
                "sdk": subprocess.check_output(["dotnet", "--version"], text=True).strip(), "cases": []}
    for stem in ["stdlib_binary_codecs", "stdlib_binary_edges", "stdlib_binary_callbacks", "stdlib_crypto_hashes"]:
        source_root = root / (stem + "-source")
        source_root.mkdir()
        source = source_root / (stem + ".py")
        shutil.copy2(repository / "tests/e2e/phase3" / source.name, source)
        baseline = capture([args.python, "-B", str(source)], root / (stem + "-cpython"))
        if baseline.returncode != 0:
            raise AssertionError(f"{stem} 的 CPython 基线失败")
        output = source_root / "output"
        publication = capture([str(compiler), "build", str(source), "--aot", "-o", str(output)], root / (stem + "-publish"))
        if publication.returncode != 0:
            raise AssertionError(f"{stem} NativeAOT 发布失败")
        generated = [Path(line.removeprefix("已生成 ")) for line in publication.stderr.decode().splitlines()
                     if line.startswith("已生成 ") and not line.endswith(".dll")]
        if len(generated) != 1:
            raise AssertionError(f"{stem} CLI 未报告唯一原生入口")
        native = generated[0].resolve(strict=True)
        if native.parent != output / (stem + ".dist"):
            raise AssertionError(f"{stem} 原生入口超出本轮发布目录")
        direct = capture([str(native)], root / (stem + "-native"), cwd=source_root)
        same_execution(baseline, direct, stem + " 原目录")
        relocated = root / (stem + "-relocated")
        shutil.copytree(native.parent, relocated)
        executable = relocated / native.name
        shutil.rmtree(source_root)
        home = root / (stem + "-home")
        home.mkdir()
        clean = {"PATH": "", "HOME": str(home), "TMPDIR": str(home), "LANG": "C.UTF-8",
                 "DOTNET_ROOT": str(home / "no-sdk"), "RYUPY_RUNTIME_DIR": str(home / "no-runtime")}
        if os.name == "nt":
            clean["SystemRoot"] = os.environ["SystemRoot"]
        moved = capture([str(executable)], root / (stem + "-moved"), cwd=home, env=clean, timeout=60)
        same_execution(baseline, moved, stem + " 搬迁")
        metadata["cases"].append({"name": stem, "exit": moved.returncode, "stdout_bytes": len(moved.stdout),
                                  "source_and_build_removed": not source_root.exists(), "relocated": str(relocated)})
        (root / "result.json").write_text(json.dumps(metadata, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"{stem}: CPython == NativeAOT == 搬迁结果", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
