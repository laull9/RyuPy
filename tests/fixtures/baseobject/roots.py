# 实际根关系和已存在单继承；tuple匹配短路不能提前验证后项。
from typing import TypedDict
class Plain:
    pass
class Explicit(object):
    pass
class Child(Explicit):
    pass
Root = object
class Alias(Root):
    pass
for cls in [Plain, Explicit, Alias]:
    assert cls.__bases__ == (object,)
    instance = cls()
    instance.value = 7
    assert instance.value == 7 and type(instance) is cls
    assert isinstance(instance, object) and issubclass(cls, object)
assert Child.__bases__ == (Explicit,)
assert isinstance(Child(), Explicit) and issubclass(Child, Explicit)
def function():
    pass
for value in [None, True, 1, 1.5, 's', [], {}, (), function, Plain, type, object, ValueError('x')]:
    assert isinstance(value, object)
for cls in [int, bool, str, list, dict, type, object, ValueError, Plain, Child]:
    assert issubclass(cls, object)
assert isinstance(1, (object, 42))
assert not isinstance(1, ())
assert issubclass(int, (object, 42))
assert not issubclass(int, ()) and not issubclass(42, ())
for check in [lambda: isinstance(1, 42), lambda: isinstance(1, (42, object)), lambda: issubclass(42, object), lambda: issubclass(int, (42, object))]:
    try:
        check()
    except TypeError:
        pass
    else:
        raise AssertionError('invalid root check accepted')
class Record(TypedDict):
    value: int
for check in [lambda: isinstance({}, Record), lambda: issubclass(dict, Record)]:
    try:
        check()
    except TypeError:
        pass
    else:
        raise AssertionError('TypedDict runtime check accepted')
print('roots-ok')
