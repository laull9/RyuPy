# 整数、布尔及任意精度历史；别名也参加生成历史。
from enum import Enum, auto
import warnings
with warnings.catch_warnings(record=True) as seen:
    warnings.simplefilter('always')
    class E(Enum):
        ZERO = 0
        ONE = auto()
        ALIAS = ONE
        NEGATIVE = -10
        TWO = auto()
        HUGE = 1267650600228229401496703205376
        NEXT = auto()
    print('values', E.ZERO.value, E.ONE.value, E.TWO.value, E.HUGE.value, E.NEXT.value)
    print('alias', E.ALIAS is E.ONE)
    print('warnings', [(w.category.__name__, str(w.message)) for w in seen])
class Negative(Enum):
    A = -10
    B = auto()
print('negative', Negative.A.value, Negative.B.value)
class Boolean(Enum):
    A = False
    B = True
    C = auto()
print('boolean', Boolean.A.value, Boolean.B.value, Boolean.C.value)
