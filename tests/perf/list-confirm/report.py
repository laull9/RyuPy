"""固定六项三轮配对的证据门与描述统计；反向轮交换CLI标签，不能误认A/B。"""
import json
import math
from pathlib import Path
import statistics
import sys

NAMES = {'list_ops','matmul','containers','sieve','closures','decorators'}
BASE = '81758bb477729aee4636f2ca6d6b0815b949849a'
CANDIDATE = '7980e75650f1873de9bb37476ff47f47b9ae0bf3'


def verify(root):
    assert (root/'base.txt').read_text().strip()==BASE
    assert (root/'candidate.txt').read_text().strip()==CANDIDATE
    assert (root/'python.txt').read_text().strip()=='Python 3.12.13'
    assert (root/'sdk.txt').read_text().strip()=='10.0.403'
    summary={name:[] for name in sorted(NAMES)}
    for index in (1,2,3):
        folder=root/f'round-{index}';data=json.loads((folder/'paired.json').read_text())
        assert data['runs']==15 and data['warmup']==1
        assert len(data['rows'])==6 and {r['name'] for r in data['rows']}==NAMES
        baseline_arm,candidate_arm=('B','A') if index==2 else ('A','B')
        expected_old='frozen-candidate' if index==2 else 'frozen-base'
        expected_new='frozen-base' if index==2 else 'frozen-candidate'
        assert Path(data['old']).parent.name==expected_old and Path(data['new']).parent.name==expected_new
        for row in data['rows']:
            assert set(row['samples'])=={'CP','A','B'}
            for samples in row['samples'].values():
                assert len(samples)==15
                assert all(math.isfinite(s['ms']) and s['ms']>0 and s['rss']>=0 for s in samples)
            for arm in ('A','B'):
                assert row['free_bytes_before_build'][arm]>=4*1024**3
                assert {'main_ms','alloc_bytes'}<=row['stats'][arm].keys()
                m=json.loads((folder/'manifests'/row['name']/arm/'ryupy-artifact.json').read_text())
                assert m['toolchain']['sdk_version']=='10.0.403' and m['toolchain']['target_framework_major']==10
            values={label:[x['ms'] for x in row['samples'][arm]] for label,arm in
                    [('base',baseline_arm),('candidate',candidate_arm),('CP','CP')]}
            medians={label:statistics.median(samples) for label,samples in values.items()}
            summary[row['name']].append({'round':index,'baseline_arm':baseline_arm,'candidate_arm':candidate_arm,
                'medians_ms':medians,'candidate_over_base':medians['candidate']/medians['base'],
                'range_ms':{label:[min(samples),max(samples)] for label,samples in values.items()},
                'allocation_bytes':{label:row['stats'][arm]['alloc_bytes'] for label,arm in [('base',baseline_arm),('candidate',candidate_arm)]}})
    return {'base':BASE,'candidate':CANDIDATE,'rounds':3,'samples_per_round':15,'results':summary,
            'limitations':['新进程含启动与输出成本，非暖态','同job顺序不代表独占硬件','三轮各自构建AOT；不与历史runner绝对时间拼接','描述统计不自动判定显著性或无回退']}


if __name__=='__main__':
    root=Path(sys.argv[1]);result=verify(root)
    (root/'study.json').write_text(json.dumps(result,ensure_ascii=False,indent=2))
    for name,rounds in result['results'].items():
        print(name,[(r['round'],round((r['candidate_over_base']-1)*100,3)) for r in rounds],flush=True)
