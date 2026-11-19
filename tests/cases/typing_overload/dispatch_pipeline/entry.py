# 完整公开语义已通过固定CP与B3差分；保原源码行号。
# 完整应用：带两个公开重载声明的编码流水线，实作不按注解限制输入。
import typing
from typing import overload, get_overloads, clear_overloads

clear_overloads()
saved = []

def retain(function):
    saved.append(function)
    return function

@overload
@retain
def encode(value: int) -> str:
    """整数声明。"""
    return "integer stub"

first_dummy = encode

@overload
@retain
def encode(value: str) -> str:
    return "text stub"

second_dummy = encode

def encode(value):
    return "value=" + str(value)

print([encode(value) for value in [12, "x", None]])
registered = get_overloads(encode)
print(first_dummy is second_dummy, first_dummy is typing._overload_dummy)
print(len(registered), registered[0] is saved[0], registered[1] is saved[1])
print(registered[0](3), registered[1]("x"))
print(registered[0].__name__, registered[0].__qualname__, registered[0].__module__)
print(registered[0].__annotations__["value"] is int, registered[0].__doc__)
print(get_overloads(first_dummy))
try:
    first_dummy(1, arbitrary=True)
except NotImplementedError as error:
    print(type(error).__name__, str(error))
first = get_overloads(encode)
second = get_overloads(encode)
print(first is second, first[0] is second[0])
first.clear()
print(len(get_overloads(encode)))
print(clear_overloads(), get_overloads(encode), len(second))
