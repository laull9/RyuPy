"""直接wall与带统计诊断分离的固定测量协议，不包含编译器或provider实现。"""
import itertools
import json
import math
from pathlib import Path
import statistics

import perf_schema

ARMS = ('cpython', 'jit', 'aot')
ORDERS = tuple(itertools.permutations(ARMS))
ROUNDS = len(ORDERS)


def direct_commands(python, dotnet, source, managed, native):
    """CP原文件与JIT/AOT公开入口，不加入Python统计外壳。"""
    return {'cpython': [str(python), '-B', str(source)],
            'jit': [str(dotnet), str(managed)], 'aot': [str(native)]}


def schedule(cases):
    for round_index in range(ROUNDS):
        order = cases[round_index % len(cases):] + cases[:round_index % len(cases)]
        for case in order:
            for arm in ORDERS[(round_index + cases.index(case)) % ROUNDS]:
                yield round_index, case, arm


def validate_output(result, expected):
    if result['exit'] != 0 or result['stdout'] != expected[0] or result['stderr'] != expected[1]:
        raise ValueError('退出码或双流不等于本次直接CPython预期')


def direct_record(result, case, arm, round_index):
    if 'stats_sha256' in result or (Path(result['case'])/'stats.json').exists():
        raise ValueError('直接wall样本被统计输出污染')
    value=result['wall_ms']
    if type(value) not in (int,float) or not math.isfinite(value) or value < 0:
        raise ValueError('无效wall样本')
    record={key:value for key,value in result.items() if key not in ('stdout','stderr')}
    record.update(workload=case, arm=arm, round=round_index)
    return record


def collect_direct(runner, products, cases, root, env):
    if 'RYUPY_STATS' in env: raise ValueError('直接wall环境不得启用统计')
    expected={}; records=[]
    # 预运行不是进程内预热；直接CP输出同时是即时oracle，不另造golden。
    for case in cases:
        for arm in ARMS:
            result=runner.run(f'pre-{case}-{arm}', products[case][arm], cwd=root,env=env,limit=120)
            if arm=='cpython':expected[case]=(result['stdout'],result['stderr'])
            validate_output(result,expected[case]);direct_record(result,case,arm,-1)
    for round_index,case,arm in schedule(cases):
        result=runner.run(f'wall-{round_index}-{case}-{arm}',products[case][arm],cwd=root,env=env,limit=120)
        validate_output(result,expected[case])
        records.append(direct_record(result,case,arm,round_index))
    return expected,records


def summary(records, cases):
    if len(records)!=len(cases)*len(ARMS)*ROUNDS:raise ValueError('直接样本总数不符')
    result={}
    for case in cases:
        result[case]={}
        for arm in ARMS:
            rows=[row for row in records if row['workload']==case and row['arm']==arm]
            if len(rows)!=ROUNDS or {row['round'] for row in rows}!=set(range(ROUNDS)):
                raise ValueError('样本缺失/重复轮次')
            values=[row['wall_ms'] for row in rows]
            result[case][arm]={'wall_ms':values,'median_ms':statistics.median(values),
                               'min_ms':min(values),'max_ms':max(values)}
    return result


def diagnostic_stats(path, arm):
    if arm!='cpython':return perf_schema.stats(path)
    data=json.loads(Path(path).read_text(),object_pairs_hook=perf_schema.pairs,parse_constant=perf_schema.constant)
    if not isinstance(data,dict) or set(data)!={'main_ms','gc0','gc1','gc2'}:
        raise ValueError('CP诊断统计缺失或多余字段')
    for key,value in data.items():
        if type(value) not in (int,float) or not math.isfinite(value) or value < 0 or (key!='main_ms' and value!=int(value)):
            raise ValueError('CP诊断统计值无效')
    return data
