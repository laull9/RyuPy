# 元数据保持稳定身份和首次变量顺序，普通类自身参数属性不被收集。
import typing as t
T=t.TypeVar('T')
U=t.TypeVar('U')
class Plain:
    __parameters__=(U,)
for alias in [t.Tuple[()],t.Tuple[T],t.Tuple[T,...],t.Tuple[T,t.List[U],T,...],t.Tuple[Plain]]:
    args=alias.__args__
    params=alias.__parameters__
    assert alias.__args__ is args and t.get_args(alias) is args
    assert alias.__parameters__ is params
    assert t.get_origin(alias) is tuple
    assert type(alias).__name__=='_GenericAlias'
assert t.Tuple[T,t.List[U],T,...].__parameters__==(T,U)
assert t.Tuple[Plain].__parameters__==()
assert repr(t.Tuple[T,...])=='typing.Tuple[~T, ...]'
print('stable-args-parameters-origin-type',True)
