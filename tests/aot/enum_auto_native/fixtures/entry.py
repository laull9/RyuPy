print('contract', 'integer_history')
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

print('contract', 'assignment_imports')
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

print('contract', 'flag_history')
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

print('contract', 'warning_publication')
# CP3.12 非数历史及 warning filter/error；生成失败不能提前替换公开类。
from enum import Enum, auto
import warnings
with warnings.catch_warnings(record=True) as seen:
    warnings.simplefilter('always')
    class Text(Enum):
        A = 'text'
        B = auto()
    class Missing(Enum):
        A = None
        B = auto()
    print('nonnumeric', Text.B.value, Missing.B.value)
    print('record', [(w.category.__name__, str(w.message)) for w in seen])
with warnings.catch_warnings(record=True) as seen:
    warnings.simplefilter('ignore', DeprecationWarning)
    class Ignore(Enum):
        A = 10
        B = 1
        C = auto()
    print('ignore', Ignore.C.value, len(seen))
Published = 'old'
with warnings.catch_warnings():
    warnings.simplefilter('error', DeprecationWarning)
    try:
        class Published(Enum):
            A = 10
            B = 1
            C = auto()
            def unreachable(self, x=print('unreachable')):
                pass
    except DeprecationWarning as e:
        print('error', type(e).__name__, str(e))
print('binding', Published)

from values import Ordinary
obj = Ordinary()
Ordinary.value = 9
obj.value = 12
print('ordinary', Ordinary.value, obj.read())
print('enum-auto-native-complete')
