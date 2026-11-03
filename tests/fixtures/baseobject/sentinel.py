# 纯根对象的身份、缺省哨兵、别名及属性边界；不比较随机地址数值。
MISSING = object()
def supplied(value=MISSING):
    return value is MISSING
assert supplied() and not supplied(None)
ctor = object
a = ctor()
b = object()
assert a is not b and a == a and a != b
assert type(a) is object and a.__class__ is object
assert type(object) is type and callable(object)
assert object is ctor
assert object.__name__ == 'object' and object.__module__ == 'builtins'
assert object.__bases__ == () and object.__base__ is None
assert object.__mro__ == (object,)
text = repr(a)
assert text.startswith('<object object at 0x') and text.endswith('>')
assert str(a) == text and repr(a) == text
assert hash(a) == hash(a)
lookup = {a: 23}
assert lookup[a] == 23
assert object.__eq__(a, b) is NotImplemented
assert object.__ne__(a, b) is NotImplemented
reverse_log = []
class ReverseBoolean:
    def __eq__(self, other):
        reverse_log.append(other)
        return True
assert (a == ReverseBoolean()) is True
assert len(reverse_log) == 1 and reverse_log[0] is a
# 默认__ne__委托__eq__；不声称已有独立自定义__ne__任意对象返回协议。
assert (a != ReverseBoolean()) is False
assert len(reverse_log) == 2 and reverse_log[1] is a
try:
    a < b
except TypeError:
    pass
else:
    raise AssertionError('ordering accepted')
for action in [lambda: setattr(a, 'value', 1), lambda: getattr(a, '__dict__')]:
    try:
        action()
    except AttributeError:
        pass
    else:
        raise AssertionError('bare object acquired dict')
for action in [lambda: setattr(a, '__class__', object), lambda: setattr(object, 'value', 1)]:
    try:
        action()
    except TypeError:
        pass
    else:
        raise AssertionError('immutable object modified')
def local_alias():
    local = object
    return local()
def typed_alias(n: int) -> int:
    local = object
    value = local()
    return n
assert type(local_alias()) is object and typed_alias(42) == 42
def through(factory):
    return factory()
assert type(through(ctor)) is object
def shadow(object):
    return object(4)
assert shadow(lambda value: value + 1) == 5
log = []
def mark(value):
    log.append(value)
    return value
for call in [lambda: object(mark(1), mark(2)), lambda: ctor(extra=mark(3)), lambda: ctor(*[mark(4)], **{'extra': mark(5)})]:
    log.clear()
    try:
        call()
    except TypeError:
        print('bad-args', log)
    else:
        raise AssertionError('object accepted arguments')
try:
    raise ValueError('active')
except ValueError as active:
    try:
        ctor(1)
    except TypeError as error:
        assert error.__context__ is active
for invalid in [lambda: object.__new__(), lambda: object.__new__(1), lambda: object.__new__(int), lambda: object.__new__(object, 1), lambda: object.__init__(), lambda: object.__init__(a, 1)]:
    try:
        invalid()
    except TypeError:
        pass
    else:
        raise AssertionError('invalid object method accepted')
assert type(object.__new__(object)) is object
assert object.__init__(a) is None
assert object.__init__(1) is None
print('sentinel-ok')
