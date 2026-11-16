# ryupy: pending
# CP完整正例：任意descriptor与属性异常顺序；首切RyuPy须NI，不吞成不登记成功。
import typing
from typing import overload, get_overloads, clear_overloads

clear_overloads()
events = []
def base():
    return "body"

class Proxy:
    @property
    def __func__(self):
        events.append("unwrap")
        return base

proxy = Proxy()
print(overload(proxy) is typing._overload_dummy, get_overloads(base)[0] is proxy, events)
events.clear()
print(get_overloads(proxy)[0] is proxy, events)

class Metadata:
    def __getattribute__(self, name):
        if name == "__module__":
            events.append("module")
            return "custom.module"
        if name == "__qualname__":
            events.append("qualname")
            return "target"
        if name == "__code__":
            events.append("code")
            return base.__code__
        return object.__getattribute__(self, name)

value = Metadata()
events.clear()
print(overload(value) is typing._overload_dummy, events)
events.clear()
print(get_overloads(value)[0] is value, events)

class MissingCode:
    __module__ = "missing.module"
    @property
    def __code__(self):
        events.append("missing code")
        raise AttributeError("code absent")

missing = MissingCode()
missing.__qualname__ = "MissingCode"
events.clear()
print(overload(missing) is typing._overload_dummy, events)
print("missing.module" in typing._overload_registry, "MissingCode" in typing._overload_registry["missing.module"])

class Failing:
    @property
    def __func__(self):
        events.append("unwrap failure")
        raise ValueError("keep failure")

events.clear()
try:
    overload(Failing())
except ValueError as error:
    print(type(error).__name__, str(error), events)
# 普通function也能带自定义__func__；已存在该属性时不能径直用原function。
def alternate():
    return "alternate"
base.__func__ = alternate
print(overload(base) is typing._overload_dummy, get_overloads(alternate)[0] is base)
print(clear_overloads())
