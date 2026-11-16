"""固定源码与双CLI的语义/搬迁及四轮平衡冷进程AOT样本。"""
import argparse
import hashlib
import itertools
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
    clis = {"A": args.old.resolve(), "B": args.new.resolve()}
    sources = {"semantic": HERE / "semantic.py", "tail_once": HERE / "tail_once.py", "tail_many": HERE / "tail_many.py",
               "head_once": HERE / "head_once.py", "head_many": HERE / "head_many.py", "list_ops": ROOT / "tests/benchmarks/list_ops.py"}
    identity = {"cli": {a: digest(p) for a, p in clis.items()},
                "source": {a: digest(p) for a, p in sources.items()}}
    (out / "identity.json").write_text(json.dumps(identity, indent=2))
    for name, source in sources.items():
        shutil.copyfile(source, out / (name + ".py"))
    with tempfile.TemporaryDirectory(prefix="numeric-list-product-") as tmp:
        owned = Path(tmp)
        source = sources["semantic"]
        expected, _ = run([sys.executable, "-B", str(source)], out, "semantic-CP")
        for arm, cli in clis.items():
            for backend in ("jit", "aot"):
                label = "semantic-" + arm + "-" + backend
                work = owned / label
                input_dir = owned / (label + "-input")
                input_dir.mkdir()
                frozen_source = input_dir / source.name
                shutil.copyfile(source, frozen_source)
                assert digest(frozen_source) == digest(source)
                exe = build(cli, frozen_source, work, out, label, backend)
                cmd = [str(exe)] if backend == "aot" else ["dotnet", str(exe)]
                actual, _ = run(cmd, out, label)
                assert actual == expected, label
                if backend == "aot":
                    moved = owned / (label + " 中文 空格")
                    shutil.copytree(exe.parent, moved)
                    # 原构建树移除，搬迁从无工具PATH运行。仅清理由本次生成的临时树。
                    shutil.rmtree(work)
                    shutil.rmtree(input_dir)
                    actual, _ = run([str(moved / exe.name)], out, label + "-moved",
                                    cwd=moved, env=os.environ | {"PATH": "/nonexistent"})
                    assert actual == expected, label + "-moved"
                    shutil.rmtree(moved)
                else:
                    shutil.rmtree(work)
        rows = []
        for round_no, order in enumerate(("AB", "BA", "BA", "AB")):
            for name in ("tail_once", "tail_many", "head_once", "head_many", "list_ops"):
                source = sources[name]
                stage = out / (str(round_no) + "-" + name)
                stage.mkdir()
                expected, _ = run([sys.executable, "-B", str(source)], stage, "oracle")
                exes = {}
                for arm in order:
                    exes[arm] = build(clis[arm], source, owned / arm, stage, arm, "aot")
                samples = {a: [] for a in ("CP", "A", "B")}
                # 六种三臂顺序各两次，每轮改变起点；无丢弃样本或暖态推断。
                for index in range(12):
                    arms = list(itertools.permutations(("CP", "A", "B")))[(index + round_no) % 6]
                    for arm in arms:
                        cmd = [sys.executable, "-B", str(source)] if arm == "CP" else [str(exes[arm])]
                        actual, ms = run(cmd, stage, arm + "-" + str(index))
                        assert actual == expected, (name, arm, index)
                        samples[arm].append(ms | {"order": arms.index(arm)})
                stats = {}
                for arm in order:
                    stats_path = stage / (arm + "-stats.json")
                    actual, _ = run([str(exes[arm])], stage, arm + "-instrumented",
                                    env=os.environ | {"RYUPY_STATS": str(stats_path)})
                    assert actual == expected
                    stats[arm] = json.loads(stats_path.read_text())
                    assert {"main_ms", "alloc_bytes"} <= stats[arm].keys()
                rows.append({"stats": stats, "round": round_no, "name": name, "build_order": order, "samples": samples})
                (out / "samples.json").write_text(json.dumps(rows, indent=2))
                for arm in order:
                    shutil.rmtree(owned / arm)
    assert identity["cli"] == {a: digest(p) for a, p in clis.items()}
    assert identity["source"] == {a: digest(p) for a, p in sources.items()}
    assert len(rows) == 20
    assert {(row["round"], row["name"]) for row in rows} == {
        (r, name) for r in range(4) for name in ("tail_once", "tail_many", "head_once", "head_many", "list_ops")}
    assert sum(len(samples) for row in rows for samples in row["samples"].values()) == 720
    assert len(list(out.rglob("*-manifest.json"))) == 42
    (out / "COMPLETE").write_text("semantic CP/A/B JIT/AOT relocation; 4x5x3x12 cold samples; 42 AOT manifests\n")


if __name__ == "__main__":
    main()
