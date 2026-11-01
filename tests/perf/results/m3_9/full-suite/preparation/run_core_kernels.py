"""获授权后运行四core实际函数kernel；与原26项进程main分表，所有进程raw保留。"""
from pathlib import Path
import argparse
import json
import os
import statistics
import sys
p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--root', type=Path, required=True)
p.add_argument('--baseline', type=Path, required=True, help='完整suite输出根，含bench/<name>/<name>.dll')
p.add_argument('--candidate', type=Path, required=True)
p.add_argument('--harness', type=Path, required=True)
p.add_argument('--python', type=Path, required=True)
p.add_argument('--expected', type=Path, required=True, help='固定CPython原脚本stdout目录：fib.stdout等')
p.add_argument('--out', type=Path, required=True)
p.add_argument('--processes', type=int, default=3)
p.add_argument('--warmups', type=int, default=3)
p.add_argument('--samples', type=int, default=7)
p.add_argument('--resource-processes', type=int, default=1)
p.add_argument('--resource-samples', type=int, default=3)
a = p.parse_args()
a.out.mkdir(parents=True, exist_ok=False)
sys.path.insert(0, str(a.root.resolve() / 'tests'))
from benchlib.measure import run_once
here = Path(__file__).resolve().parent
report = {'scope': '真实编译函数kernel；非原main，输入重建和校验计时外', 'options': {k: str(v) if isinstance(v, Path) else v for k,v in vars(a).items()}, 'runs': []}
for name in ['fib','mandelbrot','matmul','nbody']:
    expected = a.expected.resolve() / (name + '.stdout')
    assert expected.is_file(), expected
    for tier in ['default','disabled']:
        env = {k:v for k,v in os.environ.items() if not k.startswith(('DOTNET_Jit','COMPlus_Jit','DOTNET_Tiered','COMPlus_Tiered'))}
        for key in ['RYUPY_STATS','RYUPY_PIC']:
            env.pop(key, None)
        env['PYTHONHASHSEED'] = '0'
        if tier == 'disabled':
            env['DOTNET_TieredCompilation'] = '0'
        for mode, processes, samples in [('time',a.processes,a.samples),('resources',a.resource_processes,a.resource_samples)]:
            for repeat in range(processes):
                variants = ['baseline','candidate']
                # CPython无tier，只测一次分组；输入计算与CLR同kernel规模，独立列。
                if tier == 'default':
                    variants.append('cpython')
                offset = repeat % len(variants)
                for variant in variants[offset:] + variants[:offset]:
                    if variant == 'cpython':
                        command = [str(a.python.resolve()), str(here/'core_cpython.py'), str(a.root.resolve()/f'tests/benchmarks/{name}.py'), str(expected), mode, str(a.warmups), str(samples)]
                    else:
                        artifact = getattr(a, variant).resolve()/f'bench/{name}/{name}.dll'
                        command = ['dotnet', str(a.harness.resolve()), str(artifact), name, str(expected), mode, str(a.warmups), str(samples)]
                    sample = run_once(command,env,a.root.resolve(),'RYUPY_KERNEL_UNUSED_STATS')
                    stem = f'{name}-{tier}-{mode}-{variant}-{repeat}'
                    prefix = a.out/stem
                    prefix.with_suffix('.stdout').write_bytes(sample.stdout)
                    prefix.with_suffix('.stderr').write_bytes(sample.stderr)
                    rows = []
                    if sample.exit_code == 0:
                        payload = sample.stdout
                        if variant != 'cpython':
                            reference = expected.read_bytes()
                            assert payload.startswith(reference), '原main输出前缀与固定CPython不符'
                            payload = payload[len(reference):]
                        rows = [json.loads(line) for line in payload.decode().splitlines()]
                        assert len(rows) == samples and all(row['name'] == name for row in rows)
                    record = {'name':name,'tier':tier,'mode':mode,'variant':variant,'repeat':repeat,'command':command,'exit_code':sample.exit_code,'process_wall_ms':sample.wall_ms,'maxrss':sample.maxrss,'samples':rows}
                    prefix.with_suffix('.json').write_text(json.dumps(record,indent=2)+'\n')
                    report['runs'].append(record)
                    # 持续保存，长跑中断仍能看到已完成项，不伪报未完成项。
                    (a.out/'results.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
                    print(stem, 'ok' if sample.exit_code == 0 else 'FAILED', flush=True)
                    if sample.exit_code != 0:
                        raise SystemExit(1)
report['time_summary'] = {}
for name in ['fib','mandelbrot','matmul','nbody']:
    for tier in ['default','disabled']:
        for variant in ['baseline','candidate','cpython']:
            matches=[r for r in report['runs'] if r['name']==name and r['tier']==tier and r['variant']==variant and r['mode']=='time' and r['exit_code']==0]
            if matches:
                report['time_summary'][f'{name}_{tier}_{variant}']=statistics.median(statistics.median(x['elapsed_ms'] for x in r['samples']) for r in matches)
(a.out/'results.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
raise SystemExit(1 if any(r['exit_code'] for r in report['runs']) else 0)
