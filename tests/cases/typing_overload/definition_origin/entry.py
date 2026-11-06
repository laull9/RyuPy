# ryupy: pending
# 首装饰器行号/词法限定名来源；名字属性的后续修改不篡改不可变代码来源。
from typing import overload, get_overloads, clear_overloads

clear_overloads()
seen = []
def retain(function):
    seen.append(function)
    return function

@retain
@retain
def decorated():
    pass

class Container:
    __qualname__ = "RenamedClass"
    @retain
    def method(self):
        pass

    def factory(self):
        @retain
        def inner():
            pass
        return inner

nested = Container().factory()
print(decorated.__code__.co_firstlineno, seen[0] is seen[1])
print(Container.__qualname__, Container.method.__qualname__, nested.__qualname__)
print(Container.method.__code__.co_firstlineno, nested.__code__.co_firstlineno)
print(decorated.__code__ is decorated.__code__)
old_code = decorated.__code__
decorated.__name__ = "renamed"
decorated.__qualname__ = "target"
decorated.__module__ = "alternate"
print(old_code.co_name, old_code.co_qualname, decorated.__name__, decorated.__qualname__)
print(overload(decorated).__name__, get_overloads(decorated)[0] is decorated)
decorated.__dict__["__qualname__"] = "spoof"
decorated.__dict__["__module__"] = "spoof"
decorated.__dict__["__code__"] = "spoof"
print(decorated.__qualname__, decorated.__module__, decorated.__code__ is old_code)
try:
    decorated.__qualname__ = 8
except TypeError as error:
    print(type(error).__name__, str(error))
try:
    del decorated.__qualname__
except TypeError as error:
    print(type(error).__name__, str(error))
print(clear_overloads())
