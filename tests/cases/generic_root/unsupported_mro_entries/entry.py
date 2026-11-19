# 超过有限单元素tuple的合法mro_entries单独保留。
import typing as t
T = t.TypeVar('T')
A = t.Generic[T]
B = t.Generic[(T,)]
assert A.__mro_entries__((A, B)) == ()
assert A.__mro_entries__((B, A)) == (t.Generic,)
assert A.__mro_entries__((A, t.List[int])) == ()
assert A.__mro_entries__([A]) == (t.Generic,)
print('mro-entries', True)
