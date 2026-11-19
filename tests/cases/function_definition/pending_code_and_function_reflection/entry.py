# ryupy: pending
# CP完整正例：代码替换、closure/default反射另列NI；绝不能用伪行号合并登记。
from typing import overload, get_overloads, clear_overloads

clear_overloads()
def first(value=1):
    return "first"
def second(value=2):
    return "second"

first.__qualname__ = "shared"
second.__qualname__ = "shared"
original = first.__code__
print(overload(first) is overload(second), len(get_overloads(first)))
first.__code__ = second.__code__
print(first(), first.__code__ is second.__code__)
overload(first)
values = get_overloads(first)
print(len(values), values[0] is first, values[1] is first)
print(first.__defaults__, first.__kwdefaults__, first.__closure__)
print(original.co_name, first.__code__.co_name)
first.__code__ = original
print(first(), clear_overloads())
