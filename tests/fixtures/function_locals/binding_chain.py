# 绑定拒绝前保全部求值；用户遮蔽仍调用用户函数，尾部保完整异常链。

EVENTS = []

def mark(value):
    EVENTS.append(value)
    return value

def binding():
    assert locals() == {}
    assert locals(*[], **{}) == {}
    try:
        locals(mark('one'), mark('two'))
    except TypeError as error:
        assert str(error) == 'locals() takes no arguments (2 given)'
        print('bind.positional', str(error), EVENTS)
    try:
        locals(value=mark('keyword'))
    except TypeError as error:
        assert str(error) == 'locals() takes no keyword arguments'
        print('bind.keyword', str(error), EVENTS)
    try:
        locals(*[mark('star')], **{'value': mark('mapping')})
    except TypeError as error:
        assert str(error) == 'locals() takes no keyword arguments'
        print('bind.expansion', str(error), EVENTS)
    try:
        locals(*mark(1))
    except TypeError as error:
        assert str(error) == 'locals() argument after * must be an iterable, not int'
        print('bind.invalid-star', str(error), EVENTS)

binding()

def user_call(value):
    return value + 10

def shadow_parameter(locals):
    return locals(7)

def shadow_local():
    locals = user_call
    return locals(8)

assert shadow_parameter(user_call) == 17
assert shadow_local() == 18
print('bind.shadowed', shadow_parameter(user_call), shadow_local())

def inner():
    return locals(mark('chain'))
def middle():
    return inner()
def outer():
    return middle()

try:
    raise ValueError('locals-cause')
except ValueError as cause:
    saved_cause = cause
try:
    raise RuntimeError('locals-ambient') from saved_cause
except RuntimeError:
    print('bind.chain', EVENTS)
    outer()
