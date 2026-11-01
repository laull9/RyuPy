"""仅在获授权的窗口执行：固定保存CLI、独立输出、同runtime，原bench框架仍负责26项和pending。"""
from pathlib import Path
import argparse
import hashlib
import importlib.util
import json
import os
import shutil
import subprocess
import sys
import time

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--root', type=Path, required=True)
p.add_argument('--cli', type=Path, required=True)
p.add_argument('--runtime', type=Path, required=True)
p.add_argument('--out', type=Path, required=True)
p.add_argument('--python', type=Path, required=True)
p.add_argument('--tier', choices=['default', 'disabled'], required=True)
p.add_argument('--runs', type=int, default=3)
p.add_argument('--warmup', type=int, default=1)
p.add_argument('--cli-profile', choices=['debug', 'release', 'unknown'], required=True)
p.add_argument('filters', nargs='*')
a = p.parse_args()
root, cli, runtime, out = (x.resolve() for x in (a.root, a.cli, a.runtime, a.out))
if a.runs < 1 or a.warmup < 0:
    p.error('runs需正数、warmup不能负数')
out.mkdir(parents=True, exist_ok=False)
raw = out / 'raw'
raw.mkdir()
# 保留框架原有PIC变体，只消除外部残留tier/JIT开关；默认不是强行TieredCompilation=1。
for key in list(os.environ):
    if key.startswith(('DOTNET_Jit', 'COMPlus_Jit', 'DOTNET_Tiered', 'COMPlus_Tiered')):
        os.environ.pop(key)
if a.tier == 'disabled':
    os.environ['DOTNET_TieredCompilation'] = '0'
os.environ['PYTHON'] = str(a.python.resolve())
sys.path.insert(0, str(root / 'tests'))
from benchlib import measure, suite
spec = importlib.util.spec_from_file_location('saved_suite_entry', root / 'tests/run_benchmarks.py')
entry = importlib.util.module_from_spec(spec)
spec.loader.exec_module(entry)
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
benches = suite.select(suite.discover(root / 'tests/benchmarks'), a.filters)
manifest = {'cli': str(cli), 'cli_sha256': sha(cli), 'cli_profile': a.cli_profile,
            'runtime': str(runtime), 'runtime_sha256': sha(runtime), 'tier': a.tier,
            'python': str(a.python.resolve()), 'runs': a.runs, 'warmup': a.warmup,
            'warmup_scope': '另起进程；main_ms仍含本进程JIT，不是充分稳态',
            'benchmarks': [{'name': b.name, 'pending': b.pending, 'sha256': sha(b.path)} for b in benches]}
(out / 'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n')
measure.target_dir = lambda ignored: out
# 不调用cargo，也不把当前CLI复制覆盖保存CLI。
def saved_cli(ignored_root, ignored_target):
    subprocess.run([str(cli), '--help'], cwd=root, capture_output=True, check=True)
    return cli
measure.build_cli = saved_cli

def build_case(ignored_root, compiler, bench, target):
    dest = target / 'bench' / bench.name
    cmd = [str(compiler), 'build', str(bench.path), '-o', str(dest)]
    start = time.perf_counter()
    r = subprocess.run(cmd, cwd=root, text=True, capture_output=True)
    elapsed = (time.perf_counter() - start) * 1000
    (raw / f'build-{bench.name}.stdout').write_text(r.stdout)
    (raw / f'build-{bench.name}.stderr').write_text(r.stderr)
    record = {'command': cmd, 'compile_ms': elapsed, 'exit_code': r.returncode}
    if r.returncode == 0:
        # 编译计时不包括固定runtime复制；两方运行的是完全相同的Release runtime。
        shutil.copy2(runtime, dest / 'RyuPy.Runtime.dll')
        assert sha(dest / 'RyuPy.Runtime.dll') == manifest['runtime_sha256']
        record['dll_sha256'] = sha(dest / f'{bench.name}.dll')
    (raw / f'build-{bench.name}.json').write_text(json.dumps(record, indent=2) + '\n')
    first = next((s.strip() for s in r.stderr.splitlines() if s.strip()), '')
    return (dest / f'{bench.name}.dll' if r.returncode == 0 else None), elapsed, first
measure.build_case = build_case
original_run = measure.run_once
serial = 0

def run_once(command, env, cwd, stats_env):
    global serial
    sample = original_run(command, env, cwd, stats_env)
    serial += 1
    prefix = raw / f'process-{serial:05d}'
    prefix.with_suffix('.stdout').write_bytes(sample.stdout)
    prefix.with_suffix('.stderr').write_bytes(sample.stderr)
    record = {'command': command, 'stats_env': stats_env, 'wall_ms': sample.wall_ms,
              'maxrss': sample.maxrss, 'stats': sample.stats, 'exit_code': sample.exit_code,
              'environment': {k: v for k, v in env.items() if k in ['RYUPY_PIC', 'DOTNET_TieredCompilation', 'PYTHONHASHSEED']}}
    prefix.with_suffix('.json').write_text(json.dumps(record, ensure_ascii=False, indent=2) + '\n')
    return sample
measure.run_once = run_once
original_measure_bench = measure.measure_bench
def measured_bench(*args, **kwargs):
    result = original_measure_bench(*args, **kwargs)
    print('BENCH_STATUS', result.bench.name, result.status, flush=True)
    return result
measure.measure_bench = measured_bench
code = entry.main(['--include-pending', '--runs', str(a.runs), '--warmup', str(a.warmup), '--detail', '--save', 'suite', *a.filters])
(out / 'exit-code.txt').write_text(str(code) + '\n')
raise SystemExit(code)
