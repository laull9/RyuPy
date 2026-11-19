import typing as t
from collections import abc
C=t.Callable
T=t.TypeVar('T'); S=t.TypeVar('S')
a=C[[T,t.List[S],T],S]
assert type(C).__name__=='_CallableType'
assert type(a).__name__=='_CallableGenericAlias'
assert a.__origin__ is abc.Callable and t.get_origin(a) is abc.Callable
assert a.__args__ is a.__args__ and a.__parameters__ is a.__parameters__
assert a.__args__==(T,t.List[S],T,S) and a.__parameters__==(T,S)
p=t.get_args(a); q=t.get_args(a)
assert p==q==([T,t.List[S],T],S) and p is not q and p[0] is not q[0]
p[0].append(bytes)
assert t.get_args(a)==([T,t.List[S],T],S)
e=C[[],None]
assert e.__args__==(type(None),) and t.get_args(e)==([],type(None))
assert repr(e)=='typing.Callable[[], NoneType]'
# copy_with 是未缓存构造，不重新_type_check或_type_convert。
t._type_check=None; t._type_convert=None
b=C.copy_with((int,str))
c=C.copy_with((int,str))
assert b==c and b is not c and b.__args__==(int,str)
assert type(b).__name__=='_CallableGenericAlias'
print('canonical type/origin; stable flat metadata; fresh public list; empty repr; copy_with fresh: ok')
