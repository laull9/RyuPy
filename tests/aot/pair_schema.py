"""冻结180进程配对计划、guard逐条校验及事前阈值报告；不触发追加测量。"""
import math
import statistics

import perf_schema

BASELINE = '457886017cd4d32911a09b09307d07c327b87192'
KERNELS = ('matmul', 'calls', 'dataclass', 'exceptions', 'hello')
BACKENDS = ('J', 'T', 'A')
ORDERS = (('B-J', 'C-J', 'C-T', 'B-T', 'B-A', 'C-A'),
          ('C-T', 'B-T', 'B-A', 'C-A', 'C-J', 'B-J'),
          ('B-A', 'C-A', 'C-J', 'B-J', 'B-T', 'C-T'))
GUARDS = {'float': '7.5', 'bool': '6', 'custom': '(True, 100000, 100000, False)',
          'overflow': '9223372036854775807'}


def plan():
    result = []
    for phase, kernels in (('cold', KERNELS), ('hot', KERNELS[:-1]), ('guard', ('guard',))):
        for repeat, arms in enumerate(ORDERS):
            offset = repeat % len(kernels)
            for kernel in kernels[offset:] + kernels[:offset]:
                for arm in arms:
                    result.append((phase, repeat, kernel, arm))
    return result


def guard_output(raw):
    lines = raw.decode('utf-8', errors='strict').splitlines()
    if len(lines) != 40:
        raise ValueError('guard必须恰四场景×(3warm+7sample)')
    samples = {}
    for offset, (name, checksum) in enumerate(GUARDS.items()):
        block = lines[offset * 10:(offset + 1) * 10]
        for index, line in enumerate(block[:3]):
            if line != f'warm {name} {index} {checksum}':
                raise ValueError('guard warm次数/顺序/identity校验失败')
        values = []
        for index, line in enumerate(block[3:]):
            parts = line.split(' ', 4)
            if len(parts) != 5 or parts[:3] != ['sample', name, str(index)] or parts[4] != checksum:
                raise ValueError('guard sample次数/顺序/checksum校验失败')
            value = float(parts[3])
            if not math.isfinite(value) or value < 0:
                raise ValueError('guard时长必须有限非负')
            values.append(value)
        samples[name] = values
    return samples


def rows(records, kernel, phase, arm):
    found = sorted((r for r in records if (r['kernel'], r['phase'], r['arm']) ==
                    (kernel, phase, arm)), key=lambda r: r['repeat'])
    if [r['repeat'] for r in found] != [0, 1, 2]:
        raise ValueError('缺失/重复独立进程')
    return found


def ratio(before, after):
    if before <= 0:
        raise ValueError('基准统计零值不能形成相对准入证据')
    return after / before


def report(records, builds):
    if [(r['phase'], r['repeat'], r['kernel'], r['arm']) for r in records] != plan():
        raise ValueError('完整180进程必须符合冻结轮转计划')
    result = {'hot': {}, 'calls_allocation': {}, 'cold': {}, 'native_size': {},
              'blocking_thresholds': [], 'review_thresholds': [],
              'code_size_review': 'pending-static-disassembly',
              'decision': 'report-only-no-automatic-rerun-or-default-approval'}
    for backend in BACKENDS:
        for name in (*KERNELS[:-1], *GUARDS):
            phase, kernel = ('guard', 'guard') if name in GUARDS else ('hot', name)
            groups = [rows(records, kernel, phase, version + '-' + backend) for version in ('B', 'C')]
            medians = [[statistics.median(r['guard_samples'][name] if phase == 'guard' else r['samples'])
                        for r in group] for group in groups]
            change = ratio(statistics.median(medians[0]), statistics.median(medians[1]))
            all_slower = all(c > b for b, c in zip(*medians))
            key = name + '-' + backend
            result['hot'][key] = {'process_medians': medians, 'candidate_baseline_ratio': change,
                                  'all_three_slower': all_slower,
                                  'interpretation': 'fixed warmup; not proof of steady Tier1'}
            blocked = (name == 'calls' and backend in ('T', 'A') and change > 0.95)
            blocked |= all_slower and change > 1.05
            if blocked:
                result['blocking_thresholds'].append('hot:' + key)
        alloc = [statistics.median(r['stats']['alloc_bytes'] for r in
                 rows(records, 'calls', 'hot', version + '-' + backend)) for version in ('B', 'C')]
        result['calls_allocation'][backend] = {'process_bytes': alloc, 'ratio': ratio(*alloc)}
        if ratio(*alloc) > 0.8:
            result['blocking_thresholds'].append('calls-allocation:' + backend)
        changes = []
        for name in KERNELS:
            wall = [statistics.median(r['wall_ms'] for r in rows(records, name, 'cold', v + '-' + backend))
                    for v in ('B', 'C')]
            changes.append(ratio(*wall))
            result['cold'][name + '-' + backend] = {'wall_median_ms': wall, 'ratio': ratio(*wall)}
        geometric = math.exp(sum(math.log(value) for value in changes) / len(changes))
        result['cold']['geometric-' + backend] = geometric
        if geometric > 1.05:
            result['review_thresholds'].append('cold-geometric:' + backend)
    for name in (*KERNELS, 'guard'):
        sizes = [next(b['native_file_bytes'] for b in builds if b['version'] == v and b['kernel'] == name)
                 for v in ('B', 'C')]
        result['native_size'][name] = {'bytes': sizes, 'ratio': ratio(*sizes)}
        if ratio(*sizes) > 1.01:
            result['review_thresholds'].append('native-size:' + name)
    return result
