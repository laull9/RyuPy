# 保存的receiver不随模块导出重绑改变；已暖结果不消费冷helper或Generic全局。
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
t.List = saved_list
for helper in ['_type_check', '_type_convert', '_check_generic', '_collect_parameters', '_GenericAlias', '_TypingEllipsis', 'Generic', 'ClassVar']:
    existed = hasattr(t, helper)
    original = getattr(t, helper, None)
    setattr(t, helper, None)
    for i in range(11):
        assert roots[i][keys[i]] is values[i]
    if existed:
        setattr(t, helper, original)
    else:
        delattr(t, helper)
print('saved-root-warm-skips-cold-helpers', True)
