# 连续128个新TypeVar排除进程已有缓存；触碰首项后第129项只能驱逐第二项。
import typing as t
roots = [t.List, t.Dict, t.Set, t.FrozenSet, t.Iterable, t.Iterator, t.Sequence, t.Mapping, t.MutableMapping, t.Collection, t.Container]
keys = []
values = []
for i in range(128):
    root = roots[i % 11]
    key = t.TypeVar('K')
    if root is t.Dict or root is t.Mapping or root is t.MutableMapping:
        key = (key, int)
    keys.append(key)
    values.append(root[key])
assert t.List[keys[0]] is values[0]
t.Container[t.TypeVar('Overflow')]
assert t.List[keys[0]] is values[0]
assert t.Dict[keys[1]] is not values[1]
assert t.Dict[keys[1]] == values[1]
print('shared-128-true-lru', True)
