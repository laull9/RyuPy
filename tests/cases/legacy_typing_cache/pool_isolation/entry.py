# 只承诺普通根新池与其他池隔离，不声称旧Tuple/Callable/Literal/native彼此已隔离。
import typing as t
T = t.TypeVar('Anchor')
a = t.List[T]
for i in range(140):
    P = t.TypeVar('Pressure')
    t.Union[P, int]
    t.Generic[P]
    t.Literal[i]
    t.Tuple[P]
    t.Callable[[P], int]
    list[P]
assert t.List[T] is a
union = t.Union[T, int]
generic = t.Generic[T]
literal = t.Literal['anchor']
tuple_alias = t.Tuple[T]
callable_alias = t.Callable[[T], int]
for i in range(140):
    t.List[t.TypeVar('OrdinaryPressure')]
assert t.Union[T, int] is union
assert t.Generic[T] is generic
assert t.Literal['anchor'] is literal
assert t.Tuple[T] is tuple_alias
assert t.Callable[[T], int] is callable_alias
print('new-pool-bidirectional-isolation', True)
