# 跨模块后添默认值触发通用核心；canonical的对象/元数据及参数源序均保持。
import c2_function_companion_helper as helper
import c2_function_companion_other as other

original = helper.required
alias = helper.required
value = ["mutable"]
metadata = (value,)
helper.required.__defaults__ = metadata
print(original is alias, alias is helper.required)
print(helper.required.__defaults__ is metadata)
print(helper.required() is value, helper.required() is helper.required())
container = [helper.required(), alias()]
print(container[0] is container[1], container[0] is value)
value.append("changed")
print(container)

for object_value in [None, True, 3.5, "string", {"key": 3}, (1, 2), 1000000000000000000000000000001]:
    metadata = (object_value,)
    helper.required.__defaults__ = metadata
    print(helper.required.__defaults__ is metadata, helper.required() is object_value)
    print(helper.required())

helper.forward.__defaults__ = (value,)
helper.recursive.__defaults__ = (value, 4)
print(helper.forward() is value, helper.recursive() is value)
helper.keyword.__kwdefaults__ = {"other": value}
print(helper.keyword(0) is value)
helper.keyword.__kwdefaults__["other"] = "keyword-change"
print(helper.keyword(0))

other.required.__defaults__ = ("other",)
helper.required.__defaults__ = ("same-name",)
print(helper.required(), other.required())
print(helper.numeric_call(5))
helper.sibling = other.required
helper.forward.__defaults__ = ("forwarded",)
print(helper.forward())

order = []
def argument():
    order.append("argument")
    helper.recursive.__defaults__ = (value, 0)
    return "explicit"
print(helper.recursive(argument()), order)
saved_recursive = helper.recursive
def rebind_argument():
    order.append("rebind")
    helper.recursive = other.required
    return value
print(helper.recursive(rebind_argument()) is value, helper.recursive is other.required)
helper.recursive = saved_recursive
helper.effect.__defaults__ = (value,)
print(helper.effect() is value)
try:
    helper.effect(1, value=2)
except TypeError as error:
    print(type(error).__name__, str(error))
helper.required.__defaults__ = None
try:
    helper.required()
except TypeError as error:
    print(type(error).__name__, str(error))
del helper.keyword.__kwdefaults__
try:
    helper.keyword(0)
except TypeError as error:
    print(type(error).__name__, str(error))
print(alias is original)
