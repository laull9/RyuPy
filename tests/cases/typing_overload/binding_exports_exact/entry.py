# 完整公开语义已通过固定CP与B3差分；保原源码行号。
# 独立完整主源：精确函数的参数绑定、保存导出和共享dummy重绑，不消费native代码来源。
import typing
from typing import overload, get_overloads, clear_overloads

clear_overloads()
def function():
    return "real"

def report(label, action):
    try:
        value = action()
        if value is typing._overload_dummy:
            print(label, "dummy")
        else:
            print(label, value)
    except Exception as error:
        print(label, type(error).__name__, str(error))

print(overload.__name__, overload.__qualname__, overload.__module__)
print(get_overloads.__name__, clear_overloads.__name__)
report("empty overload", lambda: overload())
report("two overload", lambda: overload(function, function))
report("keyword overload", lambda: overload(func=function))
report("bad overload", lambda: overload(bad=function))
report("duplicate overload", lambda: overload(function, func=function))
report("too many plus unknown", lambda: overload(function, function, bad=3))
report("too many plus duplicate", lambda: overload(function, function, func=function))
report("empty get", lambda: get_overloads())
report("two get", lambda: get_overloads(function, function))
report("keyword get", lambda: len(get_overloads(func=function)))
report("bad get", lambda: get_overloads(bad=function))
report("get too many unknown", lambda: get_overloads(function, function, bad=3))
report("get too many duplicate", lambda: get_overloads(function, function, func=function))
report("clear positional", lambda: clear_overloads(1))
report("clear keyword", lambda: clear_overloads(bad=1))
report("clear both", lambda: clear_overloads(1, bad=1))
for value in [None, 1, "x", [], {}]:
    print("nonfunction", type(value).__name__, overload(value) is typing._overload_dummy)
report("get none", lambda: get_overloads(None))
report("dummy no arguments", lambda: typing._overload_dummy())
report("dummy any arguments", lambda: typing._overload_dummy(1, 2, x=3))

saved_overload, saved_get, saved_clear = overload, get_overloads, clear_overloads
typing.overload, typing.get_overloads, typing.clear_overloads = 11, 12, 13
print(saved_overload(function) is typing._overload_dummy)
print(saved_get(function)[0] is function, saved_clear())
typing.overload, typing.get_overloads, typing.clear_overloads = saved_overload, saved_get, saved_clear

original_dummy = typing._overload_dummy
replacement = []
typing._overload_dummy = replacement
print(overload(function) is replacement, get_overloads(function)[0] is function)
typing._overload_dummy = original_dummy
print(overload(function) is original_dummy)
print(clear_overloads())
