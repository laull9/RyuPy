# count 有限数值语义应用。
# count 公开应用三：逐次浮点加法、混合首项、NaN/inf/有符号零及溢出重试。
from itertools import count


def take(iterator, size):
    result = []
    for _ in range(size):
        result.append(next(iterator))
    return result


sample = take(count(0.0, 0.1), 12)
expected = []
current = 0.0
for _ in range(12):
    expected.append(current)
    current = current + 0.1
assert sample == expected
assert sample[6] == 0.6
assert sample[6] != 6 * 0.1
assert sample[10] == 0.9999999999999999
assert sample[10] != 10 * 0.1
mixed = count(1, 0.5)
assert type(next(mixed)) is int
assert type(next(mixed)) is float
assert next(mixed) == 2.0
assert repr(mixed) == 'count(2.5, 0.5)'
mixed = count(1.5, 2)
assert take(mixed, 3) == [1.5, 3.5, 5.5]
assert repr(mixed) == 'count(7.5, 2)'
assert take(count(float(2**53), 1.0), 3) == [9007199254740992.0] * 3
assert take(count(2**100, 0.5), 3) == [2**100, float(2**100), float(2**100)]
print('C3.float.accumulation')

nan = float('nan')
inf = float('inf')
values = take(count(3, nan), 3)
assert values[0] == 3 and values[1] != values[1] and values[2] != values[2]
values = take(count(nan, 1), 3)
assert values[0] is nan
assert values[0] != values[0] and values[1] != values[1]
assert take(count(inf, 1), 3) == [inf, inf, inf]
values = take(count(inf, -inf), 3)
assert values[0] is inf and values[1] != values[1]
assert take(count(1e308, 1e308), 3) == [1e308, inf, inf]
assert [repr(value) for value in take(count(-0.0, 0), 3)] == ['-0.0', '0.0', '0.0']
assert [repr(value) for value in take(count(-0.0, -0.0), 3)] == ['-0.0', '-0.0', '-0.0']
assert [repr(value) for value in take(count(0.0, -0.0), 3)] == ['0.0', '0.0', '0.0']
print('C3.float.special')

for iterator in [count(10**400, 1.0), count(1.0, 10**400)]:
    before = repr(iterator)
    for _ in range(2):
        try:
            next(iterator, 'must not hide overflow')
            assert False
        except OverflowError as error:
            assert str(error) == 'int too large to convert to float'
        assert repr(iterator) == before
print('C3.float.overflow.noadvance')
