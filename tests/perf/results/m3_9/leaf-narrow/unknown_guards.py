# 同一调用点的成功/类型失败/溢出/替换函数守卫；所有输入在计时外，计数与身份均验证。
import sys
import time

def twice(x):
    return x + x

alias = twice

def repeat(n, value):
    result = None
    for index in range(n):
        result = alias(value)
    return result

class Addable:
    def __init__(self):
        self.calls = 0
    def __add__(self, other):
        assert other is self
        self.calls += 1
        return self

def replacement(x):
    return x + 1

validate = '--validate' in sys.argv
count = 20 if validate else 20000
custom = Addable()
cases = [('int', 7, 14), ('bool', True, 2), ('float', 0.5, 1.0),
         ('overflow', 9223372036854775807, 18446744073709551614),
         ('custom', custom, custom), ('rebind', 3, 4)]

def check(label, result, expected):
    if label == 'custom':
        assert result is custom
        assert custom.calls == count
    else:
        assert result == expected
        assert type(result) is type(expected)
    return True

for label, value, expected in cases:
    alias = replacement if label == 'rebind' else twice
    custom.calls = 0
    result = repeat(count, value)
    print('semantics', label, check(label, result, expected))
    if not validate:
        for warm in range(3):
            custom.calls = 0
            result = repeat(count, value)
            check(label, result, expected)
        for sample in range(7):
            custom.calls = 0
            start = time.perf_counter()
            result = repeat(count, value)
            elapsed = time.perf_counter() - start
            print('sample', label, elapsed, check(label, result, expected))
