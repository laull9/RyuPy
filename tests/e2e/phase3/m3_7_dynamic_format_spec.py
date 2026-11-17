# 动态规格适用于数字和文本，保持value/spec/conversion/format的求值次序。
width = 5
precision = 2
print(f"{23:0{width}d}", f"{-23:0{width}d}", f"{3.14159:{width}.{precision}f}")
print(f"{'x':>{width}}", f"{23:0{width - 1}d}")
events = []

def value():
    events.append("value")
    return 12

def spec():
    events.append("spec")
    return 4

print(f"{value():0{spec()}d}", events)
events.clear()

class Value:
    def __str__(self):
        events.append("str")
        return "x"
    def __format__(self, specification):
        events.append("format:" + specification)
        return "result"

obj = Value()
print(f"{obj!s:>{spec()}}", events)
events.clear()
print(f"{obj:>{spec()}}", events)
events.clear()
try:
    print(f"{value():{1 // 0}}")
except ZeroDivisionError:
    print(events)
try:
    print(f"{23:{'bad'}}")
except ValueError as error:
    print(type(error).__name__, str(error))

def suspended():
    yield f"{12:0{(yield 'width')}d}"

it = suspended()
print(next(it), it.send(4))
