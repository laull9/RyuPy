# 完整公开语义已通过固定CP与B3差分；保原源码行号。
# 独立完整主源：真实module键、当前qualname、反向登记顺序与单层function属性解包。
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

other.__module__ = "fixed"
original.__module__ = "fixed"
original.__qualname__ = other.__qualname__
overload(other)
overload(original)
overload(other)
values = get_overloads(original)
print(len(values), values[0] is other, values[1] is original)
print([value() for value in values])
original.__name__ = "renamed"
print(get_overloads(original)[1] is original)
print(clear_overloads())

def alternate():
    return "alternate"
original.__func__ = alternate
print(overload(original) is overload(alternate))
print(get_overloads(alternate)[0] is alternate)
clear_overloads()
overload(original)
print(get_overloads(alternate)[0] is original, get_overloads(original)[0] is original)
print(clear_overloads())
