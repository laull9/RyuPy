# 同一短算术链的精确float命中、首/末守卫失败与混合输入；输入/实例只在计时外构造。
# 每进程先语义检查和3次完整预热，再7次样本；不把此微负载外推为整项目收益。
import sys
import time


def repeat_mul_add(n, a, b, initial):
    total = initial
    for index in range(n):
        total = a * b + total
    return total


def repeat_norm(n, dx, dy, dz):
    result = None
    for index in range(n):
        result = dx * dx + dy * dy + dz * dz
        dx, dy, dz = dy, dz, dx
    return result


def repeat_mixed(n, values):
    total = 0.5
    for index in range(n):
        value = values[index % 3]
        total = value * 2.0 + total
    return total


class Multiply:
    def __init__(self):
        self.calls = 0

    def __mul__(self, other):
        self.calls += 1
        return 3.0


class AddRight:
    def __init__(self):
        self.calls = 0

    def __radd__(self, other):
        assert other == 3.0
        self.calls += 1
        return self


validate = '--validate' in sys.argv
count = 21 if validate else 30000
multiply = Multiply()
right = AddRight()
mixed = [1.5, True, multiply]
assert repeat_mul_add(0, 1.5, 2.0, right) is right
assert repeat_norm(0, 1.0, 2.0, 3.0) is None


def run(label):
    if label == 'float_hit':
        return repeat_mul_add(count, 1.5, 2.0, 0.5)
    if label == 'norm_hit':
        return repeat_norm(count, 1.0, 2.0, 3.0)
    if label == 'first_bool':
        return repeat_mul_add(count, True, 2.0, 0.5)
    if label == 'first_int':
        return repeat_mul_add(count, 1, 2, 0)
    if label == 'first_custom':
        return repeat_mul_add(count, multiply, 2.0, 0.5)
    if label == 'last_custom':
        return repeat_mul_add(count, 1.5, 2.0, right)
    return repeat_mixed(count, mixed)


def reset():
    multiply.calls = 0
    right.calls = 0


def check(label, result):
    if label == 'float_hit':
        assert result == count * 3.0 + 0.5 and type(result) is float
    elif label == 'norm_hit':
        assert result == 14.0 and type(result) is float
    elif label == 'first_bool':
        assert result == count * 2.0 + 0.5 and type(result) is float
    elif label == 'first_int':
        assert result == count * 2 and type(result) is int
    elif label == 'first_custom':
        assert result == count * 3.0 + 0.5 and multiply.calls == count
    elif label == 'last_custom':
        assert result is right and right.calls == count
    else:
        assert result == (count // 3) * 8.0 + 0.5
        assert multiply.calls == count // 3
    return True


for label in ['float_hit', 'norm_hit', 'first_bool', 'first_int', 'first_custom', 'last_custom', 'mixed']:
    reset()
    assert check(label, run(label))
    print('semantics', label, True)
    if not validate:
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
