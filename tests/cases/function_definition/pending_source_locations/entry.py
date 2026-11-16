# ryupy: pending
# 装饰器表达式跨行、lambda、global重定义的限定名来源，禁止以func_id/运行时类名代替。
from typing import overload, get_overloads, clear_overloads

clear_overloads()
seen = []
def retain(value):
    seen.append(value)
    return value

@(
    retain
)
def multiline():
    pass

factory = lambda: (lambda: 1)
inner_lambda = factory()
print(multiline.__code__.co_firstlineno, multiline.__qualname__)
print(factory.__qualname__, inner_lambda.__qualname__)

def outer():
    global promoted
    @retain
    def promoted():
        return "global"
    return promoted

promoted = outer()
print(promoted.__qualname__, promoted.__code__.co_firstlineno, promoted())
print(overload(promoted).__name__, get_overloads(promoted)[0] is promoted)
print(clear_overloads())
