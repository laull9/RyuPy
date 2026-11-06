# ryupy: pending
# CP完整正例：模块helper重绑必须在实际读取阶段生效；无递归回调。
import typing
from typing import overload, get_overloads, clear_overloads

clear_overloads()
events = []
def one():
    return 1

def two():
    return 2

original_registry = typing._overload_registry
original_dummy = typing._overload_dummy

def lookup(value, name, default):
    events.append("getattr:" + name)
    return two

typing.getattr = lookup
print(overload(one) is original_dummy, events)
del typing.getattr
print(get_overloads(one), get_overloads(two)[0] is one)

def list_replacement(values):
    events.append("list")
    return ("replaced", len(tuple(values)))

typing.list = list_replacement
events.clear()
print(get_overloads(one), events)
print(get_overloads(two), events)
del typing.list

class Registry:
    def __getitem__(self, key):
        events.append("getitem:" + str(key))
        return self
    def __setitem__(self, key, value):
        events.append("setitem:" + str(key))
    def __contains__(self, key):
        events.append("contains:" + str(key))
        return True
    def values(self):
        events.append("values")
        return [one]
    def clear(self):
        events.append("clear")
        return 7

replacement = Registry()
typing._overload_registry = replacement
events.clear()
print(overload(one) is original_dummy, events)
events.clear()
print(get_overloads(one)[0] is one, events)
events.clear()
print(clear_overloads(), events)
typing._overload_registry = original_registry

# 清空只读取当前registry；返回始终None，不转发clear的返回值。
print(len(get_overloads(two)))
print(clear_overloads())
