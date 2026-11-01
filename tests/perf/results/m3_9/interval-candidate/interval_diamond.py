# 有限区间专项：动态函数的3/7分支合流后+2；函数调用/循环内分支两形，未知值保原语义。
import sys
import time

def choose(flag):
    if flag:
        value = 3
    else:
        value = 7
    return value + 2

def call_diamond(n, flag):
    total = 0
    for index in range(n):
        total += choose(flag)
    return total

def loop_diamond(n, flag):
    total = 0
    for index in range(n):
        if flag:
            value = 3
        else:
            value = 7
        total += value + 2
    return total

def choose_unknown(flag, argument):
    if flag:
        value = argument
    else:
        value = 7
    return value + 2

def unknown_loop(n, argument):
    total = 0
    for index in range(n):
        total += choose_unknown(True, argument)
    return total

validate = '--validate' in sys.argv
count = 20 if validate else 20000
cases = [('call_true', call_diamond, True, count * 5),
         ('call_false', call_diamond, False, count * 9),
         ('loop_true', loop_diamond, True, count * 5),
         ('loop_false', loop_diamond, False, count * 9),
         ('unknown_int', unknown_loop, 11, count * 13),
         ('unknown_bool', unknown_loop, True, count * 3),
         ('unknown_overflow', unknown_loop, 9223372036854775807, count * 9223372036854775809)]
assert choose(True) == 5 and choose(False) == 9
assert choose_unknown(False, None) == 9
assert call_diamond(0, True) == 0 and loop_diamond(0, False) == 0
for label, function, argument, expected in cases:
    result = function(count, argument)
    assert result == expected
    assert type(result).__name__ == 'int'
    print('semantics', label, result)
    if not validate:
        for warm in range(3):
            assert function(count, argument) == expected
        for sample in range(7):
            start = time.perf_counter()
            result = function(count, argument)
            elapsed = time.perf_counter() - start
            assert result == expected
            print('sample', label, elapsed, result)
