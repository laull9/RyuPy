# 本轮全新重建语义样例；不是历史丢失源码恢复。
import subject
class Left:
    def __iadd__(self, rhs):
        print("iadd")
        return NotImplemented
    def __add__(self, rhs):
        print("add")
        return NotImplemented
class Right:
    def __radd__(self, lhs):
        print("radd")
        return NotImplemented
try:
    subject.fold(Left(), [Right()])
except TypeError:
    print("TypeError")
