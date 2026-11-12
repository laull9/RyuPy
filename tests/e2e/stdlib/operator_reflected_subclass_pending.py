# ryupy: pending
# 既有 Binary 先左后右的已知偏差；operator.add/iadd 复用它，不另造独立修补分支。
import operator

class Parent:
    def __add__(self, other):
        return "left"
    def __iadd__(self, other):
        return NotImplemented

class Child(Parent):
    def __radd__(self, other):
        return "right-subclass"

left, right = Parent(), Child()
print(left + right)
print(operator.add(left, right))
print(operator.iadd(left, right))
