# 独立进程只验证暖路径；不读取未导出的helper，也不借模块delattr恢复未知槽。
import typing as t
T = t.TypeVar('Warm')
roots = [t.List, t.Dict, t.Set, t.FrozenSet, t.Iterable, t.Iterator, t.Sequence, t.Mapping, t.MutableMapping, t.Collection, t.Container]
keys = []
values = []
for root in roots:
    key = T
    if root is t.Dict or root is t.Mapping or root is t.MutableMapping:
        key = (T, int)
    keys.append(key)
    values.append(root[key])
saved_list = t.List
t.List = None
assert saved_list[T] is values[0]
for helper in ['_type_check', '_type_convert', '_check_generic', '_collect_parameters', '_GenericAlias', '_TypingEllipsis', 'Generic', 'ClassVar']:
    setattr(t, helper, None)
    for i in range(11):
        assert roots[i][keys[i]] is values[i]
print('saved-root-warm-skips-cold-helpers', True)
