# ryupy: pending
# C1 安全参数与真实类型元数据；原始 None 键不能在归一化后与 NoneType 合并。
import typing as t
from typing import ClassVar as CV

assert CV is t.ClassVar
assert repr(CV) == 'typing.ClassVar'
assert CV.__name__ == CV.__qualname__ == 'ClassVar'
assert CV.__module__ == 'typing'
assert CV.__class__ is type(CV)
assert not isinstance(CV, type)
assert type(CV) is type(t.Optional) is type(t.Union)
assert type(CV) is not type(t.Literal)
assert type(CV).__name__ == '_SpecialForm'
assert type(CV).__module__ == 'typing'
assert t.get_origin(CV) is None and t.get_args(CV) == ()
print('root', repr(type(CV)), repr(CV))

class Plain:
    pass

T = t.TypeVar('T')
U = t.TypeVar('U')
same_name = t.TypeVar('T')
keys = [int, str, bytes, type(None), Plain, t.Any, T, t.List[T], list[T],
        t.Dict[str, T], dict[str, T], t.Union[T, U], t.Optional[T],
        t.Literal[1], t.Callable[[T, U], T], tuple[T, T, U]]
for key in keys:
    alias = CV[key]
    assert alias is CV[key]
    assert alias.__class__ is type(alias)
    assert not isinstance(alias, type)
    assert type(alias) is type(t.List[int]) is type(t.Generic[T])
    assert type(alias).__name__ == '_GenericAlias'
    assert type(alias).__module__ == 'typing'
    assert alias.__name__ == alias.__qualname__ == 'ClassVar'
    assert alias.__module__ == 'typing'
    assert alias.__origin__ is CV and t.get_origin(alias) is CV
    assert alias.__args__ == (key,)
    assert alias.__args__ is alias.__args__ is t.get_args(alias)
    assert alias.__parameters__ is alias.__parameters__
    print('alias', repr(alias), repr(alias.__parameters__))

a = CV[None]
b = CV[type(None)]
assert a is not b and a == b and hash(a) == hash(b)
assert a.__args__ == b.__args__ == (type(None),)
assert CV[T] is not CV[same_name]
assert CV[T].__parameters__ == (T,)
assert CV[tuple[T, T, U]].__parameters__ == (T, U)
assert CV[t.Callable[[T, U], T]].__parameters__ == (T, U)
assert CV[t.Union[T, U]] is CV[t.Union[U, T]]
assert CV[list[T]] is CV[list[T]]
print('raw-none-and-parameters', True)
