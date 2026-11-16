# ryupy: pending
# 通用生成类身份、实例绑定、默认引用和只读字段。尚未运行CP或RyuPy。
from collections import namedtuple


def keep(value):
    return value


def expect_error(label, action, expected_name):
    try:
        action()
    except Exception as error:
        assert type(error).__name__ == expected_name
        print(label, type(error).__name__, str(error))
    else:
        raise AssertionError(label + ' did not raise')


shared = []
Vector = namedtuple('Vector', 'x y z', defaults=[shared, None],
                    module='fixture.identity')
Other = namedtuple('Vector', 'x y z', defaults=[shared, None],
                   module='fixture.identity')
alias = Vector
classes = [Vector]
first = Vector(7)
second = Vector(7)
other = Other(7)
assert Vector is not Other
assert alias is Vector and classes[0] is Vector and keep(Vector) is Vector
assert type(Vector) is type and type(Other) is type
assert type(first) is Vector and type(other) is Other
assert first is not second
assert isinstance(first, Vector) and isinstance(first, tuple)
assert isinstance(first, object) and not isinstance(first, Other)
assert issubclass(Vector, tuple) and issubclass(Vector, object)
assert issubclass(Vector, Vector) and not issubclass(Vector, Other)
assert callable(Vector) and not callable(first)
assert keep(first) is first and [first][0] is first
print('B1.identity', Vector is not Other, type(first) is Vector,
      isinstance(first, tuple), first is not second)

# 类名称元数据不应泄漏成实例上的同名属性。
assert Vector.__name__ == 'Vector' and Vector.__qualname__ == 'Vector'
assert Vector.__module__ == 'fixture.identity'
assert first.__module__ == 'fixture.identity'
assert Vector.__base__ is tuple and Vector.__bases__ == (tuple,)
assert Vector.__mro__ == (Vector, tuple, object)
saved_bases = Vector.__bases__
saved_mro = Vector.__mro__
assert Vector.__bases__ is saved_bases and Vector.__bases__ is saved_bases
assert Vector.__mro__ is saved_mro and Vector.__mro__ is saved_mro
assert Vector.__slots__ == ()
assert Vector.__doc__ == 'Vector(x, y, z)'
assert Vector._fields == ('x', 'y', 'z')
assert Vector.__match_args__ is Vector._fields
assert first._fields is Vector._fields
assert first.__match_args__ is Vector.__match_args__
assert first._field_defaults is Vector._field_defaults
assert list(Vector._field_defaults) == ['y', 'z']
assert Vector._field_defaults['y'] is shared
assert Vector._field_defaults['z'] is None
print('B1.metadata', Vector.__name__, Vector.__qualname__, Vector._fields,
      Vector.__bases__ == (tuple,), Vector.__mro__ == (Vector, tuple, object))
expect_error('B1.instance-name', lambda: first.__name__, 'AttributeError')
expect_error('B1.instance-qualname', lambda: first.__qualname__, 'AttributeError')

# 绑定不得转换字段值，也不得把显式None误当缺参。
positional = Vector(1, 2, 3)
keywords = Vector(z=3, x=1, y=2)
mixed = Vector(1, z=3, y=2)
expanded = Vector(*(1,), **{'z': 3, 'y': 2})
assert positional == keywords == mixed == expanded == (1, 2, 3)
assert all(type(value) is Vector for value in
           [positional, keywords, mixed, expanded])
assert Vector(x=1).y is shared and Vector(1).z is None
explicit_none = Vector(1, None, None)
assert explicit_none.y is None and explicit_none.z is None
assert first.y is shared and second.y is shared
print('B1.binding', positional, keywords, mixed, expanded, explicit_none)

expect_error('B1.missing-one', lambda: Vector(), 'TypeError')
Required = namedtuple('Required', ['a', 'b', 'c'], module='fixture.identity')
expect_error('B1.missing-many', lambda: Required(), 'TypeError')
expect_error('B1.too-many-defaults', lambda: Vector(1, 2, 3, 4), 'TypeError')
expect_error('B1.too-many-required', lambda: Required(1, 2, 3, 4), 'TypeError')
expect_error('B1.duplicate', lambda: Vector(1, x=2), 'TypeError')
expect_error('B1.unknown', lambda: Vector(1, extra=2), 'TypeError')
expect_error('B1.hidden-cls', lambda: Vector(1, _cls=Vector), 'TypeError')

# 公开说明字典不参与后续构造绑定；字段对象本身仍允许修改。
replacement = ['dictionary-only']
Vector._field_defaults['y'] = replacement
Vector._field_defaults['z'] = 'not-the-default'
after = Vector(8)
assert after.y is shared and after.z is None
assert Vector._field_defaults['y'] is replacement
shared.append('shared')
assert first.y == ['shared'] and second.y is shared and after.y is shared
assert first.x == 7 and after.x == 8
print('B1.defaults', first.y is shared, after.y is shared,
      after.z is None, Vector._field_defaults['y'] is replacement)

# 任意字段值保持原函数/记录/list引用；不打印函数地址作为oracle。
Envelope = namedtuple('Envelope', ['payload', 'handler', 'record'],
                      module='fixture.identity')
envelope = Envelope(shared, keep, first)
assert envelope.payload is shared and envelope.handler is keep
assert envelope.record is first and envelope.handler(first) is first
print('B1.references', envelope.payload is shared, envelope.handler is keep,
      envelope.record is first, envelope.handler(first) is first)

Vacant = namedtuple('Vacant', '', module='fixture.identity')
Single = namedtuple('Single', ('value',), module='fixture.identity')
empty = Vacant()
single = Single('item')
assert empty is not Vacant() and type(empty) is Vacant
assert type(single) is Single and single.value == 'item'
assert repr(empty) == 'Vacant()' and str(empty) == 'Vacant()'
assert repr(single) == "Single(value='item')" and str(single) == repr(single)
assert repr(positional) == 'Vector(x=1, y=2, z=3)'
assert str(positional) == repr(positional)
print('B1.repr', repr(empty), repr(single), repr(positional))

# _tuplegetter按自身固定index读取任何tuple；owner参数并不限定接收者类。
descriptor = Vector.y
assert Vector.y is descriptor
assert descriptor.__get__(None, Other) is descriptor
assert descriptor.__get__((0, shared), 37) is shared
assert descriptor.__get__(other, object) is shared
assert descriptor.__get__(Envelope(0, keep, first), None) is keep
print('B1.descriptor', Vector.y is descriptor,
      descriptor.__get__(None, Other) is descriptor,
      descriptor.__get__((0, shared), 37) is shared)
expect_error('B1.descriptor-short', lambda: descriptor.__get__((1,), Vector),
             'IndexError')
expect_error('B1.descriptor-nontuple', lambda: descriptor.__get__([1, 2], Vector),
             'TypeError')


def set_field():
    first.x = 99


def delete_field():
    del first.x


def set_extra():
    first.extra = 99


def delete_extra():
    del first.extra


def set_item():
    first[0] = 99


expect_error('B1.set-field', set_field, 'AttributeError')
expect_error('B1.delete-field', delete_field, 'AttributeError')
expect_error('B1.set-extra', set_extra, 'AttributeError')
expect_error('B1.delete-extra', delete_extra, 'AttributeError')
expect_error('B1.set-item', set_item, 'TypeError')
expect_error('B1.no-dict', lambda: first.__dict__, 'AttributeError')
assert first.x == 7 and first.y is shared and first.z is None
assert not hasattr(first, 'extra') and not hasattr(first, '__dict__')
assert type(first) is Vector and Vector is alias
print('B1.readonly-state', first.x, first.y is shared, first.z is None)
