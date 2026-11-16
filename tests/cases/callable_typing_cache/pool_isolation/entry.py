# 不承诺Literal与native旧池彼此已分开；native alias本来也不缓存身份。
import typing as t
C=t.Callable
T=t.TypeVar('Anchor')
a=C[[T],int]
for i in range(140):
    P=t.TypeVar('Pressure')
    t.List[P]
    t.Tuple[P]
    t.Union[P,int]
    t.Generic[P]
    t.Literal[i]
    list[P]
assert C[(T,),int] is a
ordinary=t.List[T]; tuple_alias=t.Tuple[T]
union=t.Union[T,int]; generic=t.Generic[T]; literal=t.Literal['anchor']
for i in range(140):
    C[[t.TypeVar('CallablePressure')],str]
assert t.List[T] is ordinary and t.Tuple[T] is tuple_alias
assert t.Union[T,int] is union and t.Generic[T] is generic
assert t.Literal['anchor'] is literal
print('callable pool bidirectional isolation: ok')
