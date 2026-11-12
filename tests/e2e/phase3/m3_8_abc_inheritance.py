# ABC 类族合同：真实身份/C3、抽象约束、空slots及live view，不能靠同名空壳类过关。
from collections.abc import Sequence, MutableSequence, MutableSet, KeysView, ItemsView, ValuesView

class Numbers(Sequence):
    __slots__ = ()
    def __len__(self):
        return 3
    def __getitem__(self, index):
        return (10, 20, 30)[index]

values = Numbers()
print(list(values), list(reversed(values)), 20 in values, values.index(30), values.count(20))
print(isinstance(values, Sequence), issubclass(Numbers, Sequence))
print(hasattr(values, '__dict__'))
try:
    values.extra = 1
except AttributeError:
    print('slots')

class MutableNumbers(MutableSequence):
    def __init__(self):
        self.values = [3, 1]
    def __len__(self):
        return len(self.values)
    def __getitem__(self, index):
        return self.values[index]
    def __setitem__(self, index, value):
        self.values[index] = value
    def __delitem__(self, index):
        del self.values[index]
    def insert(self, index, value):
        self.values.insert(index, value)

mutable = MutableNumbers()
mutable.append(2)
mutable.extend([4, 5])
mutable.reverse()
print(list(mutable), mutable.pop())
mutable.remove(1)
print(list(mutable))

class Incomplete(Sequence):
    pass
try:
    Incomplete()
except TypeError:
    print('abstract')

class Both(MutableSet, Sequence):
    def __init__(self):
        self.values = [1, 2]
    def __len__(self):
        return len(self.values)
    def __getitem__(self, index):
        return self.values[index]
    def __iter__(self):
        return iter(self.values)
    def __contains__(self, value):
        return value in self.values
    def add(self, value):
        if value not in self.values:
            self.values.append(value)
    def discard(self, value):
        if value in self.values:
            self.values.remove(value)

both = Both()
print(list(both), both[-1], isinstance(both, MutableSet), isinstance(both, Sequence))
print([base.__name__ for base in Both.__mro__])

mapping = {'a': 1, 'b': 2}
keys, items, vals = KeysView(mapping), ItemsView(mapping), ValuesView(mapping)
print(list(keys), list(items), list(vals))
mapping['c'] = 3
print(len(keys), len(items), len(vals), 'c' in keys, ('c', 3) in items, 3 in vals)
class OrderedItems(ItemsView, Sequence):
    __slots__ = ()
    def __getitem__(self, index):
        return list(self._mapping.items())[index]
view = OrderedItems(mapping)
print(view[-1], list(view), hasattr(view, '__dict__'))
