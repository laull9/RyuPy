# 富比较返回任意对象：最终结果保身份，中间结果只在链式短路位置求真。
events = []

class Token:
    def __init__(self, name, truth):
        self.name = name
        self.truth = truth

    def __bool__(self):
        events.append(self.name)
        return self.truth

class Probe:
    def __init__(self, name, result):
        self.name = name
        self.result = result

    def __lt__(self, other):
        events.append(self.name)
        return self.result

truth = Token("truth", True)
false = Token("false", False)
a = Probe("a", truth)
b = Probe("b", false)
c = Probe("c", truth)
result = a < b
print(result is truth, events)
events.clear()
result = a < b < c
print(result is false, events)
events.clear()

def middle():
    events.append("middle")
    return b

def last():
    events.append("last")
    return c

result = b < middle() < last()
print(result is false, events)
events.clear()

def keep_result():
    value = a < b < c
    def saved():
        return value
    return saved

print(keep_result()() is false, events)
events.clear()

def suspended_chain():
    value = a < (yield b) < c
    yield value

generator = suspended_chain()
print(next(generator) is b)
print(generator.send(b) is false, events)
events.clear()

def explode():
    events.append("explode")
    raise ValueError("stop")

try:
    a < explode() < last()
except ValueError:
    print(events)
