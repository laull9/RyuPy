# CP允许改写alias元数据，缓存不得假装对象不可变。
import typing as t
T = t.TypeVar('T')
a = t.Optional[T]
a.__args__ = (str, type(None))
assert t.Optional[T] is a
assert a.__parameters__ == (T,)
assert a.__args__ == (str, type(None))
a.__module__ = 'changed'
assert a.__module__ == 'changed'
print('mutable-alias', repr(a), a.__parameters__)
