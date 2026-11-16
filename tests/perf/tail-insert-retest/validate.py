"""仅性能复测；固定产品双CLI，不重复语义/搬迁验收。"""
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
    parser.add_argument('output', type=Path)
    parser.add_argument('--old', type=Path, required=True)
    parser.add_argument('--new', type=Path, required=True)
    parser.add_argument('--round', type=int, choices=range(4), required=True)
    args = parser.parse_args()
    out = args.output.resolve()
    out.mkdir(parents=True, exist_ok=False)
    assert sys.version_info[:3] == (3, 12, 13)
    os.environ.pop('RYUPY_RUNTIME_DIR', None)
    os.environ.pop('RYUPY_STATS', None)
    original = ROOT / 'tests/perf/numeric-tail-insert'
    sources = {'tail_many': original / 'tail_many.py', 'tail_many_long': HERE / 'tail_many_long.py',
               'head_many': original / 'head_many.py', 'list_ops': ROOT / 'tests/benchmarks/list_ops.py'}
    assert sources['tail_many_long'].read_bytes() == sources['tail_many'].read_bytes().replace(
        b'for repeat in range(10):', b'for repeat in range(100):')
    clis = {'A': args.old.resolve(), 'B': args.new.resolve()}
    order = ('AB', 'BA', 'BA', 'AB')[args.round]
    identity = {'round': args.round, 'build_order': order, 'cli': {a: digest(p) for a, p in clis.items()},
                'source': {name: digest(p) for name, p in sources.items()}}
    (out / 'identity.json').write_text(json.dumps(identity, indent=2))
    for name, source in sources.items():
        shutil.copyfile(source, out / (name + '.py'))
    rows = []
    with tempfile.TemporaryDirectory(prefix='tail-insert-perf-') as tmp:
        owned = Path(tmp)
        for name, source in sources.items():
            stage = out / name
            stage.mkdir()
            expected, _ = run([sys.executable, '-B', str(source)], stage, 'oracle')
            exes = {}
            for arm in order:
                if name != 'list_ops':
                    streams, _ = run([str(clis[arm]), 'dump', str(source), '--emit', 'mir'], stage, arm + '-mir', cwd=ROOT)
                    body = streams[0].decode().split('fn produce(', 1)[1].split('\nfn ', 1)[0]
                    for marker in ('NumericListCreate', 'NumericListAppendF64', 'NumericListReadCheckedF64'):
                        assert marker in body, (name, arm, marker)
                exes[arm] = build(clis[arm], source, owned / arm, stage, arm, 'aot')
            samples = {a: [] for a in ('CP', 'A', 'B')}
            for index in range(12):
                arms = list(itertools.permutations(('CP', 'A', 'B')))[(index + args.round) % 6]
                for arm in arms:
                    cmd = [sys.executable, '-B', str(source)] if arm == 'CP' else [str(exes[arm])]
                    actual, timing = run(cmd, stage, arm + '-' + str(index))
                    assert actual == expected, (name, arm, index)
                    samples[arm].append(timing | {'order': arms.index(arm)})
            stats = {}
            for arm in order:
                stats_path = stage / (arm + '-stats.json')
                actual, _ = run([str(exes[arm])], stage, arm + '-instrumented',
                                env=os.environ | {'RYUPY_STATS': str(stats_path)})
                assert actual == expected
                stats[arm] = json.loads(stats_path.read_text())
                assert {'main_ms', 'alloc_bytes'} <= stats[arm].keys()
            rows.append({'round': args.round, 'name': name, 'build_order': order, 'samples': samples, 'stats': stats})
            (out / 'samples.json').write_text(json.dumps(rows, indent=2))
            for arm in order:
                shutil.rmtree(owned / arm)
    assert identity['cli'] == {a: digest(p) for a, p in clis.items()}
    assert identity['source'] == {name: digest(p) for name, p in sources.items()}
    assert len(rows) == 4
    assert sum(len(v) for row in rows for v in row['samples'].values()) == 144
    assert len(list(out.rglob('*-manifest.json'))) == 8
    (out / 'COMPLETE').write_text('performance only; 144 cold samples; 8 AOT manifests\n')


if __name__ == '__main__':
    main()
