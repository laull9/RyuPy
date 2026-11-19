# TypeVar身份、订阅原形缓存与有限mro_entries合同。
import typing as t
T = t.TypeVar('T')
U = t.TypeVar('U')
other = t.TypeVar('T')
A = t.Generic[T]
B = t.Generic[(T,)]
C = t.Generic[T, U]
assert A is t.Generic[T] and B is t.Generic[(T,)]
assert A is not B and A == B and hash(A) == hash(B)
assert A != t.Generic[other] and A != C
assert type(A) is type(t.List[T]) and not isinstance(A, type)
assert A.__origin__ is t.Generic and A.__args__ == (T,)
assert A.__parameters__ == (T,) and C.__parameters__ == (T, U)
assert C.__args__[0] is T and C.__args__[1] is U
assert A.__module__ == 'typing' and A.__name__ == 'Generic'
assert A.__qualname__ == 'Generic'
assert t.get_origin(A) is t.Generic and t.get_args(A) == (T,)
assert repr(A) == 'typing.Generic[~T]' and str(A) == repr(A)
assert callable(A) and type(A()) is t.Generic
assert A.__mro_entries__((A,)) == (t.Generic,)
assert A.__mro_entries__((B,)) == (t.Generic,)
assert A.__mro_entries__(bases=(A,)) == (t.Generic,)
print('alias', repr(A), repr(C), True)
