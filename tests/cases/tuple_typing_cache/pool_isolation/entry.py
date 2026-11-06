# Tuple新池与普通、SpecialForm、Generic及旧Callable/Literal/native双向隔离。
import typing as t
T=t.TypeVar('Anchor')
a=t.Tuple[T]
for i in range(140):
    P=t.TypeVar('Pressure')
    t.List[P]
    t.Union[P,int]
    t.Generic[P]
    t.Literal[i]
    t.Callable[[P],int]
    list[P]
assert t.Tuple[T] is a
ordinary=t.List[T]
union=t.Union[T,int]
generic=t.Generic[T]
literal=t.Literal['anchor']
callable_alias=t.Callable[[T],int]
# native generic aliases 没有 CP identity cache，因此不对 list[T] 断言 is。
for i in range(140):
    t.Tuple[t.TypeVar('TuplePressure')]
assert t.List[T] is ordinary
assert t.Union[T,int] is union
assert t.Generic[T] is generic
assert t.Literal['anchor'] is literal
assert t.Callable[[T],int] is callable_alias
print('tuple-pool-bidirectional-isolation',True)
