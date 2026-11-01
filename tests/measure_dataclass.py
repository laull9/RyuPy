"""M3.6独立dataclass表示基准：分开记录编译/启动总时间与预热后函数体，保存语义校验值。"""
import argparse
import json
import os
from pathlib import Path
import statistics
import subprocess
import time

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--cli', required=True, type=Path)
parser.add_argument('--runtime', required=True, type=Path)
parser.add_argument('--output', required=True, type=Path)
parser.add_argument('--source', type=Path, default=Path(__file__).parent / 'perf/dataclass_hot.py')
args = parser.parse_args()
# 调用者必须给独立临时输出目录，不删除或覆盖源码/用户文件。
args.output.mkdir(parents=True, exist_ok=True)
env = dict(os.environ, RYUPY_RUNTIME_DIR=str(args.runtime.resolve()))
start = time.perf_counter()
build = subprocess.run([str(args.cli.resolve()), 'build', str(args.source.resolve()), '-o', str(args.output.resolve())],
                       text=True, capture_output=True, env=env)
compile_seconds = time.perf_counter() - start
if build.returncode:
    raise SystemExit(build.stdout + build.stderr)
start = time.perf_counter()
run = subprocess.run(['dotnet', str(args.output / 'dataclass_hot.dll')], text=True, capture_output=True, env=env)
process_seconds = time.perf_counter() - start
if run.returncode:
    raise SystemExit(run.stdout + run.stderr)
samples = {}
semantics = None
for line in run.stdout.splitlines():
    if line.startswith('semantics '):
        semantics = line
        continue
    label, elapsed, result = line.split()
    samples.setdefault(label, []).append({'seconds': float(elapsed), 'result': int(result)})
report = {'compile_seconds': compile_seconds, 'process_total_seconds': process_seconds,
          'semantics': semantics, 'samples': samples,
          'hot_medians': {name: statistics.median(x['seconds'] for x in rows) for name, rows in samples.items()}}
(args.output / 'measurement.json').write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps(report, indent=2))
