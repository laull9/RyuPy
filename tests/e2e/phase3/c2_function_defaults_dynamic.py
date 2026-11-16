# 函数默认值属于真实元数据，动态调用即时消费修改、删除和共享字典变更。
from functools import wraps


def invoke(function, *args, **kwargs):
    return function(*args, **kwargs)


def value(a, /, b=2, *, required, option=4):
    return a, b, required, option


print(value.__defaults__, value.__kwdefaults__)
print(value.__defaults__ is value.__defaults__, value.__kwdefaults__ is value.__kwdefaults__)
print(invoke(value, 1, required=3))
replacement = ([8],)
value.__defaults__ = replacement
keywords = {"required": [9], "option": 10, "unused": 99}
value.__kwdefaults__ = keywords
print(value.__defaults__ is replacement, value.__kwdefaults__ is keywords)
result = invoke(value, 1)
print(result, result[1] is replacement[0], result[2] is keywords["required"])
keywords["option"] = 11
print(invoke(value, 1))
del keywords["required"]
try:
    invoke(value, 1)
except TypeError as error:
    print(type(error).__name__, str(error))
value.__defaults__ = (6, 7)
print(invoke(value, required=3))
value.__defaults__ = (5, 6, 7)
print(invoke(value, required=3))
value.__defaults__ = None
value.__kwdefaults__ = None
print(value.__defaults__, value.__kwdefaults__)
try:
    invoke(value)
except TypeError as error:
    print(type(error).__name__, str(error))
value.__defaults__ = ()
value.__kwdefaults__ = {}
print(value.__defaults__, value.__kwdefaults__)
del value.__defaults__
del value.__kwdefaults__
del value.__defaults__
del value.__kwdefaults__
print(value.__defaults__, value.__kwdefaults__)
for name, invalid in [("__defaults__", []), ("__kwdefaults__", ())]:
    try:
        setattr(value, name, invalid)
    except TypeError as error:
        print(type(error).__name__, str(error))
value.__dict__["__defaults__"] = (99,)
print(value.__defaults__)


def original(x=[]):
    return x


@wraps(original)
def wrapped(*args, **kwargs):
    return original(*args, **kwargs)


print(wrapped.__defaults__, wrapped.__wrapped__ is original)
print(invoke(wrapped) is original.__defaults__[0])
original.__defaults__ = ([12],)
print(invoke(wrapped) is original.__defaults__[0])
original.__annotations__["x"] = int
print(wrapped.__annotations__ is original.__annotations__)
