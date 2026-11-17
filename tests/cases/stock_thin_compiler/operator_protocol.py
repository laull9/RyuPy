# ryupy: pending
# type/staticmethod只组装类型，魔术方法全部是真实编译薄体；验证反射优先级、异常及身份。
import builtins

events = []
record = events.append
marker = object()
managed_lookup = {}.__getitem__
native_lookup = getattr(builtins, "dict")().__getitem__
lookup = managed_lookup

def decline(x):
    record("left")
    return NotImplemented

def reflected(x):
    record("right")
    return marker

def fail(x):
    record("fail")
    return lookup("missing")

def add(x):
    return x + right

def false_value(x):
    record("bool")
    return False

def true_value(x):
    record("bool-true")
    return True

def empty_length(x):
    record("len")
    return 0

def bad_bool(x):
    record("bad-bool")
    return "bad"

def bad_length(x):
    record("bad-len")
    return -1

def choose(x):
    if x:
        return "yes"
    return "no"

def conditional(x):
    return "yes" if x else "no"

Base = type("Base", (), {"__add__": staticmethod(decline)})
Derived = type("Derived", (Base,), {"__radd__": staticmethod(reflected)})
Unrelated = type("Unrelated", (), {"__radd__": staticmethod(reflected)})
Broken = type("Broken", (), {"__radd__": staticmethod(fail)})
left = Base()
right = Derived()
print(add(left) is marker, events)
events.clear()
right = Unrelated()
print(add(left) is marker, events)
events.clear()
right = Broken()
try:
    add(left)
except KeyError as error:
    print(type(error).__name__, error.args, str(error), events)
events.clear()
lookup = native_lookup
try:
    add(left)
except KeyError as error:
    print(type(error).__name__, error.args, str(error), events)
events.clear()
right = object()
try:
    add(left)
except TypeError as error:
    print(type(error).__name__, error.args, str(error), events)
for protocol, body in [("__bool__", false_value), ("__bool__", true_value), ("__len__", empty_length)]:
    events.clear()
    Value = type("Value", (), {protocol: body})
    value = Value()
    print(choose(value), conditional(value), events)
for protocol, body in [("__bool__", bad_bool), ("__len__", bad_length)]:
    events.clear()
    Value = type("Value", (), {protocol: body})
    try:
        conditional(Value())
    except Exception as error:
        print(type(error).__name__, error.args, str(error), events)
