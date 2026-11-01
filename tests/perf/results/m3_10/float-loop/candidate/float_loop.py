# 相同内层循环的raw浮点累加与慢路身份对照；外层range(1)避开旧entry-only holder。
# 输入与复用实例计时外建立；不顺带消除动态getitem的下标盒，不把微负载外推整项目。
import sys
import time


def accumulate(values):
    for outer in range(1):
        total = 0.0
        for index in range(len(values)):
            total += values[index] * 1.0
    return total


def repeated(rows):
    results = []
    for row_index in range(len(rows)):
        values = rows[row_index]
        total = 0.0
        for index in range(len(values)):
            total += values[index] * 1.0
        results.append(total)
    return results


class Release:
    def __init__(self):
        self.calls = 0

    def __mul__(self, other):
        self.calls += 1
        return self


class Bucket:
    def __init__(self):
        self.total = 0.0
        self.calls = 0

    def __iadd__(self, other):
        self.calls += 1
        if isinstance(other, Release):
            return shared
        self.total += other
        return self


class Token:
    def __init__(self):
        self.mul_calls = 0
        self.add_calls = 0
        self.last_left = None

    def __mul__(self, other):
        self.mul_calls += 1
        return self

    def __radd__(self, left):
        self.add_calls += 1
        self.last_left = left
        bucket.total = left + 1.0
        return bucket


class SharedFloat:
    def __init__(self):
        self.mul_calls = 0
        self.add_calls = 0
        self.last_left = None

    def __mul__(self, other):
        self.mul_calls += 1
        return self

    def __radd__(self, left):
        self.add_calls += 1
        self.last_left = left
        return shared


validate = '--validate' in sys.argv
# 独立资源/JIT进程保完整输入规模，但不在模块初始化时先跑正式时间样本。
diagnostic = '--diagnostic' in sys.argv
count = 24 if validate else 30000
shared = 1000.25
bucket = Bucket()
token = Token()
shared_token = SharedFloat()
release = Release()
zero_reference = accumulate([])
positions = [0, count // 2, count - 1]
labels = ['first', 'middle', 'last']
inputs = {'float': [0.25] * count, 'empty': [], 'bool': [True] * count}
for index in range(3):
    values = [0.25] * count
    values[positions[index]] = token
    inputs['custom_' + labels[index]] = values
    values = [0.25] * count
    values[positions[index]] = shared_token
    inputs['shared_' + labels[index]] = values
inputs['mixed_restore'] = [token, 0.25, release, 0.25] * (count // 4)
rows = [[0.25] * (count // 3), [], [shared_token],
        [0.25] * (count // 3), [], [0.25] * (count // 3)]


def reset():
    bucket.total = 0.0
    bucket.calls = 0
    token.mul_calls = 0
    token.add_calls = 0
    token.last_left = None
    shared_token.mul_calls = 0
    shared_token.add_calls = 0
    shared_token.last_left = None
    release.calls = 0


def run(label):
    if label == 'reentry':
        return repeated(rows)
    return accumulate(inputs[label])


def check(label, result):
    if label == 'float':
        assert result == count * 0.25 and type(result) is float
    elif label == 'empty':
        assert result is zero_reference and result == 0.0
    elif label == 'bool':
        assert result == float(count) and type(result) is float
    elif label == 'reentry':
        expected = (count // 3) * 0.25
        assert result == [expected, 0.0, shared, expected, 0.0, expected]
        assert result[1] is result[4] and result[2] is shared
        assert shared_token.mul_calls == 1 and shared_token.add_calls == 1
    elif label == 'mixed_restore':
        assert result == shared + 0.25 and type(result) is float
        assert result is not shared
        assert token.mul_calls == count // 4 and token.add_calls == count // 4
        assert release.calls == count // 4 and bucket.calls == count // 2
    else:
        for index in range(3):
            position = positions[index]
            if label == 'custom_' + labels[index]:
                assert result is bucket
                assert bucket.total == (count - 1) * 0.25 + 1.0
                assert bucket.calls == count - position - 1
                assert token.mul_calls == 1 and token.add_calls == 1
                assert token.last_left == position * 0.25
            elif label == 'shared_' + labels[index]:
                assert result == shared + (count - position - 1) * 0.25
                assert (result is shared) == (position == count - 1)
                assert shared_token.mul_calls == 1 and shared_token.add_calls == 1
                assert shared_token.last_left == position * 0.25
    return True


for label in ['float', 'empty', 'bool', 'reentry', 'custom_first', 'custom_middle', 'custom_last',
              'shared_first', 'shared_middle', 'shared_last', 'mixed_restore']:
    reset()
    assert check(label, run(label))
    print('semantics', label, True)
    if not validate and not diagnostic:
        for warm in range(3):
            reset()
            assert check(label, run(label))
        for sample in range(7):
            reset()
            start = time.perf_counter()
            result = run(label)
            elapsed = time.perf_counter() - start
            assert check(label, result)
            print('sample', label, elapsed, True)
