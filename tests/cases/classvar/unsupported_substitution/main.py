# ryupy: pending
# C2 原始正例：自由参数替换有独立缓存；不能把所有再下标都伪造为 TypeError。
import typing as t
CV = t.ClassVar
T = t.TypeVar('T')
U = t.TypeVar('U')
a = CV[T][int]
b = CV[int]
assert a == b and a is not b
assert CV[T][int] is a
assert a.__args__ == (int,) and a.__parameters__ == ()
assert CV[t.List[T]][int] == CV[t.List[int]]
assert CV[dict[T, list[U]]][str, int] == CV[dict[str, list[int]]]
partial = CV[tuple[T, U]][int, U]
assert partial.__parameters__ == (U,)
assert partial[str] == CV[tuple[int, str]]
print('substitution', repr(a), repr(partial), repr(partial[str]))
for value in [(), (int, str)]:
    try:
        CV[T][value]
    except TypeError as error:
        print('arity', str(error))
    else:
        assert False
