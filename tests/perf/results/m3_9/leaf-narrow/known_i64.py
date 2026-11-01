# 同调用点已知i64输入，分别保持原函数与重绑另一个函数，锁真实身份失败回退。
import sys
import time

def twice(x):
    return x + x

alias = twice

def other(x):
    return x + 1

def repeat_index(n):
    result = None
    for index in range(n):
        result = alias(index)
    return result

def repeat_constant(n):
    result = None
    for index in range(n):
        result = alias(7)
    return result

validate = '--validate' in sys.argv
count = 20 if validate else 20000
cases = [('index_fast', repeat_index, twice, 2 * (count - 1)),
         ('index_rebind', repeat_index, other, count),
         ('constant_fast', repeat_constant, twice, 14),
         ('constant_rebind', repeat_constant, other, 8)]
for label, function, target, expected in cases:
    alias = target
    result = function(count)
    assert result == expected
    assert type(result).__name__ == 'int'
    print('semantics', label, result)
    if not validate:
        for warm in range(3):
            assert function(count) == expected
        for sample in range(7):
            start = time.perf_counter()
            result = function(count)
            elapsed = time.perf_counter() - start
            assert result == expected
            print('sample', label, elapsed, result)
