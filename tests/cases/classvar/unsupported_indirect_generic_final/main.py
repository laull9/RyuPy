# ryupy: pending
# Final 尚未提供；原生别名间接容纳 Generic[T] 是 CP 合法域，不能漏收参数后静默返回空 tuple。
import typing as t
T = t.TypeVar('T')
CV = t.ClassVar
generic = t.Generic[T]
native = list[generic]
alias = CV[native]
assert alias.__args__ == (native,)
assert alias.__parameters__ == (T,)
assert CV[dict[str, generic]].__parameters__ == (T,)
print('indirect-generic', repr(alias), alias.__parameters__)
try:
    CV[t.Final[int]]
except TypeError as error:
    print('final-rejected', str(error))
else:
    assert False
