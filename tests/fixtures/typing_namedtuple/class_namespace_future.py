from __future__ import annotations
# 尚未执行；future直接作用于真实模块，不用exec。
from typing import NamedTuple

shared = []
class Record(NamedTuple):
    "Record documentation."
    x: int
    y: list[int] = shared
    x: float
assert Record._fields == ('x', 'y')
assert Record.__doc__ == 'Record documentation.'
assert Record.__qualname__ == 'Record'
assert Record(1).y is shared and Record(2).y is shared
assert Record.__new__.__defaults__ == (shared,)
assert Record.__annotations__ is Record.__new__.__annotations__
assert Record.__annotations__['x'].__forward_arg__ == 'float'
assert Record.__annotations__['y'].__forward_arg__ == 'list[int]'
print('N2.defaults', Record._fields, Record(1).y is shared, Record.__doc__)

original = {'second': int, 'first': str}
class FinalNamespace(NamedTuple):
    ignored: Missing
    __annotations__ = original
assert FinalNamespace._fields == ('second', 'first')
assert FinalNamespace.__annotations__ == original
assert FinalNamespace.__annotations__ is not original
assert FinalNamespace.__annotations__ is FinalNamespace.__new__.__annotations__
FinalNamespace.__annotations__['later'] = float
assert FinalNamespace._fields == ('second', 'first')
assert FinalNamespace(1, 'a') == (1, 'a')
class NoDefaults(NamedTuple):
    x: int
class Empty(NamedTuple):
    pass
assert NoDefaults.__new__.__defaults__ == Empty.__new__.__defaults__ == ()
print('N2.namespace', FinalNamespace._fields, tuple(FinalNamespace.__annotations__), Empty._fields)

class Future(NamedTuple):
    a: Missing
    b: Missing
    c: 'Other'
a = Future.__annotations__['a']
b = Future.__annotations__['b']
c = Future.__annotations__['c']
assert type(a) is type(b) and a is not b and a == b and hash(a) == hash(b)
assert a.__forward_arg__ == 'Missing' and c.__forward_arg__ == "'Other'"
assert a.__forward_module__ is None and a.__forward_is_argument__ is True
assert a.__forward_is_class__ is False and a.__forward_evaluated__ is False
assert a.__forward_value__ is None
print('N2.future', repr(a), repr(c), a == b, a is not b)

module_token = object()
class Metadata(NamedTuple):
    "Original doc."
    __doc__ = 'Replaced doc.'
    __module__ = module_token
    x: int
assert Metadata.__module__ is module_token
assert Metadata.__doc__ == 'Replaced doc.'
assert Metadata.__qualname__ == 'Metadata'
print('N2.metadata', Metadata.__module__ is module_token, Metadata.__doc__, Metadata.__qualname__)

trace = []
Broken = 'old'
try:
    class Broken(NamedTuple):
        x: Missing = 1
        y: int
        trace.append('body')
except TypeError as error:
    assert str(error) == 'Non-default namedtuple field y cannot follow default field x'
    assert Broken == 'old' and trace == ['body']
    print('N2.default-error', str(error), Broken, trace)
else:
    raise AssertionError('bad defaults accepted')

try:
    class Forbidden(NamedTuple):
        x: int
        _asdict = 3
        trace.append('forbidden-body')
except AttributeError as error:
    assert str(error) == 'Cannot overwrite NamedTuple attribute _asdict'
    print('N2.forbidden-error', str(error), trace)
else:
    raise AssertionError('forbidden accepted')
try:
    class NameFirst(NamedTuple):
        __annotations__ = {'_x': int}
        _asdict = 3
except ValueError as error:
    assert str(error) == "Field names cannot start with an underscore: '_x'"
    print('N2.name-first', str(error))
else:
    raise AssertionError('bad field accepted')
print('N2.complete', True)
