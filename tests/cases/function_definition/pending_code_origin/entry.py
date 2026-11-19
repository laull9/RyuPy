# ryupy: pending
# 完整CP正例：公开代码对象、真实首行与替换；本次只支持内部冷来源，不扩公开code。
def retain(function):
    return function

@(
    retain
)
def decorated(value=1):
    return value + 1

def replacement(value=2):
    return value * 3

def generator(value):
    yield value

factory = lambda: (lambda: 4)
inner = factory()
original = decorated.__code__
assert original.co_firstlineno == 7
assert original.co_name == "decorated" and original.co_qualname == "decorated"
assert generator.__code__.co_firstlineno == 15
assert factory.__code__.co_firstlineno == 18
assert inner.__code__.co_firstlineno == 18
assert inner.__code__.co_qualname == "<lambda>.<locals>.<lambda>"
decorated.__qualname__ = "changed"
decorated.__name__ = "short"
decorated.__dict__["__code__"] = "dictionary-spoof"
assert decorated.__code__ is original
assert original.co_name == original.co_qualname == "decorated"
decorated.__code__ = replacement.__code__
assert decorated(4) == 12 and decorated.__code__ is replacement.__code__
assert decorated.__qualname__ == "changed" and decorated.__name__ == "short"
try:
    del decorated.__code__
except TypeError as error:
    print(type(error).__name__, str(error))
else:
    raise AssertionError("代码对象删除未报错")
decorated.__code__ = original
assert decorated(4) == 5
print(original.co_firstlineno, generator.__code__.co_firstlineno)
print(factory.__code__.co_firstlineno, inner.__code__.co_firstlineno)
print(original.co_name, original.co_qualname, inner.__code__.co_qualname)
print(decorated.__qualname__, decorated.__name__, decorated(4), list(generator(6)))
