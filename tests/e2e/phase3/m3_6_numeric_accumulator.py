# holder仅替换无观察的+=，值类型变化/溢出/用户协议仍走完整Python语义。
def accumulate(initial, values):
    total = initial
    for i in range(len(values)):
        total += values[i]
    return total

for initial, values in [
    (9223372036854775807, [1, 2, -3]),
    (-9223372036854775808, [-1, -2, 3]),
    (10, [True, False, 2]),
    (10, [0.5, 2, -1]),
]:
    print(accumulate(initial, values))

initial = []
print(accumulate(initial, []) is initial)

class Result:
    def __init__(self, value):
        self.value = value
    def __iadd__(self, value):
        print('iadd', value)
        self.value += value
        return self

shared = Result(0)
class Reverse:
    def __radd__(self, value):
        print('radd', value)
        shared.value = value + 100
        return shared

result = accumulate(5, [Reverse(), 3])
print(result is shared, result.value)

class Source:
    def __init__(self):
        self.number = 0
    @property
    def value(self):
        self.number += 1
        print('get', self.number)
        return self.number

def properties(source, count):
    total = 0
    for i in range(count):
        total += source.value
    return total

print(properties(Source(), 3))

def shared_observation(initial, count):
    total = initial
    for i in range(count):
        total += i
    return total, total

pair = shared_observation(1000, 3)
print(pair[0], pair[0] is pair[1])

# 循环内别名/观察是拒绝优化条件；可变原地协议与别名仍保持。
def observed(initial, count):
    total = initial
    seen = []
    for i in range(count):
        seen.append(total)
        total += [i]
    return total, seen

initial = []
value, seen = observed(initial, 3)
print(value, value is initial, [item is initial for item in seen])

# 错误类型仍抛错，带except观察局部的循环不允许使用holder。
try:
    accumulate(0, [None])
except TypeError:
    print('type-error')

def protected(initial, values):
    total = initial
    try:
        for i in range(len(values)):
            total += values[i]
    except TypeError:
        return total
    return total

print(protected(10, [2, None]))
