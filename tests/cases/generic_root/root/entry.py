# 真实Generic根、空布局与普通类同名不混淆。
import typing as t
from typing import Generic as ImportedGeneric
assert ImportedGeneric is t.Generic and type(ImportedGeneric) is type
assert ImportedGeneric.__module__ == 'typing'
assert ImportedGeneric.__name__ == 'Generic' and ImportedGeneric.__qualname__ == 'Generic'
assert ImportedGeneric.__base__ is object and ImportedGeneric.__bases__ == (object,)
assert ImportedGeneric.__mro__ == (ImportedGeneric, object)
assert ImportedGeneric.mro() == [ImportedGeneric, object]
assert t.get_origin(ImportedGeneric) is ImportedGeneric and t.get_args(ImportedGeneric) == ()
for name in ('__slots__', '__parameters__', '__orig_bases__'):
    assert not hasattr(ImportedGeneric, name)
T = t.TypeVar('T')
for value in (ImportedGeneric(), ImportedGeneric[T](), object.__new__(ImportedGeneric)):
    assert type(value) is ImportedGeneric and value.__class__ is ImportedGeneric
    assert isinstance(value, ImportedGeneric) and isinstance(value, object)
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
