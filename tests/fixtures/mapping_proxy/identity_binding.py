# 类型与绑定：首次CP通过，原函数内类被HIR拒绝；模块级try尾修待CP/JIT复验。
# 原首次source逐字保存在pending/identity_nested_class_original.py，不扩大nested class语法。
import types
from types import MappingProxyType as P
from enum import Enum
from collections.abc import Mapping, MutableMapping


def reject(label, action, expected, message):
    try:
        action()
    except expected as error:
        assert str(error) == message
        print(label, type(error).__name__, str(error))
    else:
        raise AssertionError(label)


p = P({'a': 1})
assert type(p) is P and callable(P)
assert isinstance(p, P) and isinstance(p, Mapping)
assert not isinstance(p, MutableMapping) and not isinstance(p, dict)
assert issubclass(P, object) and not issubclass(P, dict)
print('P1.type', P.__name__, P.__module__, repr(P))
class E(Enum):
    A = 1
assert type(E.__members__) is P and isinstance(E.__members__, P)
print('P1.enum', E.__members__['A'] is E.A)
try:
    class Sub(P):
        pass
except TypeError as error:
    assert str(error) == "type 'mappingproxy' is not an acceptable base type"
    print('P1.subclass', type(error).__name__, str(error))
else:
    raise AssertionError('P1.subclass')
reject('P1.noarg', lambda: P(), TypeError,
       "mappingproxy() missing required argument 'mapping' (pos 1)")
reject('P1.twoargs', lambda: P({}, {}), TypeError,
       'mappingproxy() takes at most 1 argument (2 given)')
reject('P1.duplicate', lambda: P({}, mapping={}), TypeError,
       'mappingproxy() takes at most 1 argument (2 given)')
reject('P1.badkw', lambda: P(wrong=1), TypeError,
       "mappingproxy() missing required argument 'mapping' (pos 1)")
reject('P1.extra', lambda: P({}, {}, wrong=1), TypeError,
       'mappingproxy() takes at most 1 argument (3 given)')
for label, value, name in [('list', [], 'list'), ('tuple', (), 'tuple'),
                           ('object', object(), 'object')]:
    reject('P1.' + label, lambda: P(value), TypeError,
           'mappingproxy() argument must be a mapping, not ' + name)
assert P(mapping={'a': 2})['a'] == 2
assert P(*[{'a': 3}])['a'] == 3
assert P(**{'mapping': {'a': 4}})['a'] == 4
print('P1.expanded', True)
assert P('ab')[1] == 'b'
print('P1.string', repr(P('ab')))
class NullSlot:
    __getitem__ = None
assert type(P(NullSlot())) is P
reject('P1.none-slot', lambda: P(NullSlot())['k'], TypeError,
       "'NoneType' object is not callable")
class Fake:
    pass
fake = Fake()
fake.__getitem__ = lambda key: 9
reject('P1.instance-slot', lambda: P(fake), TypeError,
       'mappingproxy() argument must be a mapping, not Fake')

def transfer(factory):
    return factory
saved = types.MappingProxyType
holder = [saved]
assert type(transfer(holder[0])({})) is P
assert type(type(p)({})) is P

def replacement(mapping):
    return ('replacement', mapping)
types.MappingProxyType = replacement
assert types.MappingProxyType({'x': 2}) == ('replacement', {'x': 2})
assert type(saved({})) is P and type(P({})) is P
assert transfer(replacement)({}) == ('replacement', {})
types.MappingProxyType = saved
print('P1.dynamic', True)
print('P1.complete', True)
