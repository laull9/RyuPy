import typing as t
C=t.Callable
T=t.TypeVar('T'); P=t.ParamSpec('P'); Ts=t.TypeVarTuple('Ts')
a=C[P,T]
assert a.__args__==(P,T) and a.__parameters__==(P,T)
assert t.get_args(a)==(P,T) and t.get_args(a) is a.__args__
assert repr(a)=='typing.Callable[~P, ~T]' and C[P,T] is a
concat=t.Concatenate[T,P]
b=C[concat,int]
assert b.__args__==(concat,int) and b.__parameters__==(T,P)
assert t.get_args(b)==(concat,int) and t.get_args(b) is b.__args__
assert repr(b)=='typing.Callable[typing.Concatenate[~T, ~P], int]'
unpack=t.Unpack[Ts]
c=C[[unpack],T]
assert c.__args__==(unpack,T) and c.__parameters__==(Ts,T)
assert t.get_args(c)==([unpack],T)
assert repr(c)=='typing.Callable[[typing.Unpack[Ts]], ~T]'
assert C[(unpack,),T] is c
print('parameter expressions complete shapes, collector and repr: ok')
