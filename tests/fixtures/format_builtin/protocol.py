# ryupy: pending
# 类型槽、字符串子类身份和默认对象协议；不覆盖现有守卫禁止的槽。
events = []
class Text(str):
    def __str__(self):
        events.append('text-str')
        return 'OVERRIDE'
spec = Text('arbitrary !?')
result = Text('RESULT')
class Custom:
    def __format__(self, actual):
        events.append(('custom', actual is spec))
        return result
    def __str__(self):
        raise AssertionError('unexpected str')
obj = Custom()
obj.__format__ = lambda s: 'INSTANCE'
assert format(obj, spec) is result
assert format(obj) is result
assert events == [('custom', True), ('custom', False)]
assert format(result) == 'OVERRIDE'
assert type(format(result, 's')) is Text
assert format(result, 's') is result
assert events == [('custom', True), ('custom', False), 'text-str']

class Default:
    def __str__(self):
        events.append('default-str')
        return result
assert format(Default()) is result
before = len(events)
try:
    format(Default(), '>10')
except TypeError as error:
    print(type(error).__name__, str(error))
else:
    raise AssertionError('nonempty object spec accepted')
assert len(events) == before

class Parent:
    def __format__(self, actual):
        return 'OLD'
class Child(Parent):
    pass
child = Child()
assert format(child) == 'OLD'
Parent.__format__ = lambda self, actual: 'NEW'
assert format(child) == 'NEW'

class CallSlot:
    def __call__(self, actual):
        assert actual is spec
        return result
class CallableSlot:
    __format__ = CallSlot()
class PropertySlot:
    @property
    def __format__(self):
        events.append('getter')
        return lambda actual: result if actual is spec else 'BAD'
class StaticSlot:
    @staticmethod
    def __format__(actual):
        assert actual is spec
        return result
class ClassSlot:
    @classmethod
    def __format__(cls, actual):
        assert cls is Derived
        assert actual is spec
        return result
class Derived(ClassSlot):
    pass
for value in (CallableSlot(), PropertySlot(), StaticSlot(), Derived()):
    assert format(value, spec) is result
assert events[-1] == 'getter'

class Missing:
    def __getattr__(self, name):
        raise AssertionError('unexpected getattr')
    def __str__(self):
        return 'DEFAULT'
assert format(Missing()) == 'DEFAULT'
class NullSlot:
    __format__ = None
class Returner:
    def __format__(self, actual):
        return self.answer
for answer in (None, 7, NotImplemented):
    value = Returner()
    value.answer = answer
    try:
        format(value)
    except TypeError as error:
        print(type(error).__name__, str(error))
    else:
        raise AssertionError('bad result accepted')
try:
    format(NullSlot())
except TypeError as error:
    print(type(error).__name__, str(error))
else:
    raise AssertionError('null slot accepted')
for value in (None, [], {}, (1,), ValueError('x')):
    print(format(value))
print('format-protocol-ok')
