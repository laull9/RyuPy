# scalar Ellipsis与list里的Ellipsis原键不同，规范化后相等；不能冒普通参数list形状。
import typing as t
from collections import abc
C=t.Callable
T=t.TypeVar('T')
a=C[...,T]; b=C[[...],T]
assert a==b and a is not b
assert C[...,T] is a and C[(...,),T] is b
for x in (a,b):
    assert type(x).__name__=='_CallableGenericAlias'
    assert x.__origin__ is abc.Callable
    assert x.__args__==(Ellipsis,T) and x.__parameters__==(T,)
    assert t.get_args(x)==(Ellipsis,T) and t.get_args(x) is x.__args__
    assert repr(x)=='typing.Callable[..., ~T]'
print('ellipsis complete shape/repr/identity: ok')
