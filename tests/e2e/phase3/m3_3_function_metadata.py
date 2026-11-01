# 函数元数据属于实际函数对象；包装不会自动复制原函数信息。
def function(value: int = 2) -> str:
    "真实文档"
    return str(value)


print(function.__name__, function.__doc__)
print(function.__annotations__["value"] is int, function.__annotations__["return"] is str)
function.custom = 3
print(function.__dict__["custom"])
function.__dict__["custom"] = 4
print(function.custom)
function.__name__ = "renamed"
function.__doc__ = None
print(function.__name__, function.__doc__)
del function.custom
print(hasattr(function, "custom"))
function.__annotations__ = None
print(function.__annotations__)


def wrap(original):
    def wrapper():
        return original()
    return wrapper


@wrap
def decorated():
    "原文档"
    return 5


print(decorated.__name__, decorated.__doc__, decorated())
print(len is len, repr(len))
callback = len
print(callback([1, 2, 3]))
