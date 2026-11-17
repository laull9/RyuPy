# 每层先准备本层值，再按源码顺序写目标；深层数量失败保留已完成的外层写入。
class Box:
    pass

box = Box()
values = [0, 0]
events = []

def target():
    events.append("target")
    return box

def index():
    events.append("index")
    return 1

a, (target().value, values[index()]) = [1, [2, 3]]
print(a, box.value, values, events)
a, [b, *middle, c], *tail = (4, range(5), 9, 10)
print(a, b, middle, c, tail)
first, *(second, third) = range(3)
print(first, second, third)
whole = head, (left, right) = [7, [8, 9]]
print(whole, head, left, right)
for idx, (name, count) in enumerate([("a", 2), ("b", 3)], start=1):
    print(idx, name, count)

events.clear()
box.value = 0
try:
    target().value, (b, c), values[index()] = [11, [12], 13]
except ValueError as error:
    print(type(error).__name__, str(error), box.value, events)
events.clear()
try:
    target().value, (b, c) = [1]
except ValueError as error:
    print(type(error).__name__, str(error), box.value, events)

# 先写入的目标可修改尚未开始拆分的内层容器。
inner = [21, 22]
inner[:], (left, right) = [[31, 32], inner]
print(left, right, inner)

# 空目标也必须执行迭代与数量检查；嵌套星号目标接收实际新列表。
empty, () = (7, iter([]))
print(empty)
try:
    empty, () = (8, iter([1]))
except ValueError as error:
    print(empty, type(error).__name__, str(error))

def suspended():
    a, (values[(yield "index")], b) = [41, [42, 43]]
    yield a, b, values

it = suspended()
print(next(it))
print(it.send(0))
