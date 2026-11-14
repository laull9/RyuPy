# G2独立边界，不以G1真根冒充用户泛型派生支持。
import typing as t
T = t.TypeVar('T')
class Box(t.Generic[T]):
    def __init__(self, value):
        self.value = value
assert Box(3).value == 3
assert Box.__parameters__ == (T,)
assert Box.__orig_bases__[0] is t.Generic[T]
assert issubclass(Box, t.Generic)
print('generic-derivation', True)
