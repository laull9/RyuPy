"""SLP前置三臂：旧scalar/同guard去盒typed scalar/packed，通用轮转；不构建、不替换runtime，不挑快路样本。"""
import argparse
import json
import os
from pathlib import Path
import statistics
import subprocess
import time

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('variants', nargs='+', help='标签=包含slp_pairs.dll和对应runtime的目录')
parser.add_argument('--output', required=True, type=Path)
parser.add_argument('--processes', type=int, default=3)
args = parser.parse_args()
if len(args.variants) != 3:
    parser.error('必须提供 A旧scalar、B诊断scalar、Cpacked 三臂')
if args.processes < 1:
    parser.error('processes 必须为正数')
variants = {item.split('=', 1)[0]: Path(item.split('=', 1)[1]).resolve() for item in args.variants}
args.output.mkdir(parents=True, exist_ok=False)
expected = {name: ['True'] for name in ['float_list', 'float_tuple', 'scalar_tail', 'first_bool', 'last_bool', 'first_custom', 'last_custom']}
report = {'variants': {name: str(path) for name, path in variants.items()}, 'runs': {}}
for tier in ('default', 'disabled'):
    env = dict(os.environ)
    for key in list(env):
        if key.startswith(('DOTNET_Jit', 'COMPlus_Jit', 'DOTNET_Tiered', 'COMPlus_Tiered')):
            env.pop(key)
    if tier == 'disabled':
        env['DOTNET_TieredCompilation'] = '0'
    for repeat in range(args.processes):
        offset = repeat % len(variants)
        order = list(variants)[offset:] + list(variants)[:offset]
        for name in order:
            start = time.perf_counter()
            result = subprocess.run(['dotnet', str(variants[name] / 'slp_pairs.dll')],
                                    env=env, text=True, capture_output=True)
            wall = time.perf_counter() - start
            prefix = args.output / f'{tier}-{name}-{repeat}'
            prefix.with_suffix('.stdout').write_text(result.stdout)
            prefix.with_suffix('.stderr').write_text(result.stderr)
            if result.returncode:
                raise RuntimeError(f'子进程失败：{prefix} exit={result.returncode}')
            samples = {label: [] for label in expected}
            semantics = {}
            for line in result.stdout.splitlines():
                parts = line.split()
                if parts[0] == 'semantics':
                    label = parts[1]
                    assert parts[2:] == expected[label], line
                    assert label not in semantics, line
                    semantics[label] = parts[2:]
                else:
                    assert parts[0] == 'sample', line
                    label = parts[1]
                    assert parts[3:] == expected[label], line
                    samples[label].append(float(parts[2]))
            assert set(semantics) == set(expected)
            assert all(len(values) == 7 for values in samples.values())
            report['runs'].setdefault(tier + '_' + name, []).append({
                'samples': samples, 'semantics': semantics, 'process_wall_seconds': wall,
                'medians': {label: statistics.median(values) for label, values in samples.items()},
            })
report['summary'] = {
    variant: {label: statistics.median(run['medians'][label] for run in runs) for label in expected}
    for variant, runs in report['runs'].items()
}
(args.output / 'comparison.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(json.dumps(report['summary'], indent=2))
