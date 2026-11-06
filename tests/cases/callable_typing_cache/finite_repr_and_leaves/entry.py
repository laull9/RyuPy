import typing as t
from collections import abc
C=t.Callable
T=t.TypeVar('T')
class Plain:
    __parameters__=(T,)
values=(int,Plain,abc.Iterable,t.List[T],t.Tuple[T,...],t.Union[T,int],list[T],t.Any)
for value in values:
    a=C[[value],str]
    assert a.__args__==(value,str) and t.get_args(a)==([value],str)
assert C[[Plain],str].__parameters__==()
assert repr(C[[],None])=='typing.Callable[[], NoneType]'
assert repr(C[[int,str],bool])=='typing.Callable[[int, str], bool]'
assert repr(C[[T,t.List[T]],T])=='typing.Callable[[~T, typing.List[~T]], ~T]'
assert type(t.List[int]).__name__=='_GenericAlias'
assert type(t.Tuple[int]).__name__=='_GenericAlias'
assert type(t.Union[int,str]).__name__=='_UnionGenericAlias'
assert type(t.Literal[1]).__name__=='_LiteralGenericAlias'
assert type(C[[int],str]).__name__=='_CallableGenericAlias'
print('safe repr/leaves; plain class metadata ignored; other alias names unchanged: ok')
