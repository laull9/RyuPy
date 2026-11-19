"""四独立AOT构建轮次、每轮12次完整六排列，保留每轮原始样本且不择优汇报。"""
import argparse
from collections import Counter
import itertools
import json
import math
from pathlib import Path
import statistics
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
EXPECTED = {'original':{'generators'},'generator-consumers':{'ordinary_for','delegated_for','genexpr_sum'}}


def batch_roles(manifest, enabled, name):
    hits = [r for r in manifest['emitted_delegate_references'] if r['role'] == 'generator_sum_resume']
    expected = 1 if enabled and name in ('generators', 'genexpr_sum') else 0
    assert len(hits) == expected, '逐轮真实委托发射与归一化off/on臂不符'
    assert all(r['target'] is not None for r in hits)


def verify(root):
    result = {}
    for index in (1,2,3,4):
        off,on=('B','A') if index % 2 == 0 else ('A','B')
        for selected,names in EXPECTED.items():
            folder=root/f'round-{index}'/selected; data=json.loads((folder/'paired.json').read_text())
            expected_old='frozen-on' if index % 2 == 0 else 'frozen-off'
            expected_new='frozen-off' if index % 2 == 0 else 'frozen-on'
            assert Path(data['old']).parent.name==expected_old and Path(data['new']).parent.name==expected_new
            assert data['runs']==12 and data['warmup']==1
            assert len(data['rows'])==len(names) and {r['name'] for r in data['rows']}==names
            for row in data['rows']:
                assert row['build_order']==['A','B']
                assert Counter(map(tuple,row['execution_order']))==Counter({p:2 for p in itertools.permutations(('CP','A','B'))})
                assert set(row['samples'])=={'CP','A','B'}
                for values in row['samples'].values():
                    assert len(values)==12 and all(math.isfinite(v['ms']) and v['ms']>0 and v['rss']>=0 for v in values)
                for arm in ('A','B'):
                    assert row['free_bytes_before_build'][arm]>=4*1024**3
                    assert {'main_ms','alloc_bytes'}<=row['stats'][arm].keys()
                    m=json.loads((folder/'manifests'/row['name']/arm/'ryupy-artifact.json').read_text())
                    batch_roles(m, arm == on, row['name'])
                    assert m['toolchain']['sdk_version']=='10.0.403' and m['toolchain']['target_framework_major']==10
                samples={label:[s['ms'] for s in row['samples'][arm]] for label,arm in [('off',off),('on',on),('CP','CP')]}
                med={label:statistics.median(values) for label,values in samples.items()}
                result.setdefault(row['name'],[]).append({'round':index,'off_arm':off,'on_arm':on,
                    'medians_ms':med,'on_over_off':med['on']/med['off'],
                    'range_ms':{label:[min(v),max(v)] for label,v in samples.items()},
                    'allocation_bytes':{label:row['stats'][arm]['alloc_bytes'] for label,arm in [('off',off),('on',on)]}})
    return {'rounds':4,'pairs_per_round':12,'results':result,
            'limits':['4次AOT构建，每臂48新进程样本非48次独立构建','含启动与I/O，共享runner并非独占硬件','分配每轮每臂另测一次，不是每个wall样本','不自动输出显著性或无回退结论']}


def run(args):
    root=args.output.resolve();assert not root.is_relative_to(ROOT);root.mkdir(parents=True,exist_ok=False)
    for index in (1,2,3,4):
        old,new=(args.new,args.old) if index%2==0 else (args.old,args.new)
        suites=list(EXPECTED)
        if index%2==0:suites.reverse()
        for selected in suites:
            folder=root/f'round-{index}'/selected;folder.mkdir(parents=True)
            command=[str(args.python),'-B',str(ROOT/'tests/perf/aot_hotspot_pair.py'),
                     '--suite',selected,'--only',','.join(sorted(EXPECTED[selected])),
                     '--old',str(old),'--new',str(new),'--python',str(args.python),'--runs','12','--balanced-order',
                     '--manifest-evidence',str(folder/'manifests'),'--min-free-gib','4','--cleanup-completed',
                     '--output',str(folder/'paired.json')]
            if index%2==0:command.append('--reverse-cases')
            (folder/'command.json').write_text(json.dumps(command,indent=2))
            with (folder/'run.log').open('w') as log:
                subprocess.run(command,cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,check=True)
    report=verify(root);(root/'study.json').write_text(json.dumps(report,ensure_ascii=False,indent=2))
    (root/'complete.txt').write_text('COMPLETE\n')
    for name,rows in report['results'].items():print(name,[round((r['on_over_off']-1)*100,3) for r in rows],flush=True)


if __name__=='__main__':
    p=argparse.ArgumentParser()
    for name in ('output','old','new','python'):p.add_argument('--'+name,type=Path,required=True)
    run(p.parse_args())
