#!/usr/bin/env python3
"""固定五源码/七配置的 AOT 裁剪语义门禁；时长仅构建观测，不是性能结论。"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import platform
import shutil
import tempfile
import time

from smoke import Runner, executable, published_native


# 保留原文件全部期望，不按结果动态缩减配置或替换成更小的输入。
CASES = [
    ("bigint", "phase2/m2_1_bigint.py", []),
    ("static-dataclass", "phase1/m1_5_dataclass.py", []),
    ("dynamic-dataclass", "phase3/m3_3_dataclass_protocol.py", []),
    ("numeric-list", "phase3/m3_10_numeric_list_island.py", []),
    ("slp-default", "phase3/m3_10_float_slp.py", []),
    ("slp-packed", "phase3/m3_10_float_slp.py", ["--experimental-packed-slp"]),
    ("slp-scalar", "phase3/m3_10_float_slp.py", ["--diagnostic-scalar-slp"]),
]


def tree_bytes(root):
    """统计真实普通文件逻辑大小，不沿符号链接访问外部目录。"""
    return sum(path.stat().st_size for path in root.rglob("*")
               if path.is_file() and not path.is_symlink())


def trim_build_cache(output, native):
    """仅成功配置自有 host/bin、host/obj 可删，项目/源码/IL/native/日志全保。"""
    host = native.parent.parent / "host"
    removed = []
    for name in ("bin", "obj"):
        cache = host / name
        if cache.is_symlink():
            raise AssertionError(f"不清理符号链接构建缓存：{cache}")
        if cache.exists():
            cache.resolve().relative_to(output.resolve())
            removed.append({"path": str(cache), "bytes": tree_bytes(cache)})
            shutil.rmtree(cache)
    return removed


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--python", default="python3.12")
    parser.add_argument("--dotnet", default=os.environ.get("DOTNET", "dotnet"))
    parser.add_argument("--ryupy", default="target/debug/ryupy")
    args = parser.parse_args()
    if platform.system() != "Linux" or platform.machine() != "x86_64":
        raise RuntimeError("本门禁仅支持真实 Linux x86_64")
    repo = Path(__file__).resolve().parents[2]
    python, dotnet, ryupy = map(executable, [args.python, args.dotnet, args.ryupy])
    args.out.mkdir(parents=True, exist_ok=True)
    work = Path(tempfile.mkdtemp(prefix="aot-coverage-", dir=args.out.resolve()))
    logs = work / "logs"
    logs.mkdir()
    run = Runner(logs)
    started = time.monotonic()
    deadline = started + 1200
    records = []
    status = "failed"
    print(f"固定七配置 AOT 日志：{work}", flush=True)

    def timed(label, command, *, cwd, env=None, check=False, limit=60):
        remaining = deadline - time.monotonic()
        if remaining <= 0:
            raise TimeoutError("整组 20 分钟预算已耗尽；不删减未跑配置")
        return run.run(label, command, cwd=cwd, env=env, check=check,
                       timeout=min(limit, remaining))

    try:
        version = timed("python-version", [python, "-c",
                        "import sys; print(sys.implementation.name); print('.'.join(map(str,sys.version_info[:3])))"],
                        cwd=work, check=True)
        if version.stdout.strip() != b"cpython\n3.12.13":
            raise AssertionError("必须使用固定 CPython 3.12.13")
        baselines = {}
        for label, relative, flags in CASES:
            source = repo / "tests/e2e" / relative
            digest = hashlib.sha256(source.read_bytes()).hexdigest()
            record = {"case": label, "source": relative, "source_sha256": digest,
                      "flags": flags, "status": "started"}
            records.append(record)
            if relative not in baselines:
                baseline = timed("python-" + source.stem, [python, str(source)], cwd=work)
                if baseline.returncode != 0 or baseline.stderr:
                    raise AssertionError(f"{relative}: 固定 CP 基线失败")
                baselines[relative] = baseline
            baseline = baselines[relative]
            output = work / label
            # 每配置只一次公开 CLI build --aot，默认开关不被实验臂污染。
            before = time.monotonic()
            published = timed("publish-" + label,
                              [ryupy, *flags, "build", str(source), "--out-dir", str(output), "--aot"],
                              cwd=work, env={**os.environ, "DOTNET": dotnet}, check=True, limit=180)
            record["build_seconds"] = time.monotonic() - before
            native = published_native(published.stderr, output)
            for name in ("publish.stdout.log", "publish.stderr.log"):
                shutil.copy2(native.parent.parent / name, logs / (label + "-" + name))
            with native.open("rb") as stream:
                if stream.read(4) != b"\x7fELF":
                    raise AssertionError(f"{label}: 产物不是 ELF")
            record["native_bytes"] = native.stat().st_size
            record["native_sha256"] = hashlib.sha256(native.read_bytes()).hexdigest()
            record["native_path"] = str(native)
            # 同配置实际CIL和native各一次；容器搬迁由前置smoke四mode负责。
            jit = timed("jit-" + label, [dotnet, str(output / (source.stem + ".dll"))], cwd=work)
            actual = timed("native-" + label, [str(native)], cwd=work,
                           env={"PATH": "/usr/bin:/bin", "HOME": str(work), "LANG": "C.UTF-8"})
            for kind, result in (("jit", jit), ("native", actual)):
                if (result.returncode != baseline.returncode or result.stdout != baseline.stdout
                        or result.stderr != baseline.stderr):
                    raise AssertionError(f"{label}/{kind}: stdout/stderr/exit 与固定 CP 不同")
            record["artifact_bytes_before_trim"] = tree_bytes(output)
            record["removed_build_cache"] = trim_build_cache(output, native)
            record["artifact_bytes_after_trim"] = tree_bytes(output)
            record["status"] = "passed"
        status = "passed"
    finally:
        # 失败不清理证据，不把部分成功写成完整覆盖。
        (work / "result.json").write_text(json.dumps({
            "status": status, "expected_configurations": 7, "records": records,
            "elapsed_seconds": time.monotonic() - started,
            "aot_isa_off": "not-tested", "nan_payload_bits": "not-tested",
            "timing_scope": "build-observation-only",
            "retained_artifact_bytes": tree_bytes(work),
        }, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
