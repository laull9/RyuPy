# 真实Generic根、空布局与普通类同名不混淆。
import typing as t
from typing import Generic
assert Generic is t.Generic and type(Generic) is type
assert Generic.__module__ == 'typing'
assert Generic.__name__ == 'Generic' and Generic.__qualname__ == 'Generic'
assert Generic.__base__ is object and Generic.__bases__ == (object,)
assert Generic.__mro__ == (Generic, object)
assert Generic.mro() == [Generic, object]
assert t.get_origin(Generic) is Generic and t.get_args(Generic) == ()
for name in ('__slots__', '__parameters__', '__orig_bases__'):
    assert not hasattr(Generic, name)
T = t.TypeVar('T')
for value in (Generic(), Generic[T](), object.__new__(Generic)):
    assert type(value) is Generic and value.__class__ is Generic
    assert isinstance(value, Generic) and isinstance(value, object)
    for name in ('__dict__', '__orig_class__', '__weakref__'):
        assert not hasattr(value, name)
    try:
        value.x = 1
    except AttributeError as error:
        print('write', str(error))
    else:
        assert False
    try:
        del value.x
    except AttributeError as error:
        print('delete', str(error))
    else:
        assert False
class Generic:
    pass
assert Generic is not t.Generic
assert not isinstance(Generic(), t.Generic)
print('root', True)
