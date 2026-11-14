# alias元数据改写合法，不能误作只读AttributeError。
import typing as t
T = t.TypeVar('T')
U = t.TypeVar('U')
A = t.Generic[T]
A.__args__ = (U,)
assert A.__args__ == (U,) and t.get_args(A) == (U,)
assert A.__parameters__ == (T,)
print('alias-metadata', repr(A))
