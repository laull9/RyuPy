# 列表推导式：独立绑定、共享 cell、嵌套求值顺序及异常退出。
items = [1, 2, 3]
print([items * 2 for items in items], items)
print([(x, y) for x in range(4) if x > 0 for y in range(x) if y % 2 == 0])


def build(seed):
    item = "外层"
    late = [lambda: item for item in range(3)]
    early = [lambda item=item: item for item in range(3)]
    nested = [[seed + x + y for y in range(2)] for x in range(3)]
    print(item, nested)
    return late, early


late, early = build(10)
print([f() for f in late], [f() for f in early])
print([a + b for a, b in [(1, 2), (3, 4)]])
log = []


def mark(tag, value):
    log.append(tag)
    return value


print([mark("元素", x + y) for x in mark("外层", [1, 2])
       if mark("条件", x > 0) for y in mark("内层", [3, 4])])
print(log)
x = "未改变"
try:
    print([10 // x for x in [2, 0, 1]])
except ZeroDivisionError:
    print(x)

# 空循环不求值元素，生成空结果；每次执行都有新的捕获环境。
print([missing for missing in []])
print([f() for f in [lambda: x for x in [4, 5]]])
