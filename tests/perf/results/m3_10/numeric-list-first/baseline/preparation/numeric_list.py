# NumericList 三臂对照：普通引用路径与 fresh-append 浮点生产/消费。
# 输入构造、计数复位与身份校验在计时外；正式矩阵另用原100²三次kernel。
import sys
import time


class Marker:
    def __init__(self):
        self.calls = 0

    def __mul__(self, value):
        self.calls += 1
        return 0.5


class Reference:
    pass


def construct_refs(n, value):
    return [value] * n


def append_refs(n, value):
    result = []
    for index in range(n):
        result.append(value)
    return result


def read_refs(values, expected):
    count = 0
    for index in range(len(values)):
        if values[index] is expected:
            count += 1
    return count


def produce_consume(values, factors, result_factors):
    result = []
    for outer in range(2):
        total = 0.0
        for index in range(len(values)):
            total += values[index] * factors[index]
        result.append(total)
    output = []
    # 两个候选岛采用独立累加槽；这是命中正例，不代表任意源形状命中率。
    for outer in range(1):
        consumed = 0.0
        for index in range(len(result)):
            consumed += result[index] * result_factors[index]
        output.append(consumed)
    return output[0]


validate = '--validate' in sys.argv
diagnostic = '--diagnostic' in sys.argv
count = 24 if validate else 30000
reference = Reference()
marker = Marker()
refs = [reference] * count
factors = [1.0] * count
result_factors = [1.0, 1.0]
floats = [0.25] * count
bools = [True] * count
custom = [marker] * count
mixed = [0.25, marker] * (count // 2)


def run(label):
    if label == 'construct_refs':
        return construct_refs(count, reference)
    if label == 'append_refs':
        return append_refs(count, reference)
    if label == 'read_refs':
        return read_refs(refs, reference)
    if label == 'float':
        return produce_consume(floats, factors, result_factors)
    if label == 'bool':
        return produce_consume(bools, factors, result_factors)
    if label == 'custom':
        return produce_consume(custom, factors, result_factors)
    return produce_consume(mixed, factors, result_factors)


def check(label, result):
    if label == 'construct_refs' or label == 'append_refs':
        assert len(result) == count
        for item in result:
            assert item is reference
    elif label == 'read_refs':
        assert result == count
    elif label == 'float':
        assert result == count * 0.5
    elif label == 'bool':
        assert result == count * 2.0
    elif label == 'custom':
        assert result == float(count) and marker.calls == count * 2
    else:
        assert result == count * 0.75 and marker.calls == count
    return True


for label in ['construct_refs', 'append_refs', 'read_refs', 'float', 'bool', 'custom', 'mixed']:
    marker.calls = 0
    assert check(label, run(label))
    print('semantics', label, True)
    if not validate and not diagnostic:
        for warmup in range(3):
            marker.calls = 0
            assert check(label, run(label))
        for sample in range(7):
            marker.calls = 0
            start = time.perf_counter()
            result = run(label)
            elapsed = time.perf_counter() - start
            assert check(label, result)
            print('sample', label, elapsed, True)
