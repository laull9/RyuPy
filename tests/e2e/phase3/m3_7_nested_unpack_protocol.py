# 独立迭代协议场景：外层先耗尽，深层迭代与叶目标副作用按顺序发生。
events = []

class Source:
    def __init__(self, name, values):
        self.name = name
        self.values = values
    def __iter__(self):
        events.append(self.name)
        return iter(self.values)

class Box:
    pass

box = Box()

def target():
    events.append("write")
    return box

source = Source("outer", [3, Source("inner", [4, 5])])
target().value, (a, b) = source
print(box.value, a, b, events)
events.clear()
try:
    target().value, (a, b), target().other = Source("outer", [6, Source("inner", [7]), 8])
except ValueError as error:
    print(box.value, events, str(error))

def partial():
    try:
        first, (second, third) = [10, []]
    except ValueError:
        print("first", first)
    try:
        print(second)
    except UnboundLocalError as error:
        print(type(error).__name__)

partial()
for number, [a, *middle, b] in [(1, "abcd"), (2, "xy")]:
    print(number, a, middle, b)

def closure():
    value = 0
    def assign():
        nonlocal value
        (a, (value, b)) = [21, [22, 23]]
        return a + b
    print(assign(), value)

closure()
