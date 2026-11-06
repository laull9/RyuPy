# ryupy: pending
# 完整CP正例：保存API身份不变，改写函数成员后错误前缀读取当前限定名，结束时恢复。
import typing
from typing import overload, get_overloads, clear_overloads

clear_overloads()
saved_overload = overload
saved_get = get_overloads
saved_clear = clear_overloads
overload_metadata = (overload.__name__, overload.__qualname__, overload.__module__)
get_metadata = (get_overloads.__name__, get_overloads.__qualname__, get_overloads.__module__)
clear_metadata = (clear_overloads.__name__, clear_overloads.__qualname__, clear_overloads.__module__)

def target(value=1):
    return value

def report(label, action):
    try:
        action()
    except TypeError as error:
        print(label, type(error).__name__, str(error))

try:
    overload.__name__ = "renamed_overload"
    overload.__qualname__ = "Container.changed_overload"
    overload.__module__ = "alternate.module"
    print(overload is saved_overload, overload is typing.overload)
    print(overload.__name__, overload.__qualname__, overload.__module__)
    print(overload(func=target) is typing._overload_dummy, get_overloads(target)[0] is target)
    report("overload missing", lambda: overload())
    report("overload unknown", lambda: overload(bad=target))
    report("overload duplicate", lambda: overload(target, func=target))
    report("overload excess", lambda: overload(target, target))
    report("overload excess unknown", lambda: overload(target, target, bad=1))

    get_overloads.__name__ = "renamed_get"
    get_overloads.__qualname__ = "Container.changed_get"
    get_overloads.__module__ = "alternate.module"
    print(get_overloads is saved_get, get_overloads is typing.get_overloads)
    print(get_overloads.__name__, get_overloads.__qualname__, get_overloads.__module__)
    print(get_overloads(func=target)[0] is target)
    report("get missing", lambda: get_overloads())
    report("get unknown", lambda: get_overloads(bad=target))
    report("get duplicate", lambda: get_overloads(target, func=target))
    report("get excess", lambda: get_overloads(target, target))
    report("get excess duplicate", lambda: get_overloads(target, target, func=target))

    clear_overloads.__name__ = "renamed_clear"
    clear_overloads.__qualname__ = "Container.changed_clear"
    clear_overloads.__module__ = "alternate.module"
    print(clear_overloads is saved_clear, clear_overloads is typing.clear_overloads)
    print(clear_overloads.__name__, clear_overloads.__qualname__, clear_overloads.__module__)
    print(clear_overloads(), get_overloads(target))
    # clear没有必需形参或命名形参；无missing/形参重复场景，不引入通用**展开边界。
    report("clear unknown", lambda: clear_overloads(bad=1))
    report("clear excess", lambda: clear_overloads(1))
    report("clear excess unknown", lambda: clear_overloads(1, bad=1))
finally:
    overload.__name__, overload.__qualname__, overload.__module__ = overload_metadata
    get_overloads.__name__, get_overloads.__qualname__, get_overloads.__module__ = get_metadata
    clear_overloads.__name__, clear_overloads.__qualname__, clear_overloads.__module__ = clear_metadata
    clear_overloads()

print(overload.__name__, overload.__qualname__, overload.__module__)
print(get_overloads.__name__, get_overloads.__qualname__, get_overloads.__module__)
print(clear_overloads.__name__, clear_overloads.__qualname__, clear_overloads.__module__)
