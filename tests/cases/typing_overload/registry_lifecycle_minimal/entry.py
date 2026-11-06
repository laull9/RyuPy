# 完整公开语义已通过固定CP与B3差分；保原源码行号。
# 最小完整应用：同定义点重复执行覆盖、列表快照、当前module/qualname与clear，不读代码对象。
from typing import overload, get_overloads, clear_overloads

clear_overloads()
saved = []
def retain(function):
    saved.append(function)
    return function

def factory(label):
    @overload
    @retain
    def convert(value: int):
        return label + ":int"
    @overload
    @retain
    def convert(value: str):
        return label + ":str"
    def convert(value):
        return label + ":" + str(value)
    return convert

old = factory("old")
snapshot = get_overloads(old)
new = factory("new")
current = get_overloads(old)
print(old(3), new(4), len(snapshot), len(current))
print(snapshot[0] is saved[0], current[0] is saved[2], current[1] is saved[3])
print(snapshot[0](0), current[0](0), current[0].__qualname__)
print(get_overloads(new)[0] is current[0])
current[0].__name__ = "renamed"
print(get_overloads(new)[0].__name__)
current[0].__module__ = "other.module"
print(get_overloads(current[0]))
print(overload(current[0]) is overload(current[1]))
print(len(get_overloads(current[0])), len(get_overloads(new)))
new.__module__ = "other.module"
print(get_overloads(new)[0] is current[0])
new.__qualname__ = "other_name"
print(get_overloads(new))
print(clear_overloads(), len(snapshot), get_overloads(old), get_overloads(new))
