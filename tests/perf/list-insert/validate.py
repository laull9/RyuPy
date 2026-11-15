"""固定源码宽下标语义差分、原Runtime缺陷分类与JIT/AOT搬迁验证。"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import time

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from benchlib import suite
from benchlib.artifact import verified_native


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def run(command, evidence, name, cwd=None, env=None, timeout=180):
    start = time.perf_counter_ns()
    (evidence / (name + ".command.json")).write_text(json.dumps(command))
    rss = evidence / (name + ".rss-kib")
    try:
        result = subprocess.run(["/usr/bin/time", "-f", "%M", "-o", str(rss), *command],
                                cwd=cwd, env=env, capture_output=True, timeout=timeout)
    except subprocess.TimeoutExpired as error:
        (evidence / (name + ".stdout")).write_bytes(error.stdout or b"")
        (evidence / (name + ".stderr")).write_bytes(error.stderr or b"")
        (evidence / (name + ".exit")).write_text("TIMEOUT")
        raise
    elapsed = (time.perf_counter_ns() - start) / 1e6
    for suffix, value in (("stdout", result.stdout), ("stderr", result.stderr)):
        (evidence / (name + "." + suffix)).write_bytes(value)
    (evidence / (name + ".exit")).write_text(str(result.returncode))
    assert result.returncode == 0, (name, result.returncode)
    timing = {"ms": elapsed, "rss_kib": int(rss.read_text().strip())}
    # 每次成功执行即时落盘；后续样本失败不能抹掉已有原始计时。
    (evidence / (name + ".timing.json")).write_text(json.dumps(timing))
    return (result.stdout, result.stderr), timing


def build(cli, source, work, evidence, label, backend):
    bench = suite.Benchmark(name=source.stem, path=source)
    work.mkdir(parents=True, exist_ok=False)
    command = [str(cli), "build", str(source), "-o", str(work), "--native"]
    if backend == "aot":
        command.append("--aot")
    streams, timing = run(command, evidence, label + "-build", cwd=ROOT, timeout=1800)
    exe = verified_native(work, bench, streams[1].decode(), mode="standalone") if backend == "aot" else work / (source.stem + ".dll")
    assert exe.is_file()
    ms = timing["ms"]
    record = {"build_ms": ms, "executable_sha256": digest(exe)}
    if backend == "aot":
        manifest = exe.parent / "ryupy-artifact.json"
        shutil.copyfile(manifest, evidence / (label + "-manifest.json"))
        data = json.loads(manifest.read_text())
        assert data["toolchain"]["sdk_version"] == "10.0.403"
        record["manifest_sha256"] = digest(manifest)
    (evidence / (label + "-build.json")).write_text(json.dumps(record))
    return exe



def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("output", type=Path)
    parser.add_argument("--old", required=True, type=Path)
    parser.add_argument("--new", required=True, type=Path)
    args = parser.parse_args()
    out = args.output.resolve()
    out.mkdir(parents=True, exist_ok=False)
    assert sys.version_info[:3] == (3, 12, 13)
    os.environ.pop("RYUPY_RUNTIME_DIR", None)
    os.environ.pop("RYUPY_STATS", None)
    source = ROOT / "tests/e2e/phase2/m2_list_insert_wide_index.py"
    clis = {"base": args.old.resolve(), "candidate": args.new.resolve()}
    identity = {"source": digest(source), "cli": {a: digest(p) for a, p in clis.items()}}
    (out / "identity.json").write_text(json.dumps(identity, indent=2))
    shutil.copyfile(source, out / source.name)
    expected, _ = run([sys.executable, "-B", str(source)], out, "CP")
    # 仅允许已知三个截断位置的差异；其余输出、退出、双流必须相同。
    baseline = expected[0].replace(
        b"-9223372036854775808 [['new'], 10, 20, 30]",
        b"-9223372036854775808 [10, 20, 30, ['new']]").replace(
        b"-4294967296 [['new'], 10, 20, 30]",
        b"-4294967296 [10, 20, 30, ['new']]").replace(
        b"['front', 7, 8]", b"[7, 8, 'front']")
    assert baseline != expected[0]
    with tempfile.TemporaryDirectory(prefix="list-insert-validation-") as tmp:
        owned = Path(tmp)
        for arm, cli in clis.items():
            oracle = expected
            for backend in ("jit", "aot"):
                label = arm + "-" + backend
                work = owned / label
                inputs = owned / (label + "-input")
                inputs.mkdir()
                frozen = inputs / source.name
                shutil.copyfile(source, frozen)
                assert digest(frozen) == identity["source"]
                exe = build(cli, frozen, work, out, label, backend)
                cmd = [str(exe)] if backend == "aot" else ["dotnet", str(exe)]
                actual, _ = run(cmd, out, label)
                if arm == "base":
                    assert actual in (expected, (baseline, expected[1])), label
                    oracle = actual
                    (out / (label + "-classification.txt")).write_text(
                        "matches-CP" if actual == expected else "exact-known-Insert-truncation")
                else:
                    assert actual == oracle, label
                if backend == "aot":
                    moved = owned / (label + " 中文 搬迁")
                    shutil.copytree(exe.parent, moved)
                    shutil.rmtree(work)
                    shutil.rmtree(inputs)
                    actual, _ = run([str(moved / exe.name)], out, label + "-moved",
                                    cwd=moved, env=os.environ | {"PATH": "/nonexistent"})
                    assert actual == oracle, label + "-moved"
                    shutil.rmtree(moved)
                else:
                    shutil.rmtree(work)
                    shutil.rmtree(inputs)
    assert digest(source) == identity["source"]
    assert {a: digest(p) for a, p in clis.items()} == identity["cli"]
    (out / "COMPLETE").write_text("CP/base/candidate JIT+AOT+relocation verified\n")


if __name__ == "__main__":
    main()
