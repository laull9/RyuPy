"""原生模块懒属性启动配对的固定顺序、逐条校验与事前采用阈值。"""
import math
from statistics import median

BASELINE = '972fac072c2dbdd42e250f633a9e21b3d09cbb62'
FIXTURES = ('hello', 'import_typing', 'import_re', 'rebind')
ORDERS = (
    ('B-J', 'C-J', 'C-T', 'B-T', 'B-A', 'C-A'),
    ('C-T', 'B-T', 'B-A', 'C-A', 'C-J', 'B-J'),
    ('B-A', 'C-A', 'C-J', 'B-J', 'C-T', 'B-T'),
    ('C-J', 'B-J', 'B-T', 'C-T', 'C-A', 'B-A'),
    ('B-T', 'C-T', 'C-A', 'B-A', 'B-J', 'C-J'),
)


def plan():
    return [(r, name, arm) for r, order in enumerate(ORDERS)
            for name in FIXTURES for arm in order]


def output(raw, expected):
    if raw != expected.encode('utf-8'):
        raise ValueError('逐字stdout与冻结CP期望不同')


def report(records, builds):
    if [(r['repeat'], r['fixture'], r['arm']) for r in records] != plan():
        raise ValueError('120条顺序/数量/身份不符')
    if sorted((b['version'], b['fixture']) for b in builds) != sorted(
            (v, n) for v in ('B', 'C') for n in FIXTURES):
        raise ValueError('16次应用构建不完整/重复')
    results, blocking, review = {}, [], []
    for name in FIXTURES:
        for backend in ('J', 'T', 'A'):
            points = {v: [r for r in records if r['fixture'] == name
                          and r['arm'] == v + '-' + backend] for v in ('B', 'C')}
            walls = {v: [r['wall_ms'] for r in points[v]] for v in points}
            alloc = {v: [r['stats']['alloc_bytes'] for r in points[v]] for v in points}
            wm = {v: median(walls[v]) for v in points}
            am = {v: median(alloc[v]) for v in points}
            if wm['B'] <= 0 or am['B'] <= 0:
                raise ValueError('baseline主指标不可为零')
            wr, ar = wm['C'] / wm['B'], am['C'] / am['B']
            key = name + '-' + backend
            results[key] = {'wall_ms': walls, 'wall_median': wm, 'wall_ratio': wr,
                            'wall_min': {v: min(walls[v]) for v in points},
                            'wall_max': {v: max(walls[v]) for v in points},
                            'alloc_bytes': alloc, 'alloc_median': am, 'alloc_ratio': ar,
                            'alloc_min': {v: min(alloc[v]) for v in points},
                            'alloc_max': {v: max(alloc[v]) for v in points},
                            'paired_wall_ratio': [c / b for b, c in zip(walls['B'], walls['C'])],
                            'aot_jit_metrics': None if backend == 'A' else 'see raw stats'}
            if all(c > b for b, c in zip(walls['B'], walls['C'])) and wr > 1.05 and wm['C'] - wm['B'] > 1:
                blocking.append(key + ': 五点同退且>5%及>1ms')
            if ar > 1.05:
                review.append(key + ': alloc增长>5%')
    for backend in ('J', 'T', 'A'):
        if results['hello-' + backend]['alloc_ratio'] > .8:
            blocking.append('hello-' + backend + ': 分配未下降20%')
    for backend in ('J', 'A'):
        if results['hello-' + backend]['wall_ratio'] > .95:
            blocking.append('hello-' + backend + ': wall未下降5%')
    geomean = {b: math.exp(sum(math.log(results[n + '-' + b]['wall_ratio'])
                               for n in FIXTURES) / len(FIXTURES)) for b in ('J', 'T', 'A')}
    review.extend(b + ': wall几何比值>1.03' for b, value in geomean.items() if value > 1.03)
    for name in FIXTURES:
        sizes = {b['version']: b['native_file_bytes'] for b in builds if b['fixture'] == name}
        if sizes['C'] > sizes['B'] * 1.01:
            review.append(name + ': native文件增长>1%')
    return {'cases': results, 'wall_geomean': geomean, 'blocking': blocking, 'review': review,
            'adoption': 'manual-review-required; not automatic approval'}
