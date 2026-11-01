# ryupy: pending
# 已登记不支持：动态装饰器返回Enum基类不能绕过HIR成员范围和Finalize。
# CPython能动态创建根Enum子类；RyuPy明确NotImplementedError，本例不计入兼容通过。
from enum import Enum

def root(cls):
    return Enum
@root
class DynamicRoot:
    pass
try:
    class Child(DynamicRoot):
        VALUE = 1
    print(Child.VALUE.name)
except NotImplementedError:
    print('unsupported-dynamic-root')

class Finished(Enum):
    VALUE = 1

def finished(cls):
    return Finished
@finished
class DynamicFinished:
    pass
try:
    class Other(DynamicFinished):
        OTHER = 2
except TypeError:
    print('completed-enum-type-error')
except NotImplementedError:
    print('unsupported-dynamic-completed')
