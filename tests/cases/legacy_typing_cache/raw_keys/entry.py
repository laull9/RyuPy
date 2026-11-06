# 十一个普通根保留原始键形状；归一化结果相等不意味着缓存身份相同。
import typing as t
T = t.TypeVar('T')
other = t.TypeVar('T')
for root in [t.List, t.Set, t.FrozenSet, t.Iterable, t.Iterator, t.Sequence, t.Collection, t.Container]:
    a = root[T]
    b = root[(T,)]
    assert a is root[T] and b is root[(T,)]
    assert a is not b and a == b and hash(a) == hash(b)
    assert a is not root[other]
    n = root[None]
    nt = root[type(None)]
    assert n is root[None] and nt is root[type(None)]
    assert n is not nt and n == nt and hash(n) == hash(nt)
for root in [t.Dict, t.Mapping, t.MutableMapping]:
    a = root[T, None]
    b = root[T, type(None)]
    assert a is root[T, None] and b is root[T, type(None)]
    assert a is not b and a == b and hash(a) == hash(b)
assert t.List[T] != t.Sequence[T]
print('raw-shape-none-receiver', True)
