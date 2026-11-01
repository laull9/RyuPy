# 直接有类型CIL累加快路必须保留失配回退、最终物化和同一结果的身份。
def accumulate(initial, values):
    total = initial
    for index in range(len(values)):
        total += values[index]
    return total, total

none_pair = accumulate(None, [])
print(none_pair[0] is None, none_pair[0] is none_pair[1])

initial = int('1000000000001')
empty = accumulate(initial, [])
print(empty[0] is initial, empty[0] is empty[1])
unchanged = accumulate(initial, [0])
print(unchanged[0], unchanged[0] is initial, unchanged[0] is unchanged[1])

# 回退返回的整数对象本身可能被外界持有，未再次运算时不能重新装箱。
shared = int('1000000000023')
class ReturnShared:
    def __radd__(self, other):
        print('radd', other)
        return shared

one = accumulate(0, [ReturnShared()])
two = accumulate(0, [ReturnShared(), 0])
print(one[0] is shared, one[0] is one[1])
print(two[0] == shared, two[0] is shared, two[0] is two[1])

# bool不是精确int64；宽整数溢出后仍能恢复为常见整数，浮点变化不能硬拆箱。
for start, values in [
    (True, []),
    (True, [0, 1]),
    (9223372036854775807, [1, -1, -9223372036854775807]),
    (-9223372036854775808, [-1, 1, 9223372036854775807]),
    (0, [1, 0.5, 2]),
]:
    pair = accumulate(start, values)
    print(pair[0], type(pair[0]).__name__, pair[0] is pair[1])

class Mutable:
    def __eq__(self, other):
        raise AssertionError('内部缓存判定不能执行Python相等协议')
    def __init__(self):
        self.count = 0
    def __iadd__(self, value):
        self.count += value
        print('iadd', self.count)
        return self

value = Mutable()
result = accumulate(value, [1, 2])
print(result[0] is value, result[0] is result[1], value.count)

# RHS回调只执行一次，不能在守卫失败时重新求值。
class Items:
    def __len__(self):
        return 3
    def __getitem__(self, index):
        print('get', index)
        return [1, 0.5, 2][index]

print(accumulate(0, Items())[0])
# 失败后不能继续请求下一项；异常回调也不能被快路吞掉。
class FailingItems:
    def __len__(self):
        return 3
    def __getitem__(self, index):
        print('before-failure', index)
        return [1, None, 3][index]

try:
    accumulate(0, FailingItems())
except TypeError:
    print('type-error')

class Raises:
    def __radd__(self, other):
        print('raising', other)
        raise ValueError('fallback-error')

try:
    accumulate(0, [1, Raises(), 3])
except ValueError as error:
    print(str(error))
