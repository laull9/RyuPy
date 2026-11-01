# 两lane只配对独立新鲜链；每lane保持原操作顺序与自己的最终盒。
import math


def pair(a, b, c, d, e, f):
    return [a * b + c, d * e + f]


def asymmetric(a, b, c, d, e, f):
    return [(a - b) * c, (d - e) * f]


def recurrence(a, b, c, d, e, f):
    return [((a * b + c) * b + c) * b + c,
            ((d * e + f) * e + f) * e + f]


print(pair(2.0, 3.0, 5.0, 7.0, 11.0, 13.0))
print(asymmetric(11.0, 3.0, 2.0, 5.0, 7.0, 3.0))
print(recurrence(2.0, 3.0, 4.0, 5.0, 2.0, 1.0))
x = 2.0 ** -27
rounded = pair(1.0 + x, 1.0 - x, -1.0, 1e16, 1.0, -1e16)
print(rounded, rounded[1] + 1.0)
zeros = pair(-0.0, 1.0, -0.0, 0.0, 1.0, 0.0)
print(repr(zeros[0]), repr(zeros[1]))
limits = pair(1e308, 2.0, -1e308, float('inf'), 1.0, -float('inf'))
print(math.isinf(limits[0]), math.isnan(limits[1]))
identical = pair(2.0, 3.0, 5.0, 2.0, 3.0, 5.0)
print(identical[0] == identical[1], identical[0] is identical[1])
shared_input = float('nan')
nans = pair(shared_input, 1.0, 0.0, shared_input, 1.0, 0.0)
print(math.isnan(nans[0]), math.isnan(nans[1]), nans[0] is nans[1])

# 相同已有值不是两个独立新鲜产物，不能因数值相同或sink相同而共用盒。
def shared(a, b):
    value = a * b
    return [value, value]


alias = shared(2.0, 3.0)
print(alias[0] is alias[1])


def observed(a, b, c, d, e, f):
    left = a * b + c
    alias = left
    right = d * e + f
    return [alias, right]


print(observed(2.0, 3.0, 5.0, 7.0, 11.0, 13.0))
log = []
class Product:
    def __init__(self, label, fail):
        self.label = label
        self.fail = fail
    def __add__(self, value):
        log.append(('add', self.label, value))
        if self.fail:
            raise ValueError(self.label)
        return self
class Factor:
    def __init__(self, label, fail):
        self.label = label
        self.fail = fail
    def __mul__(self, value):
        log.append(('mul', self.label, value))
        return Product(self.label, self.fail)


# 任何guard失败时原lane0完整执行后才lane1；失败不能重复前缀或抢跑后lane。
for failing in ['left', 'right']:
    log.clear()
    try:
        pair(Factor('left', failing == 'left'), 2.0, 3.0,
             Factor('right', failing == 'right'), 5.0, 7.0)
    except ValueError as error:
        print(str(error), log)
print(pair(True, 2.0, 3.0, 4.0, 5.0, 6.0))
print(pair(2, 3.0, 4.0, 5.0, 6.0, 7.0))
try:
    pair(10 ** 400, 1.0, 0.0, Factor('unreachable', True), 1.0, 0.0)
except OverflowError:
    print('overflow before second lane')


def argument(label, value):
    log.append(label)
    return value


log.clear()
print(pair(argument('a', 1.0), argument('b', 2.0), argument('c', 3.0),
           argument('d', 4.0), argument('e', 5.0), argument('f', 6.0)))
print(log)

# 奇数尾项不移动、不复制；原求值异常也不能被packed快路吞掉。
def tail():
    log.append('tail')
    return 'third'
def with_tail(a, b, c, d, e, f):
    return [a * b + c, d * e + f, tail()]


log.clear()
print(with_tail(1.0, 2.0, 3.0, 4.0, 5.0, 6.0), log)
log.clear()
try:
    with_tail(Factor('left', True), 1.0, 2.0, 3.0, 4.0, 5.0)
except ValueError:
    print(log)
