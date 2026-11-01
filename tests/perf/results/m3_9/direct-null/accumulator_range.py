# 独立纯range累加诊断：RHS无属性/索引/用户调用，用于区分跨回调活跃值压力。
# 保留无注解动态参数，和原dataclass及六种守卫负载并列，不能替换原性能验收分母。
import sys
import time


def sum_range(initial, n):
    total = initial
    for index in range(n):
        total += index
    return total


validate = '--validate' in sys.argv
count = 20 if validate else 200000
expected = count * (count - 1) // 2
result = sum_range(0, count)
assert result == expected
assert type(result).__name__ == 'int'
# 空循环保原对象；bool/float和溢出仅作边界校验，不冒充正式热路样本。
marker = []
assert sum_range(marker, 0) is marker
assert sum_range(True, 3) == 4
assert sum_range(0.5, 3) == 3.5
assert sum_range(9223372036854775807, 3) == 9223372036854775810
print('semantics', 'range_int', result, type(result).__name__)
if not validate:
    for warm in range(3):
        result = sum_range(0, count)
        assert result == expected
    for repeat in range(7):
        start = time.perf_counter()
        result = sum_range(0, count)
        elapsed = time.perf_counter() - start
        assert result == expected
        assert type(result).__name__ == 'int'
        print('sample', 'range_int', elapsed, result, type(result).__name__)
