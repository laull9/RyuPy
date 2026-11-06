# count 有限数值语义应用。
# count 公开应用二：真实递增ID、i64上下界、任意精度、bool首项和零步无限性。
from itertools import count


def take(iterator, size):
    result = []
    for _ in range(size):
        result.append(next(iterator))
    return result


ids = count(100, 3)
assert take(ids, 4) == [100, 103, 106, 109]
assert next(ids) == 112
assert repr(ids) == 'count(115, 3)'
assert take(count(5, -2), 5) == [5, 3, 1, -1, -3]
assert take(count(7, 0), 6) == [7, 7, 7, 7, 7, 7]
upper = 9223372036854775807
lower = -9223372036854775808
assert take(count(upper - 1), 4) == [upper - 1, upper, upper + 1, upper + 2]
assert take(count(upper - 1, 3), 3) == [upper - 1, upper + 2, upper + 5]
assert take(count(lower, -2), 3) == [lower, lower - 2, lower - 4]
assert take(count(upper + 3, -2), 4) == [upper + 3, upper + 1, upper - 1, upper - 3]
huge = 10**80
step = 10**70
assert take(count(huge, step), 3) == [huge, huge + step, huge + 2 * step]
assert take(count(-huge, -step), 3) == [-huge, -huge - step, -huge - 2 * step]
assert take(count(3, huge), 3) == [3, 3 + huge, 3 + 2 * huge]
print('C2.integer.promotion')

for start, step in [(True, 1), (False, True), (True, True), (1, True)]:
    iterator = count(start, step)
    first = next(iterator)
    assert type(first) is int
    assert first == int(start)
    assert repr(iterator) == 'count(' + str(int(start) + 1) + ')'
for start, step in [(True, False), (False, 2), (True, -1), (True, 1.0)]:
    iterator = count(start, step)
    first = next(iterator)
    assert first is start
    assert type(first) is bool
    second = next(iterator)
    assert second == start + step
    assert type(second) is float if type(step) is float else type(second) is int
assert repr(count(5, True)) == 'count(5)'
assert repr(count(True, False)) == 'count(True, False)'
print('C2.bool.state')
