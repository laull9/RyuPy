# ryupy: pending
# 当前可变module/qualname是注册键，Name、代码qualified name和globals不替代它们。
import typing
from typing import overload, get_overloads, clear_overloads

clear_overloads()
def original():
    return "original"
def other():
    return "other"

original.__qualname__ = "shared"
other.__qualname__ = "shared"
original.__module__ = None
other.__module__ = None
print(overload(original) is overload(other), len(get_overloads(original)))
original.__module__ = 7
print(get_overloads(original))
overload(original)
print(get_overloads(original)[0] is original)
original.__module__ = [1]
try:
    overload(original)
except TypeError as error:
    print(type(error).__name__, str(error))
try:
    get_overloads(original)
except TypeError as error:
    print(type(error).__name__, str(error))
del original.__module__
print(original.__module__, len(get_overloads(original)))
original.__qualname__ = "not registered"
print(get_overloads(original))
print(clear_overloads())

# 重复同line登记覆盖原值但不改插入次序；结果不是按行号排序。
other.__module__ = "fixed"
original.__module__ = "fixed"
original.__qualname__ = other.__qualname__
overload(other)
overload(original)
overload(other)
values = get_overloads(original)
print(len(values), values[0] is other, values[1] is original)
print(values[0].__code__.co_firstlineno > values[1].__code__.co_firstlineno)
print(clear_overloads())
