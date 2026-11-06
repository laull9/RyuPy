# ryupy: pending
# CP合法类级wrapper、派生、copy/pickle；不把未实现路径删除或改写成拒绝测试。
from itertools import count
import copy
import pickle

iterator = count(10, 2)
assert count.__iter__(iterator) is iterator
assert count.__next__(iterator) == 10
assert iterator.__reduce__() == (count, (12, 2))
for copied in [copy.copy(iterator), copy.deepcopy(iterator), pickle.loads(pickle.dumps(iterator))]:
    assert type(copied) is count
    assert copied is not iterator
    assert next(copied) == 12
assert next(iterator) == 12
print('P3.copy.independent.state')


class Child(count):
    pass


iterator = Child(3, 2)
assert type(iterator) is Child and isinstance(iterator, count)
assert next(iterator) == 3
assert repr(iterator) == 'Child(5, 2)'
iterator.note = 'allowed on subclass'
assert iterator.note == 'allowed on subclass'
print('P3.real.subclass')
