# 已有 typing 组合保存变量身份并递归按首次出现次序收集参数。
import typing as t
T = t.TypeVar('T')
T2 = t.TypeVar('T')
U = t.TypeVar('U')
for alias, args, parameters in (
    (t.List[T], (T,), (T,)),
    (t.Dict[U, T], (U, T), (U, T)),
    (t.Tuple[T, T, U], (T, T, U), (T, U)),
    (t.Optional[T], (T, type(None)), (T,)),
    (list[T], (T,), (T,)),
    (t.Union[T, T2, U, T], (T, T2, U), (T, T2, U)),
):
    assert t.get_args(alias) == args
    assert alias.__parameters__ == parameters
    for actual, expected in zip(alias.__parameters__, parameters):
        assert actual is expected
assert t.get_origin(t.List[T]) is list
assert t.get_origin(list[T]) is list
assert t.List[T] is t.List[T] and t.List[T] is not t.List[T2]
assert t.Union[T, T] is T
assert t.Union[T, T2] == t.Union[T2, T]
assert hash(t.Union[T, T2]) == hash(t.Union[T2, T])
print('aliases', True)
nested = t.Dict[U, t.Tuple[t.List[T], U, T2]]
assert nested.__parameters__ == (U, T, T2)
callable_alias = t.Callable[[U, t.List[T], U], T2]
assert callable_alias.__parameters__ == (U, T, T2)
args = t.get_args(callable_alias)
assert args[0][0] is U and args[0][1] is t.List[T] and args[1] is T2
assert t.List[int].__parameters__ == ()
assert t.Literal[T].__parameters__ == (T,)
print('recursive-parameters', True)
class Pair(t.NamedTuple):
    first: T
    second: U
assert Pair.__annotations__['first'] is T and Pair.__annotations__['second'] is U
pair = Pair(1, 'two')
assert pair.first == 1 and pair.second == 'two'
class Row(t.TypedDict):
    first: T
    second: U
assert Row.__annotations__['first'] is T and Row.__annotations__['second'] is U
row = Row(first=1, second='two')
assert row == {'first': 1, 'second': 'two'}
print('annotations', True)
