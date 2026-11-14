# ryupy: pending
# 不触及未知hook的稳定元数据与普通分配身份，可直接成为主cases。
from typing import Generic, TypeVar
T = TypeVar('T')
A = Generic[T]
class Box(A):
    pass
class Child(Box):
    pass
print('stable', Box.__bases__ is Box.__bases__, Box.__mro__ is Box.__mro__,
      Box.__orig_bases__ is Box.__orig_bases__, Box.__parameters__ is Box.__parameters__)
print('mro-list', Box.mro() == Box.mro(), Box.mro() is Box.mro())
print('inherited', Child.__orig_bases__ is Box.__orig_bases__, Child.__parameters__ == ())
x = object.__new__(Box)
y = object.__new__(Child)
x.field = 11
y.field = 12
print('allocation', type(x) is Box, type(y) is Child, x.field, y.field)
