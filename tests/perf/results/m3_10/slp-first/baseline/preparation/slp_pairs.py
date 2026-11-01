# 两lane同构独立算术链的fresh容器输出；第三元素是未配对scalar尾项。
# 普通Python源码不显式调用SIMD；输入/计数复位/结果校验在计时外。
import sys
import time


def list_pairs(n, a, b, c, d):
    result = []
    for index in range(n):
        result = [((a * b + c) * b + c) * b + c, ((d * b + c) * b + c) * b + c]
    return result


def tuple_pairs(n, a, b, c, d):
    result = ()
    for index in range(n):
        result = (((a * b + c) * b + c) * b + c, ((d * b + c) * b + c) * b + c)
    return result


def tail_pairs(n, a, b, c, d):
    result = []
    for index in range(n):
        result = [((a * b + c) * b + c) * b + c, ((d * b + c) * b + c) * b + c, a - d]
    return result


class Marker:
    def __init__(self, value):
        self.value = value
        self.calls = 0

    def __mul__(self, value):
        self.calls += 1
        return self.value * value


validate = '--validate' in sys.argv
diagnostic = '--diagnostic' in sys.argv
count = 21 if validate else 30000
first = Marker(1.25)
last = Marker(2.25)


def reset():
    first.calls = 0
    last.calls = 0


def run(label):
    if label == 'float_tuple':
        return tuple_pairs(count, 1.25, 0.5, 0.25, 2.25)
    if label == 'scalar_tail':
        return tail_pairs(count, 1.25, 0.5, 0.25, 2.25)
    if label == 'first_bool':
        return list_pairs(count, True, 0.5, 0.25, 2.25)
    if label == 'last_bool':
        return list_pairs(count, 1.25, 0.5, 0.25, True)
    if label == 'first_custom':
        return list_pairs(count, first, 0.5, 0.25, 2.25)
    if label == 'last_custom':
        return list_pairs(count, 1.25, 0.5, 0.25, last)
    return list_pairs(count, 1.25, 0.5, 0.25, 2.25)


def check(label, result):
    expected_first = 0.5625 if label == 'first_bool' else 0.59375
    expected_last = 0.5625 if label == 'last_bool' else 0.71875
    assert result[0] == expected_first and type(result[0]) is float
    assert result[1] == expected_last and type(result[1]) is float
    if label == 'float_tuple':
        assert type(result) is tuple
    else:
        assert type(result) is list
    if label == 'scalar_tail':
        assert len(result) == 3 and result[2] == -1.0
    else:
        assert len(result) == 2
    assert first.calls == (count if label == 'first_custom' else 0)
    assert last.calls == (count if label == 'last_custom' else 0)
    return True


for label in ['float_list', 'float_tuple', 'scalar_tail', 'first_bool', 'last_bool', 'first_custom', 'last_custom']:
    reset()
    assert check(label, run(label))
    print('semantics', label, True)
    if not validate and not diagnostic:
        for warmup in range(3):
            reset()
            assert check(label, run(label))
        for sample in range(7):
            reset()
            start = time.perf_counter()
            result = run(label)
            elapsed = time.perf_counter() - start
            assert check(label, result)
            print('sample', label, elapsed, True)
