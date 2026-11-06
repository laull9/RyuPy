# ryupy: pending
# CP合法标准数值与原生子类，不把尚未支持的模块/载体伪装成TypeError。
from itertools import count
from decimal import Decimal
from fractions import Fraction


def take(iterator, size):
    return [next(iterator) for _ in range(size)]


assert take(count(1+2j, 0.5j), 3) == [1+2j, 1+2.5j, 1+3j]
assert take(count(Decimal('0.1'), Decimal('0.2')), 3) == [Decimal('0.1'), Decimal('0.3'), Decimal('0.5')]
assert take(count(Fraction(1, 3), Fraction(1, 6)), 3) == [Fraction(1, 3), Fraction(1, 2), Fraction(2, 3)]
print('P2.standard.numbers')

events = []


class IntChild(int):
    def __add__(self, other):
        events.append('add')
        return 99


small = IntChild(3)
iterator = count(small, 1)
assert type(next(iterator)) is int
assert events == []
iterator = count(small, 2)
assert next(iterator) is small
assert events == ['add']
assert next(iterator) == 99
events.clear()
large = IntChild(2**63)
iterator = count(large, 1)
assert next(iterator) is large
assert events == ['add']
print('P2.subclass.fast.slow')
