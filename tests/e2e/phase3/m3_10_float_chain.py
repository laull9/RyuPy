# 短浮点DAG只消除不可观察的中间盒；不能FMA/重结合，也不能改用户协议回退顺序。
import math


def mul_add(a, b, c):
    return a * b + c


def norm_squared(dx, dy, dz):
    return dx * dx + dy * dy + dz * dz


def subtract_chain(a, b, c):
    return a * b - c


def add_chain(a, b, c):
    return (a + b) + c


def inplace(total, a, b):
    total += a * b
    return total


def observed_product(a, b, c):
    temporary = a * b
    alias = temporary
    result = temporary + c
    return temporary, alias, result


def line_boundary(a, b, c):
    temporary = a * b
    events.append(('observed_product', temporary))
    return temporary + c


print(mul_add(2.0, 3.0, 4.0), inplace(4.0, 2.0, 3.0))
print(norm_squared(1.0, 2.0, 3.0))
epsilon = 2.0 ** -27
print(subtract_chain(1.0 + epsilon, 1.0 - epsilon, 1.0))
print(add_chain(1e16, -1e16, 1.0))
print(mul_add(-0.0, 1.0, -0.0))
print(math.isinf(subtract_chain(1e308, 2.0, 1e308)))
print(math.isnan(subtract_chain(float('inf'), 1.0, float('inf'))))
print(math.isnan(mul_add(float('nan'), 1.0, 2.0)))
print(mul_add(True, 2.0, 1.0), mul_add(2, 3, 4), mul_add(10 ** 40, 2, 1))
try:
    mul_add(10 ** 400, 1.0, 0.0)
except OverflowError:
    print('overflow conversion')

values = observed_product(3.0, 4.0, 5.0)
print(values[0] is values[1], values[0], values[2])
events = []
print(line_boundary(2.0, 3.0, 4.0), events)


class Right:
    def __radd__(self, value):
        events.append(('radd', type(value).__name__, value))
        return 17


events = []
print(mul_add(2.0, 3.0, Right()), events)


class Accumulator:
    def __iadd__(self, value):
        events.append(('iadd', value))
        return self


accumulator = Accumulator()
events = []
print(inplace(accumulator, 2.0, 3.0) is accumulator, events)


class Fallback:
    def __iadd__(self, value):
        events.append(('iadd fallback', value))
        return NotImplemented

    def __add__(self, value):
        events.append(('add fallback', value))
        return self


fallback = Fallback()
events = []
print(inplace(fallback, 2.0, 3.0) is fallback, events)


class Product:
    def __add__(self, value):
        events.append(('add error', value))
        raise ValueError('stop')


class Left:
    def __mul__(self, value):
        events.append(('mul', value))
        return Product()


def rhs():
    events.append('rhs')
    return 4.0


def after():
    events.append('after')
    return 9.0


def effect_chain(a, b):
    return (a * b + rhs()) + after()


events = []
try:
    mul_add(Left(), 3.0, 4.0)
except ValueError:
    print(events)

events = []
try:
    effect_chain(Left(), 3.0)
except ValueError:
    print(events)
