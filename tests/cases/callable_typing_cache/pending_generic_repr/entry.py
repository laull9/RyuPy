import typing as t
C=t.Callable
T=t.TypeVar('T')
a=C[[t.Generic],int]
assert a.__args__==(t.Generic,int) and a.__parameters__==()
assert t.get_args(a)==([t.Generic],int)
assert repr(a)=='typing.Callable[[typing.Generic], int]'
b=C[[t.Generic[T]],T]
assert b.__args__==(t.Generic[T],T) and b.__parameters__==(T,)
assert t.get_args(b)==([t.Generic[T]],T)
assert repr(b)=='typing.Callable[[typing.Generic[~T]], ~T]'
print('Generic argument conversion, collector and full repr: ok')
