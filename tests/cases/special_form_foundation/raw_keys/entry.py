# 原始下标形状与归一化结果分离；同名TypeVar保持身份。
import typing as t
T = t.TypeVar('T')
other = t.TypeVar('T')
a = t.Union[T, int]
b = t.Union[T, int, T]
c = t.Union[int, T]
assert a is t.Union[T, int]
assert a is not b and a is not c and a == b and a == c
assert hash(a) == hash(b) and hash(a) == hash(c)
assert t.Union[int] is int and t.Union[(int,)] is int
assert t.Union[int, int] is int
assert t.Optional[T] is t.Union[T, type(None)]
assert t.Optional[None] is type(None)
assert t.Optional[T] is not t.Optional[other]
for arg in [t.List[T], list[T], t.Literal[1], t.Any]:
    result = t.Optional[arg]
    assert result is t.Optional[arg]
    assert result is t.Union[arg, type(None)]
assert t.Union[None, int] is not t.Union[type(None), int]
assert t.Union[None, int] == t.Union[type(None), int]
print('raw-safe-keys', True)
