# ryupy: pending
# 全局KeyError重绑独立成图，不能混进direct源让预扫描绕开旧内建路径。
events = []


def first(value):
    events.append('first')
    return ['first', value]


def second(value):
    events.append('second')
    return ['second', value]


def local(KeyError):
    return KeyError('local')


def outer(KeyError):
    def inner():
        return KeyError('closure')
    return inner


assert local(first) == ['first', 'local']
assert outer(second)() == ['second', 'closure']
KeyError = first
saved = KeyError


def argument():
    global KeyError
    events.append('argument')
    KeyError = second
    return 'value'


def call_current():
    return KeyError(argument())


events.clear()
assert call_current() == ['first', 'value']
assert events == ['argument', 'first']
assert KeyError('next') == ['second', 'next']
assert saved('saved') == ['first', 'saved']
print('local closure global binding and callee capture')

KeyError = 7
events.clear()
try:
    KeyError(argument())
except TypeError as caught:
    assert str(caught) == "'int' object is not callable"
    assert events == ['argument']
else:
    raise AssertionError('noncallable binding was replaced')
del KeyError
assert str(KeyError('after deletion')) == "'after deletion'"
print('noncallable argument order and existing deleted-name fallback')
