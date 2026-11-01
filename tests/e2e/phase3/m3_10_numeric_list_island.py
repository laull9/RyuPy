# 数值列表仍是真list；生产、再消费与所有可观察复制均保元素盒身份。
def produce(a, b, count):
    rows = []
    for outer in range(1):
        row = []
        for column in range(2):
            total = 0.0
            for index in range(count):
                total += a[index] * b[index]
            row.append(total)
        rows.append(row)
    return rows[0]

row = produce([1.5, 2.0], [2.0, 3.0], 2)
# 先再消费raw结果，再复制；repr在这些步骤之后，不能提前物化掩盖测试。
print(produce(row, row, 2))
copies = [row.copy(), row[:], row * 2, list(row), row + row]
print(row, type(row).__name__)
value = row[0]
for copied in copies:
    print(copied[0] is value)
print(value is row[0], value is row.copy()[0], value is row[:][0])
print(value is (row * 2)[0], value is list(row)[0], value is (row + row)[0])
row[0] = 'changed'
print(row, value)

nan_row = produce([float('nan')], [1.0], 1)
nan_value = nan_row[0]
print(nan_value != nan_value, nan_value is nan_row[0])
print(nan_value in nan_row, nan_row.index(nan_value), float('nan') in nan_row)
print(nan_value is nan_row.copy()[0], nan_value is nan_row[:][0])
print(nan_value is list(nan_row)[0], nan_value is (nan_row * 2)[0])

shared = float('1234.5')
class Product:
    def __radd__(self, left):
        return shared
class Factor:
    def __mul__(self, right):
        return Product()
slow = produce([Factor()], [2.0], 1)
print(slow, slow[0] is shared, slow[1] is shared)
recovered = produce([Factor(), 1.0], [2.0, 0.0], 2)
print(recovered, recovered[0] is shared)

# 零次循环沿原append路径，保每次初始化的盒而非从raw重造。
def zero(initial):
    rows = []
    for outer in range(1):
        row = []
        for column in range(1):
            total = initial
            for index in range(0):
                total += [1.0][index] * [2.0][index]
            row.append(total)
        rows.append(row)
    return rows[0]
print(zero(shared)[0] is shared)
empty_row = produce([], [], 0)
print(empty_row, empty_row[0] is empty_row[1])

log = []
class Inputs:
    def __init__(self, label, values):
        self.label = label
        self.values = values
    def __getitem__(self, key):
        log.append((self.label, key))
        return self.values[key]
print(produce(Inputs('a', [1.0]), Inputs('b', [2.0]), 1))
print(log)
log.clear()
try:
    produce(Inputs('a', []), Inputs('b', [2.0]), 1)
except IndexError:
    print('first failed', log)

# 非整数索引不得在预检中执行__index__，只能原getitem路径执行一次。
class Index:
    def __index__(self):
        log.append('index')
        return 0
idx = Index()
def indexed(a, b, key):
    rows = []
    for outer in range(1):
        row = []
        for column in range(1):
            total = 0.0
            for iteration in range(1):
                total += a[key] * b[key]
            row.append(total)
        rows.append(row)
    return rows[0]
log.clear()
print(indexed([2.0], [3.0], idx), log)
log.clear()
try:
    indexed([], [3.0], idx)
except IndexError:
    print('index failed', log)
