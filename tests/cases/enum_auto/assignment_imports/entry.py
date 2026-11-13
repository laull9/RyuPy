# 赋值期值供后续表达式和方法默认参数读取；导入别名保持真实对象。
import enum
from enum import Enum as Base, auto as next_value
class First(Base):
    A = next_value()
    B = A
    C = enum.auto()
    def captured(self, value=A):
        return value
    def calculated(self, value=A + 4):
        return value
print('first', First.A.value, First.B.value, First.C.value, First.C.captured(), First.C.calculated())
class Second(enum.Enum):
    A = enum.auto()
    B = next_value()
print('second', Second.A.value, Second.B.value)
print('types', type(First.A.value) is int, type(Second.A.value) is int)
