# 相同候选函数的守卫命中/频繁回退负载；数据和实例在计时外建立，不能以不同函数替代快慢路径。
# --validate 仅运行短语义验收，不采集时间；正式比较由独立脚本隔离进程和tier。
import sys
import time


def accumulate(initial, values):
    total = initial
    # 保持现有优化器的严格range候选形状；索引/读取成本在所有场景及两变体中一致。
    for index in range(len(values)):
        total += values[index]
    return total


class ReverseAdd:
    def __init__(self):
        self.calls = 0

    def __radd__(self, other):
        self.calls += 1
        return other + 1


class MutableAdd:
    def __init__(self):
        self.calls = 0
        self.total = 0

    def __iadd__(self, value):
        self.calls += 1
        self.total += value
        return self


validate = '--validate' in sys.argv
count = 20 if validate else 20000
reverse = ReverseAdd()
mutable = MutableAdd()
maximum = 9223372036854775807
cases = [
    ('int_fast', 0, [1] * count, None, count, 'int'),
    ('bool_fallback', 0, [True] * count, None, count, 'int'),
    ('float_fallback', 0, [0.5] * count, None, count / 2.0, 'float'),
    ('radd_fallback', 0, [reverse] * count, reverse, count, 'int'),
    ('iadd_fallback', mutable, [1] * count, mutable, count, 'MutableAdd'),
    ('overflow_alternating', maximum, [1, -1] * (count // 2), None, maximum, 'int'),
]


def reset(counter):
    if counter is not None:
        counter.calls = 0
    if counter is mutable:
        mutable.total = 0


def checked(label, result, counter, expected, expected_type):
    actual = result.total if label == 'iadd_fallback' else result
    calls = 0 if counter is None else counter.calls
    identity = result is mutable if label == 'iadd_fallback' else True
    assert actual == expected
    assert type(result).__name__ == expected_type
    assert calls == (count if counter is not None else 0)
    assert identity
    return actual, type(result).__name__, calls, identity


for label, initial, values, counter, expected, expected_type in cases:
    reset(counter)
    result = accumulate(initial, values)
    actual, kind, calls, identity = checked(label, result, counter, expected, expected_type)
    print('semantics', label, actual, kind, calls, identity)
    if not validate:
        for warm in range(3):
            reset(counter)
            result = accumulate(initial, values)
            checked(label, result, counter, expected, expected_type)
        for repeat in range(7):
            reset(counter)
            start = time.perf_counter()
            result = accumulate(initial, values)
            elapsed = time.perf_counter() - start
            actual, kind, calls, identity = checked(label, result, counter, expected, expected_type)
            print('sample', label, elapsed, actual, kind, calls, identity)
