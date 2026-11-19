# 每根均被另一普通根的128个唯一键驱逐，不能偷偷分成11个私池。
import typing as t
roots = [t.List, t.Dict, t.Set, t.FrozenSet, t.Iterable, t.Iterator, t.Sequence, t.Mapping, t.MutableMapping, t.Collection, t.Container]
for root in roots:
    key = t.TypeVar('Victim')
    if root is t.Dict or root is t.Mapping or root is t.MutableMapping:
        key = (key, int)
    old = root[key]
    other = t.Set if root is t.List else t.List
    for i in range(128):
        other[t.TypeVar('Pressure')]
    new = root[key]
    assert old is not new and old == new
    assert new is root[key]
print('all-eleven-cross-evict', True)
