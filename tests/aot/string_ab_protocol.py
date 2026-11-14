"""固定同机五臂A/B：十轮正反循环平衡顺序，完整双流即时CP对照。"""
import statistics
from direct_protocol import direct_record, validate_output

ARMS = ('cpython', 'base_jit', 'candidate_jit', 'base_aot', 'candidate_aot')
CASES = ('append_local', 'append_module', 'join_local', 'join_module', 'numeric_local')
ROUNDS = 10


def schedule():
    # 每臂每个位置各两次，正向/反向降低固定前驱影响；不自适应追加。
    for iteration in range(ROUNDS):
        cases = CASES[iteration % len(CASES):] + CASES[:iteration % len(CASES)]
        arms = ARMS if iteration < 5 else ARMS[::-1]
        offset = iteration % len(ARMS)
        arms = arms[offset:] + arms[:offset]
        for case in cases:
            for arm in arms:
                yield iteration, case, arm


def collect(runner, products, cwd, env, save):
    if 'RYUPY_STATS' in env:
        raise ValueError('直接计时不得继承统计')
    expected, records = {}, []
    for case in CASES:
        for arm in ARMS:
            result = runner.run(f'pre-{case}-{arm}', products[case][arm], cwd=cwd, env=env, limit=120)
            if arm == 'cpython':
                expected[case] = result['stdout'], result['stderr']
            validate_output(result, expected[case])
            direct_record(result, case, arm, -1)
    for iteration, case, arm in schedule():
        result = runner.run(f'wall-{iteration}-{case}-{arm}', products[case][arm], cwd=cwd, env=env, limit=120)
        validate_output(result, expected[case])
        records.append(direct_record(result, case, arm, iteration))
        save(records)  # 中断亦保留已完成样本，不据此生成成功结果。
    return expected, records


def summarize(records):
    expected = {(r, c, a) for r, c, a in schedule()}
    keys = [(x['round'], x['workload'], x['arm']) for x in records]
    if len(keys) != len(expected) or set(keys) != expected:
        raise ValueError('样本缺失/重复/额外项')
    result = {}
    for case in CASES:
        result[case] = {}
        for arm in ARMS:
            rows = sorted((x for x in records if x['workload'] == case and x['arm'] == arm), key=lambda x:x['round'])
            values = [x['wall_ms'] for x in rows]
            result[case][arm] = dict(samples_ms=values, median_ms=statistics.median(values), min_ms=min(values), max_ms=max(values))
    return result
