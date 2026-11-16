# 公开构造/转换保真实对象身份；类发布先完成私有壳再替换公开绑定。
events = []

class Token:
    pass

old = Token()
S = old

def decorate(cls):
    assert S is old
    assert cls.previous is old
    events.append('decorate')
    return cls

@decorate
class S(str):
    previous = S
    events.append('body')
    def captured(self, value=S):
        return value

assert events == ['body', 'decorate']
assert S is not old
assert S.previous is old
s = S('abc')
assert type(s) is S
assert isinstance(s, str)
assert s.captured() is old
assert type(S()) is S
assert S() == ''
assert S(object=123) == '123'
assert S(True) == 'True'
assert type(str.__new__(S, 'root')) is S
assert str.__new__(S, 'root') == 'root'
assert type(str(s)) is str
assert str(s) == 'abc'
assert type(repr(s)) is str
saved_str = str
saved_repr = repr
assert type(saved_str(s)) is str
assert saved_str(s) == 'abc'

class Child(S):
    pass
class Grandchild(Child):
    pass
class Last(Grandchild):
    pass
assert type(Last('child')) is Last
assert Last('child').captured() is old

calls = []
class ReturnsString:
    def __str__(self):
        calls.append('str')
        return s
    def __repr__(self):
        calls.append('repr')
        return s

value = ReturnsString()
assert str(value) is s
assert saved_str(value) is s
assert repr(value) is s
assert saved_repr(value) is s
assert calls == ['str', 'str', 'repr', 'repr']

def identity(value: str) -> str:
    return value
assert identity(s) is s
assert identity(value) is value

def fail_body():
    events.append('fail-body')
    raise ValueError('body')

Broken = old
try:
    class Broken(str):
        field = fail_body()
except ValueError:
    pass
else:
    raise AssertionError('body must fail')
assert Broken is old
assert events == ['body', 'decorate', 'fail-body']

def fail_decorator(cls):
    assert Rejected is old
    assert cls.field == 'ready'
    events.append('fail-decorator')
    raise ValueError('decorator')

Rejected = old
try:
    @fail_decorator
    class Rejected(str):
        field = 'ready'
except ValueError:
    pass
else:
    raise AssertionError('decorator must fail')
assert Rejected is old
assert events == ['body', 'decorate', 'fail-body', 'fail-decorator']

Alias = str
class Actual(Alias):
    pass
assert type(Actual('alias')) is Actual
assert Actual('alias') == 'alias'
# 根初始化描述符共用object槽；规则由本组三源的首次CP/JIT执行核实。
class RootInitPlain(str):
    pass
class RootInitCustom(str):
    def __init__(self, *args, **kwargs):
        pass
root_plain, root_custom = RootInitPlain('raw'), RootInitCustom('raw')
assert str.__init__ is object.__init__ and RootInitPlain.__init__ is object.__init__
assert str.__init__.__qualname__ == 'object.__init__'
assert str.__init__.__objclass__ is object
for obj in ('exact', root_plain):
    assert object.__init__(obj, 'ignored', x=1) is None
assert object.__init__(root_custom) is None
for call in (lambda: object.__init__(root_custom, 'extra'), lambda: object.__init__(root_custom, x=1)):
    try:
        call()
    except TypeError as error:
        print(type(error).__name__, str(error))
    else:
        raise AssertionError('expected root init argument error')
print('ROOT INIT IDENTITY AND ARGUMENT RULES PASSED')
print('str-construction-identity-publication')
