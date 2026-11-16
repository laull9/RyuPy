# operator 与语言运算共用协议；非布尔富比较结果不能被 operator 提前求真值。
import operator

a = [1]
b = operator.iadd(a, (2, 3))
print(a is b, a, operator.add(a, [4]))
print(operator.add(2, 3), operator.iadd(2, 3))
print(operator.eq(2, 2), operator.ne(2, 2), operator.lt(1, 2), operator.le(2, 2), operator.gt(2, 1), operator.ge(2, 2))
events = []
marker = [42]

class Left:
    def __eq__(self, other):
        events.append("eq")
        return marker
    def __lt__(self, other):
        events.append("lt")
        return marker
    def __iadd__(self, other):
        events.append("iadd")
        return self

left = Left()
print(operator.eq(left, left) is marker, operator.lt(left, left) is marker)
print(bool(left == left), bool(left < left), operator.ne(left, left))
print(operator.iadd(left, 1) is left, events)

class Base:
    def __eq__(self, other):
        events.append("base-eq")
        return NotImplemented
    def __ne__(self, other):
        events.append("base-ne")
        return NotImplemented
    def __lt__(self, other):
        events.append("base-lt")
        return NotImplemented

class Child(Base):
    def __eq__(self, other):
        events.append("child-eq")
        return marker
    def __ne__(self, other):
        events.append("child-ne")
        return []
    def __gt__(self, other):
        events.append("child-gt")
        return marker

base, child = Base(), Child()
events.clear()
print(operator.eq(base, child) is marker, operator.ne(base, child), operator.lt(base, child) is marker)
print(bool(base == child), bool(base != child), bool(base < child), events)

class Missing:
    def __eq__(self, other):
        events.append("missing")
        return NotImplemented
    def __ne__(self, other):
        events.append("missing-ne")
        return NotImplemented

one, two = Missing(), Missing()
events.clear()
print(operator.eq(one, two), operator.ne(one, two), operator.eq(one, one), events)

class AddLeft:
    def __add__(self, other):
        events.append("add")
        return NotImplemented
    def __iadd__(self, other):
        events.append("inplace")
        return NotImplemented

class AddRight:
    def __radd__(self, other):
        events.append("radd")
        return marker

events.clear()
print(operator.add(AddLeft(), AddRight()) is marker, operator.iadd(AddLeft(), AddRight()) is marker, events)
for func in (operator.add, operator.iadd, operator.eq, operator.ne, operator.lt, operator.le, operator.gt, operator.ge):
    try:
        func(a=1, b=2)
    except TypeError:
        print("posonly")

class Unequal:
    def __eq__(self, other):
        return False
    def __ne__(self, other):
        return marker
u = Unequal()
print(operator.ne(u, u) is marker, bool(u != u))
try:
    operator.lt(one, two)
except TypeError:
    print("unorderable")
