# G3用户类订阅与替换仍完整pending，不能因G2建类成功就缩短此合同。
from typing import Generic, TypeVar, get_origin, get_args
T = TypeVar('T')
class Box(Generic[T]):
    def __init__(self, value):
        self.value = value
A = Box[T]
B = A[int]
assert B == Box[int]
assert get_origin(B) is Box and get_args(B) == (int,)
x = B(3)
assert type(x) is Box and x.__orig_class__ == B and x.value == 3
class Concrete(B):
    pass
assert Concrete.__parameters__ == ()
assert Concrete.__orig_bases__[0] == B
assert Concrete.__base__ is Box
print('user-alias-substitution-concrete', True)
