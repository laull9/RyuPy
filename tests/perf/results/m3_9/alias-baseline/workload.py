# 无注解叶函数的模块别名/静态解析调用对照；同形循环和累加结果，不假设别名调用必经binder。
# --validate仅校验语义；真实调用路径先用保存CLI的MIR/CIL确认，当前不作性能承诺。
import sys
import time


def twice(x):
    return x + x


alias = twice


def call_alias(n):
    total = 0
    for index in range(n):
        total += alias(index)
    return total


def call_direct(n):
    total = 0
    for index in range(n):
        total += twice(index)
    return total


validate = '--validate' in sys.argv
count = 20 if validate else 20000
expected = count * (count - 1)
assert alias is twice
assert call_alias(0) == 0
assert call_direct(0) == 0
for label, function in [('alias', call_alias), ('direct', call_direct)]:
    result = function(count)
    assert result == expected
    assert type(result).__name__ == 'int'
    print('semantics', label, result)
    if not validate:
        for warm in range(3):
            assert function(count) == expected
        for repeat in range(7):
            start = time.perf_counter()
            result = function(count)
            elapsed = time.perf_counter() - start
            assert result == expected
            assert type(result).__name__ == 'int'
            print('sample', label, elapsed, result)
