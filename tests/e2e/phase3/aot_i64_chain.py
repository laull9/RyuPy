# Add-only短链：原序、溢出、失配、观察与最终结果身份，不覆盖Sub/Mul优化。
def chain2(a, b, c):
    return a + b + c


def chain3(a, b, c, d):
    return a + b + c + d


def chain4(a, b, c, d, e):
    return a + b + c + d + e


print(chain2(1, 2, 3), chain3(1, 2, 3, 4), chain4(1, 2, 3, 4, 5))
maximum = 9223372036854775807
minimum = -9223372036854775808
# 首/中/末Add溢出；之后可返回i64范围，但不能丢任意精度中间值。
for values in [(maximum, 1, -1, 0, 0),
               (maximum, 0, 1, -1, 0),
               (maximum, 0, 0, 0, 1),
               (minimum, -1, 1, 0, 0),
               (minimum, 0, -1, 1, 0),
               (minimum, 0, 0, 0, -1)]:
    print(chain4(*values))
for values in [(True, 2, 3), (1, False, 3), (1, 2, True),
               (10 ** 60, 2, 3), (1, 10 ** 60, 3), (1, 2, 10 ** 60),
               (1.5, 2, 3), (1, 2.5, 3), (1, 2, 3.5)]:
    value = chain2(*values)
    print(value, type(value).__name__)

log = []
class Number:
    def __init__(self, label):
        self.label = label
    def __add__(self, other):
        log.append(('add', self.label, other))
        return self
    def __radd__(self, other):
        log.append(('radd', self.label, other))
        return self


for position in range(3):
    log.clear()
    item = Number('value')
    values = [1, 2, 3]
    values[position] = item
    print(chain2(*values) is item, log)

class Decline:
    def __add__(self, other):
        log.append('decline')
        return NotImplemented
class Right:
    def __radd__(self, other):
        log.append('right')
        return 10
log.clear()
print(chain2(Decline(), Right(), 3), log)

class RaiseAdd:
    def __add__(self, other):
        log.append('raise-add')
        raise ValueError('add-failed')
class RaiseRight:
    def __radd__(self, other):
        log.append(('raise-right', other))
        raise ValueError('right-failed')
for values in [(RaiseAdd(), 2, Number('never')),
               (1, RaiseRight(), Number('never')),
               (maximum, 1, RaiseRight())]:
    log.clear()
    try:
        chain2(*values)
    except ValueError as error:
        print(str(error), log)


def argument(label, value):
    log.append(label)
    return value
log.clear()
print(chain3(argument('a', 1), argument('b', 2), argument('c', 3), argument('d', 4)), log)

# 已有中间值观察必须保原对象；跨callback不得拼链。
def observed(a, b, c):
    middle = a + b
    result = middle + c
    return middle, result, middle


def observe(value):
    log.append(value)
    return 0


def interrupted(a, b, c):
    middle = a + b
    observe(middle)
    return middle + c


large = int('1000000000001')
result = chain2(large, 0, 0)
print(result == large, result is large)
parts = observed(large, 0, 0)
print(parts[0] is parts[2], parts[0] is parts[1], parts[0] is large)
log.clear()
print(interrupted(large, 0, 0), log)

shared = int('1000000000023')
class Shared:
    def __radd__(self, other):
        return shared
print(chain2(1, 2, Shared()) is shared)
print(chain3(1, 2, Shared(), 0) is shared)
# 小整数仅锁数值，缓存策略仍沿既有路径，不新承诺缓存身份。
print(chain2(0, 0, 0), chain4(-2, 1, 1, 1, -1))

# 末项inplace Add：普通int仍新结果，用户__iadd__必须回退而非偷换__add__。
def inplace(total, a, b):
    total += a + b
    return total


class Inplace:
    def __iadd__(self, value):
        log.append(('iadd', value))
        return self
    def __add__(self, value):
        raise AssertionError('inplace must not become ordinary add')


print(inplace(10, 2, 3), inplace(maximum, 1, 0), inplace(minimum, -1, 0))
log.clear()
mutable = Inplace()
print(inplace(mutable, 2, 3) is mutable, log)
