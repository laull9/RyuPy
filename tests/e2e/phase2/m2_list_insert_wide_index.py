# 64位插入下标必须先归一化再截断；CP同次产生预期，无生成golden。
for index in [-9223372036854775808, -4294967296, -2147483649, -2147483648,
              -4, -3, -1, 0, 2, 3, 9223372036854775807]:
    values = [10, 20, 30]
    marker = ["new"]
    values.insert(index, marker)
    print(index, values, any(x is marker for x in values))
empty = []
empty.insert(-9223372036854775808, "first")
empty.insert(9223372036854775807, "last")
print(empty)
# 回调可改变列表，归一化必须发生在__index__完成之后。
values = [1, 2, 3]
class Index:
    def __index__(self):
        values.clear()
        values.extend([7, 8])
        return -4294967296
values.insert(Index(), "front")
print(values)
calls = []
class Throwing:
    def __index__(self):
        calls.append("once")
        raise ValueError("index exploded")
try:
    values.insert(Throwing(), "not inserted")
except ValueError as error:
    print(type(error).__name__, str(error), calls, values)
class Bad:
    def __index__(self):
        return "bad"
for index in [1.5, 2 ** 100, -(2 ** 100), Bad()]:
    before = values[:]
    try:
        values.insert(index, "not inserted")
    except (TypeError, OverflowError) as error:
        print(type(error).__name__, str(error), values == before)
class Typed:
    def __index__(self):
        calls.append("type-slot")
        return 0
typed = Typed()
typed.__index__ = lambda: 999
values.insert(typed, "typed-first")
print(values, calls)
