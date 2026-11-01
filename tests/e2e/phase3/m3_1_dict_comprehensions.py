# 字典推导式按 key → value 求值；覆盖重复键、独立作用域和闭包。
log = []

def mark(label, value):
    log.append(label)
    return value

item = "外层"
print({mark("键", item % 2): mark("值", item) for item in range(5)})
print(log, item)
print({(left, right): left + right for left in range(3) if left > 0
       for right in range(left) if right % 2 == 0})
print({})
print({key: value for key, value in [("a", 1), ("b", 2), ("a", 3)]})


def build(seed):
    return {item: lambda: seed + item for item in range(3)}


functions = build(10)
print([functions[key]() for key in functions])

# key 在 value 之前求值，但哈希插入在两者之后；非法键也不能吞掉 value 的副作用。
log = []
try:
    print({mark("非法键", []): mark("值仍执行", 1) for item in [1]})
except TypeError as error:
    print(str(error), log)

log = []
try:
    print({10 // item: mark("值不执行", item) for item in [0]})
except ZeroDivisionError:
    print(log)

print({key: sorted({value for value in range(key)}) for key in range(3)})

# 合法键的 __hash__ 也有副作用，每次插入只能在 value 后调用一次。
log = []


class Key:
    def __hash__(self):
        log.append("哈希")
        return 11


keys = [Key(), Key()]
result = {mark("键", key): mark("值", 1) for key in keys}
print(len(result), log)
