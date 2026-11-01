# 浮点链跨循环留原生槽：初值、慢路返回盒及每次外层重入必须保持Python语义。
def accumulate(values):
    # 内层preheader避开旧entry-only整数holder；用真实新浮点循环状态验收。
    for outer in range(1):
        total = 0.0
        for index in range(len(values)):
            total += values[index] * 1.0
    return total


def repeated(rows):
    results = []
    for row_index in range(len(rows)):
        row = rows[row_index]
        total = 0.0
        for index in range(len(row)):
            total += row[index] * 1.0
        results.append(total)
    return results

print(accumulate([1.25, 2.5, -0.5]))
print(repeated([[1.25, 2.5], [], [3.0], [0.0, -0.0], []]))


def zero_identity(values):
    for outer in range(1):
        initial = 1000.25
        original = initial
        total = initial
        for index in range(len(values)):
            total += values[index] * 1.0
    return total is original, total

print(zero_identity([]), zero_identity([0.0]))


class Items:
    def __init__(self, values, failure):
        self.values = values
        self.failure = failure
        self.calls = []

    def __len__(self):
        print('len', len(self.values))
        return len(self.values)

    def __getitem__(self, index):
        self.calls.append(index)
        print('get', index)
        if index == self.failure:
            raise ValueError('getitem failure')
        return self.values[index]

items = Items([1.0, 2.0, 3.0], -1)
print(accumulate(items), items.calls)
failed = Items([1.0, 2.0, 3.0], 1)
try:
    accumulate(failed)
except ValueError as error:
    print(str(error), failed.calls)


class MutableTotal:
    def __init__(self):
        self.values = []

    def __iadd__(self, other):
        if isinstance(other, Token):
            self.values.append(other.name)
        else:
            self.values.append(other)
        print('iadd', self.values[-1])
        return self


class Token:
    def __init__(self, name):
        self.name = name

    def __mul__(self, factor):
        print('multiply', self.name, factor)
        return self

    def __radd__(self, left):
        print('radd', self.name, left)
        bucket.values.append(self.name)
        return bucket

bucket = MutableTotal()
for position in range(3):
    bucket.values.clear()
    values = [1.0, 2.0, 3.0]
    values[position] = Token('token')
    result = accumulate(values)
    print('mutable', position, result is bucket, bucket.values)
print(accumulate([Token('first'), Token('second'), 2.0]) is bucket)

shared = 1000.25


class SharedFloat:
    def __mul__(self, factor):
        print('shared-multiply', factor)
        return self

    def __radd__(self, left):
        print('shared-radd', left)
        return shared

for prefix in [[], [2.0], [2.0, 3.0]]:
    result = accumulate(prefix + [SharedFloat()])
    print('shared-exit', result is shared, result)
    result = accumulate(prefix + [SharedFloat(), 0.0])
    print('resume-exact', result is shared, result)
print(repeated([[SharedFloat()], [], [0.0], [SharedFloat(), 0.0]]))


class BadMultiply:
    def __mul__(self, other):
        print('bad-multiply')
        raise TypeError('multiply failure')

try:
    accumulate([1.0, BadMultiply(), 3.0])
except TypeError as error:
    print(str(error))


# 以下有可观察中间值或其它出口，必须保留通用循环，而不是仅结果恰好一致。
def observed(values):
    total = 0.0
    seen = []
    for index in range(len(values)):
        seen.append(total)
        total += values[index] * 1.0
    return seen, total


def interrupted(values):
    total = 0.0
    for index in range(len(values)):
        total += values[index] * 1.0
        if index == 1:
            break
    return total


def early_return(values):
    total = 0.0
    for index in range(len(values)):
        if index == 1:
            return total
        total += values[index] * 1.0
    return total

print(observed([1.0, 2.0, 3.0]))
print(interrupted([1.0, 2.0, 3.0]))
print(early_return([1.0, 2.0, 3.0]))


# 慢路返回None不能被内部CLR null脏标记误认成旧浮点结果。
class NoneResult:
    def __mul__(self, factor):
        return self

    def __radd__(self, left):
        return None

print('none-exit', accumulate([1.0, NoneResult()]) is None)
try:
    accumulate([1.0, NoneResult(), 2.0])
except TypeError:
    print('none-next-type-error')
print('empty-outer', repeated([]))
