#!/usr/bin/env python3
"""压缩标准库 CPython/JIT/搬迁 NativeAOT 双向验收；只检查行为与容器字段，不计算文件摘要。"""
import argparse
import gzip
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys
import tempfile
import time
import zlib


def fixtures(directory):
    directory.mkdir()
    corpus = {"empty": b"", "mixed": bytes(range(256)) * 16 + "猫🎉\0".encode() * 100}
    for kind, data in corpus.items():
        (directory / (kind + ".bin")).write_bytes(data)
        for level in range(-1, 10):
            for window in (15, -15, 31):
                name = f"cp-{kind}-{level}-{window}"
                (directory / name).write_bytes(zlib.compress(data, level, wbits=window))
    packed = gzip.compress(b"one", mtime=0)
    (directory / "cp-members").write_bytes(packed + gzip.compress(b"\0two", mtime=1) + b"\0\0")
    header = bytearray(packed[:10])
    header[3] = 4 | 8 | 16 | 2
    header.extend(b"\x03\0abcname\0comment\0")
    header.extend((zlib.crc32(header) & 65535).to_bytes(2, "little"))
    (directory / "cp-header").write_bytes(header + packed[10:])
    (directory / "cp-truncated").write_bytes(packed[:-1])
    (directory / "cp-tail").write_bytes(packed + b"bad")
    for name, offset in (("cp-bad-crc", -8), ("cp-bad-size", -4)):
        damaged = bytearray(packed)
        damaged[offset] ^= 1
        (directory / name).write_bytes(damaged)


def verify_outputs(directory, before, after):
    count = 0
    for kind in ("empty", "mixed"):
        original = (directory / (kind + ".bin")).read_bytes()
        for level in range(-1, 10):
            for window in (15, -15, 31):
                assert zlib.decompress((directory / f"out-{kind}-{level}-{window}").read_bytes(), window) == original
                count += 1
        for stamp in (0, 1, 1234567890, 4294967295):
            packed = (directory / f"out-{kind}-mtime-{stamp}").read_bytes()
            assert int.from_bytes(packed[4:8], "little") == stamp
            assert gzip.decompress(packed) == original
            count += 1
    packed = (directory / "out-time").read_bytes()
    assert before <= int.from_bytes(packed[4:8], "little") <= after
    assert gzip.decompress(packed) == b"now"
    for name, original in (("file", b"first\nsecond\nend"), ("empty", b""),
                           ("text", "猫\r\n🎉\r\n".encode())):
        packed = (directory / ("out-" + name)).read_bytes()
        assert len(packed) >= 20
        assert gzip.decompress(packed) == original
        assert before <= int.from_bytes(packed[4:8], "little") <= after
        count += 1
    return count + 1


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ryupy", required=True, type=Path)
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--aot", action="store_true")
    args = parser.parse_args()
    assert sys.version_info[:3] == (3, 12, 13), sys.version
    args.out.mkdir(parents=True, exist_ok=True)
    work = Path(tempfile.mkdtemp(prefix="compression-", dir=args.out.resolve()))
    print(work, flush=True)
    logs = work / "logs"
    logs.mkdir()
    records = {"status": "failed", "host": platform.platform(), "python": sys.version, "steps": []}
    ryupy = args.ryupy.resolve()
    env = dict(os.environ)
    env["DOTNET_CLI_UI_LANGUAGE"] = "en-US"

    def run(label, command, cwd, process_env=None, timeout=180):
        command = list(map(str, command))
        result = subprocess.run(command, cwd=cwd, env=env if process_env is None else process_env,
                                capture_output=True, timeout=timeout)
        (logs / (label + ".stdout")).write_bytes(result.stdout)
        (logs / (label + ".stderr")).write_bytes(result.stderr)
        records["steps"].append({"step": label, "argv": command, "exit": result.returncode})
        print(label, result.returncode, flush=True)
        assert result.returncode == 0, (label, result.stderr.decode(errors="replace"))
        return result

    try:
        source = work / "source"
        source.mkdir()
        app = source / "native_probe.py"
        shutil.copy2(Path(__file__).with_name("native_probe.py"), app)
        baseline = work / "cpython"
        fixtures(baseline)
        before = int(time.time())
        expected = run("cpython", [sys.executable, app], baseline)
        records["cpython_cross_outputs"] = verify_outputs(baseline, before, int(time.time()))
        jit = work / "jit"
        fixtures(jit)
        build = work / "jit-build"
        run("jit-build", [ryupy, "build", app, "-o", build], work)
        before = int(time.time())
        actual = run("jit", [shutil.which("dotnet"), build / "native_probe.dll"], jit)
        assert (actual.stdout, actual.stderr) == (expected.stdout, expected.stderr)
        records["jit_cross_outputs"] = verify_outputs(jit, before, int(time.time()))
        if args.aot:
            native_build = work / "native-build"
            run("aot-build", [ryupy, "build", app, "--aot", "-o", native_build], work, timeout=600)
            native_dist = native_build / "native_probe.dist"
            moved = work / "搬迁 native bundle"
            shutil.copytree(native_dist, moved)
            executable = moved / ("native_probe.exe" if os.name == "nt" else "native_probe")
            assert executable.is_file()
            # 移除本轮源与原构建，改用无工具 PATH，从另一个 cwd 直接启动真实本机产物。
            shutil.rmtree(source)
            shutil.rmtree(build)
            shutil.rmtree(native_build)
            isolated = work / "native-cwd"
            fixtures(isolated)
            empty_path = work / "empty-path"
            empty_path.mkdir()
            clean_env = {key: value for key, value in env.items()
                         if not key.startswith(("PYTHON", "RYUPY", "DOTNET", "COREHOST", "COMPlus"))}
            clean_env["PATH"] = str(empty_path)
            before = int(time.time())
            actual = run("native-moved", [executable], isolated, clean_env)
            assert (actual.stdout, actual.stderr) == (expected.stdout, expected.stderr)
            records["native_cross_outputs"] = verify_outputs(isolated, before, int(time.time()))
            records["native_relocated"] = True
        records["status"] = "passed"
    finally:
        (work / "result.json").write_text(json.dumps(records, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
