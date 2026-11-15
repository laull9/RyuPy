"""现有基线单臂源码诊断；CP对照，不宣称候选收益。"""
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


def run(command, evidence, name, cwd=None, env=None, timeout=180, allowed_exit=(0,)):
    start = time.perf_counter_ns()
    (evidence / (name + ".command.json")).write_text(json.dumps(command))
    rss = evidence / (name + ".rss-kib")
    try:
        result = subprocess.run(["/usr/bin/time", "-q", "-f", "%M", "-o", str(rss), *command],
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
    assert result.returncode in allowed_exit, (name, result.returncode)
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
    parser.add_argument("--cli", required=True, type=Path)
    args = parser.parse_args()
    out = args.output.resolve()
    out.mkdir(parents=True, exist_ok=False)
    assert sys.version_info[:3] == (3, 12, 13)
    os.environ.pop("RYUPY_RUNTIME_DIR", None)
    os.environ.pop("RYUPY_STATS", None)
    cli = args.cli.resolve()
    identity = {"cli": digest(cli), "sources": {}}
    (out / "identity.json").write_text(json.dumps(identity, indent=2))
    inputs = {}
    rows = []
    with tempfile.TemporaryDirectory(prefix="numeric-pop-diag-") as tmp:
        owned = Path(tmp)
        for name in ("pop_once", "pop_all", "list_ops"):
            source = HERE / (name + ".py") if name != "list_ops" else ROOT / "tests/benchmarks/list_ops.py"
            identity["sources"][name] = digest(source)
            inputs[name] = source
            (out / "identity.json").write_text(json.dumps(identity, indent=2))
            shutil.copyfile(source, out / (name + ".py"))
            stage = out / name
            stage.mkdir()
            expected, _ = run([sys.executable, "-B", str(source)], stage, "CP-oracle")
            for backend in ("jit", "aot"):
                work = owned / (name + "-" + backend)
                exe = build(cli, source, work, stage, backend, backend)
                cmd = [str(exe)] if backend == "aot" else ["dotnet", str(exe)]
                actual, _ = run(cmd, stage, backend)
                assert actual == expected
                if backend == "aot":
                    samples = {"CP": [], "AOT": []}
                    for i in range(12):
                        for arm in (("CP", "AOT") if i % 2 == 0 else ("AOT", "CP")):
                            command = cmd if arm == "AOT" else [sys.executable, "-B", str(source)]
                            actual, timing = run(command, stage, arm + "-" + str(i))
                            assert actual == expected
                            samples[arm].append(timing)
                    stats_path = stage / "stats.json"
                    actual, _ = run(cmd, stage, "instrumented", env=os.environ | {"RYUPY_STATS": str(stats_path)})
                    assert actual == expected
                    stats = json.loads(stats_path.read_text())
                    assert {"main_ms", "alloc_bytes"} <= stats.keys()
                    rows.append({"name": name, "samples": samples, "stats": stats})
                    (out / "samples.json").write_text(json.dumps(rows, indent=2))
                shutil.rmtree(work)
    assert digest(cli) == identity["cli"]
    assert {k: digest(v) for k, v in inputs.items()} == identity["sources"]
    (out / "identity.json").write_text(json.dumps(identity, indent=2))
    assert len(rows) == 3
    (out / "COMPLETE").write_text("baseline only; no product optimization\n")


if __name__ == "__main__":
    main()
