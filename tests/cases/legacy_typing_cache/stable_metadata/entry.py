# 冷建结果和暖命中的args/parameters都是稳定快照；保留嵌套变量顺序和身份。
import typing as t
T = t.TypeVar('T')
U = t.TypeVar('U')
class Plain:
    __parameters__ = (U,)
for alias in [t.List[T], t.Dict[T, t.List[U]], t.Mapping[T, T], t.Sequence[int], t.Container[Plain]]:
    args = alias.__args__
    parameters = alias.__parameters__
    assert args is alias.__args__ and args is t.get_args(alias)
    assert parameters is alias.__parameters__
assert t.Dict[T, t.List[U]].__parameters__ == (T, U)
assert t.Mapping[T, T].__parameters__ == (T,)
assert t.Sequence[int].__parameters__ == ()
assert t.Container[Plain].__parameters__ == ()
a = t.List[T]
b = t.List[(T,)]
assert a.__args__ == b.__args__ and a.__parameters__ == b.__parameters__
assert a.__args__ is not a.__parameters__
assert t.get_origin(a) is list
assert type(a) is type(t.Generic[T])
print('stable-args-parameters', True)
