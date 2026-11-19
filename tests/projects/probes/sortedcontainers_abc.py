# 扩展诊断，不加入固定20分母：原包多态构造、实时view、Sequence/Set双协议与继承方法。
from sortedcontainers import SortedDict, SortedList, SortedSet
from collections.abc import Sequence, MutableSequence, MutableSet, ItemsView, KeysView, ValuesView

values = SortedList([4, 1, 3, 2])
print(list(values), isinstance(values, MutableSequence), list(reversed(values)))
values.add(0)
print(values.pop(), list(values), values.bisect_left(2), values.bisect_right(2))
keyed = SortedList(['bbb', 'a', 'cc'], key=len)
print(type(keyed).__name__, list(keyed), keyed.key is len)
keyed.add('dd')
print(list(keyed), keyed.bisect_key_left(2), keyed.bisect_key_right(2))

mapping = SortedDict({'b': 2, 'a': 1})
keys, items, vals = mapping.keys(), mapping.items(), mapping.values()
print(isinstance(keys, KeysView), isinstance(items, ItemsView), isinstance(vals, ValuesView))
print(list(keys), list(items), list(vals), items[-1], vals[0])
mapping['c'] = 3
print(list(keys), list(items), list(vals), 'c' in keys, ('c', 3) in items, 3 in vals)
del mapping['a']
print(len(keys), len(items), len(vals), list(items))
print(sorted(keys & {'b', 'd'}), sorted(items | {('d', 4)}))
print(hasattr(items, '__dict__'))
try:
    items.extra = 1
except AttributeError:
    print('view slots')

unique = SortedSet([3, 1, 2, 3])
print(isinstance(unique, Sequence), isinstance(unique, MutableSet), list(unique), unique[-1])
unique.add(4)
unique.discard(2)
print(list(unique), list(unique | {0, 3}), list(unique & {1, 9}))
print([cls.__name__ for cls in type(unique).__mro__])

class Descendant(SortedList):
    def total(self):
        return sum(self)
child = Descendant([2, 1, 3])
print(list(child), child.total(), isinstance(child, MutableSequence))
