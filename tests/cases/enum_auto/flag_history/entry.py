# Flag 非负历史与任意精度位，含别名、组合位和赋值期读取。
from enum import Flag, auto
class Bits(Flag):
    ZERO = 0
    A = auto()
    ALIAS = A
    TWO = 2
    COMBINED = 3
    B = auto()
    HUGE = 1267650600228229401496703205376
    NEXT = auto()
    def captured(self, value=A):
        return value
print('flag', Bits.ZERO.value, Bits.A.value, Bits.COMBINED.value, Bits.B.value, Bits.HUGE.value, Bits.NEXT.value)
print('alias', Bits.ALIAS is Bits.A, Bits.B.captured())
class Other(Flag):
    A = auto()
    B = auto()
print('independent', Other.A.value, Other.B.value)
