# 尚未执行；真实CP-first源码，不接受NI替代。
from typing import NamedTuple

pairs = [('x', int), ('y', str)]
Full = NamedTuple('Full', pairs)
Once = NamedTuple('Once', iter(pairs))
Empty = NamedTuple('Empty')
EmptyNone = NamedTuple('EmptyNone', None)
Other = NamedTuple('Full', pairs)
assert Full._fields == Once._fields == ('x', 'y')
assert Full.__annotations__ == {'x': int, 'y': str}
assert Once.__annotations__ == {}
assert Empty._fields == EmptyNone._fields == ()
assert Full is not Other and type(Full) is type
assert Full.__bases__ == (tuple,) and Full.__orig_bases__ == (NamedTuple,)
assert type(Full(1, 'a')) is Full and isinstance(Full(1, 'a'), tuple)
print('N1.fields', Full._fields, Once.__annotations__, Empty._fields)

Keywords = NamedTuple('Keywords', typename=int, fields=str)
assert Keywords._fields == ('typename', 'fields')
assert Keywords(2, 'x') == (2, 'x')
assert Full.__new__.__defaults__ == ()
assert Empty.__new__.__defaults__ == ()
assert Full.__annotations__ is Full.__new__.__annotations__
Full.__annotations__['extra'] = float
assert Full._fields == ('x', 'y') and Full(1, 'b') == (1, 'b')
print('N1.metadata', Keywords._fields, Full.__new__.__defaults__, tuple(Full.__annotations__))

Refs = NamedTuple('Refs', [('none', None), ('a', 'Missing'), ('b', 'Missing')])
a = Refs.__annotations__['a']
b = Refs.__annotations__['b']
assert Refs.__annotations__['none'] is type(None)
assert type(a) is type(b) and a is not b and a == b
assert hash(a) == hash(b)
assert a.__forward_arg__ == 'Missing'
assert a.__forward_module__ is None
assert a.__forward_is_argument__ is True and a.__forward_is_class__ is False
assert a.__forward_evaluated__ is False and a.__forward_value__ is None
assert repr(a) == "ForwardRef('Missing')"
assert Refs(1, 2, 3) == (1, 2, 3)
print('N1.refs', repr(a), a is not b, a == b, hash(a) == hash(b))

try:
    NamedTuple('Mixed', [], x=int)
except TypeError as error:
    assert str(error) == 'Either list of fields or keywords can be provided to NamedTuple, not both'
    print('N1.mixed-error', str(error))
else:
    raise AssertionError('mixed fields accepted')
try:
    NamedTuple('Pair', [('x', int), ('y', str, float)])
except ValueError as error:
    assert str(error) == 'too many values to unpack (expected 2)'
    print('N1.pair-error', str(error))
else:
    raise AssertionError('bad pair accepted')
try:
    NamedTuple('Duplicate', [('x', int), ('x', str)])
except ValueError as error:
    assert str(error) == "Encountered duplicate field name: 'x'"
    print('N1.duplicate-error', str(error))
else:
    raise AssertionError('duplicate accepted')
print('N1.complete', True)
