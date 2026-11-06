# Generic为可修改heap type；普通元数据不能误报不可变类型。
import typing as t
T = t.TypeVar('T')
A = t.Generic[T]
t.Generic.extra = 10
assert A.extra == 10
A.extra = 20
assert t.Generic.extra == 20
original = t.Generic.__module__
t.Generic.__module__ = 'changed'
assert A.__module__ == 'typing'
assert repr(A) == 'changed.Generic[~T]'
t.Generic.__module__ = original
del t.Generic.extra
print('root-metadata', True)
