"""正确性修复代价的固定36热进程顺序；阈值仅诊断，不撤销正确语义。"""
from statistics import median
import math

BASELINE = '8b15b29b8b9e67086f386f62d22e6d4314b0a9d1'
FIXTURES = ('matmul', 'math_module')


def plan():
    rows = []
    for r in range(3):
        names = FIXTURES if r != 1 else tuple(reversed(FIXTURES))
        arms = ('J', 'T', 'A')[r:] + ('J', 'T', 'A')[:r]
        for ni, name in enumerate(names):
            for ai, backend in enumerate(arms):
                versions = ('B', 'C') if (r + ni + ai) % 2 == 0 else ('C', 'B')
                rows.extend((r, name, v + '-' + backend) for v in versions)
    return rows


def report(records, builds):
    if [(x['repeat'], x['fixture'], x['arm']) for x in records] != plan():
        raise ValueError('36条身份/顺序/数量不符')
    if sorted((x['version'], x['fixture']) for x in builds) != sorted((v,n) for v in ('B','C') for n in FIXTURES):
        raise ValueError('8应用构建不完整')
    results, flags = {}, []
    for name in FIXTURES:
        for backend in ('J','T','A'):
            points = {v: [x for x in records if x['fixture']==name and x['arm']==v+'-'+backend] for v in ('B','C')}
            samples = {v: [x['sample_ms'] for x in xs] for v,xs in points.items()}
            if any(len(s)!=7 or any(not math.isfinite(t) or t<=0 for t in s) for ss in samples.values() for s in ss):
                raise ValueError('每过程要求7个正计时点')
            med = {v:[median(s) for s in ss] for v,ss in samples.items()}
            alloc = {v:[x['stats']['alloc_bytes'] for x in xs] for v,xs in points.items()}
            m = {v:median(xs) for v,xs in med.items()}; a = {v:median(xs) for v,xs in alloc.items()}
            if a['B']<=0: raise ValueError('baseline分配必须正数')
            ratio, aratio = m['C']/m['B'], a['C']/a['B']
            key=name+'-'+backend
            results[key]={'sample_ms':samples,'process_median_ms':med,
                'process_min_ms':{v:[min(s) for s in ss] for v,ss in samples.items()},
                'process_max_ms':{v:[max(s) for s in ss] for v,ss in samples.items()},
                'median_ms':m,'ratio':ratio,'paired_ratio':[c/b for b,c in zip(med['B'],med['C'])],
                'alloc_bytes':alloc,'alloc_median':a,'alloc_ratio':aratio,
                'whole_process_wall_ms':{v:[x['wall_ms'] for x in xs] for v,xs in points.items()},
                'stats':{v:[{k:(None if backend=='A' and k in ('jit_ms','jit_methods') else val)
                    for k,val in x['stats'].items()} for x in xs] for v,xs in points.items()},
                'maxrss_bytes':{v:[x['maxrss_bytes'] for x in xs] for v,xs in points.items()}}
            if all(c>b for b,c in zip(med['B'],med['C'])) and ratio>1.05 and m['C']-m['B']>.1:
                flags.append(key+': 三过程同方向>5%且>0.1ms，需要解释')
            if all(c>b for b,c in zip(alloc['B'],alloc['C'])) and aratio>1.05 and a['C']-a['B']>1048576:
                flags.append(key+': 整进程分配三过程同增>5%且>1MiB，需要解释')
    return {'cases':results,'diagnostic_flags':flags,'adoption_gate':False,
            'interpretation':'正确性代价观测；不得据此恢复错误调用求值语义'}
