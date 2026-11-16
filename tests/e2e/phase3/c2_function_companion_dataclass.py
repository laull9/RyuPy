# 两模块类构造回归：原完整实参合法程序保持，新增默认对象由现有Dynamic体处理。
import c2_function_companion_dataclass_helper as helper

print(helper.make_box(1).x)
value = ["shared"]
helper.make_box.__defaults__ = (value,)
first = helper.make_box()
second = helper.make_box()
print(first.x is value, first.x is second.x)
print(type(first) is helper.Box, type(second) is helper.Box)
value.append("changed")
print(second.x)
