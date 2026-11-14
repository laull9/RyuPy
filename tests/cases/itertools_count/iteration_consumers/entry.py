# count 有限数值语义应用。
# count 公开应用四：有限流消费、共享对象、显式方法与无限迭代的有界观察。
import itertools
from itertools import count

iterator = count(10, 2)
assert iter(iterator) is iterator
assert iterator.__iter__() is iterator
read = iterator.__next__
assert next(iterator) == 10
assert read() == 12
assert next(iterator, 'fallback') == 14
assert list(itertools.islice(iterator, 0)) == []
assert next(iterator) == 16
assert list(itertools.islice(iterator, 3)) == [18, 20, 22]
assert next(iterator) == 24
assert repr(iterator) == 'count(26, 2)'
print('C4.shared.consumer')

iterator = count(100)
rows = []
for value in iterator:
    rows.append(value)
    if len(rows) == 4:
        break
assert rows == [100, 101, 102, 103]
assert next(iterator) == 104
zero = count(10**80, 0)
for _ in range(16):
    assert next(zero) == 10**80
assert iter(zero) is zero
assert list(itertools.islice(count(5, -2), 4)) == [5, 3, 1, -1]
print('C4.infinite.bounded')

for name in ['start', 'step']:
    try:
        getattr(iterator, name)
        assert False
    except AttributeError as error:
        assert str(error) == "'itertools.count' object has no attribute '" + name + "'"
try:
    len(iterator)
    assert False
except TypeError as error:
    assert str(error) == "object of type 'itertools.count' has no len()"
print('C4.absent.attributes')
