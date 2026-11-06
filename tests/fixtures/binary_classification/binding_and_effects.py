# ryupy: pending
"""ASCII分类全族绑定与真实实参副作用；root-direct错误是首次应用CP守门的扩展组合。"""
METHODS = ('isalpha', 'isalnum', 'isdigit', 'isspace', 'isascii', 'islower', 'isupper', 'istitle')


def direct_args(value, name, args, kwargs):
    if name == 'isalpha': return value.isalpha(*args, **kwargs)
    if name == 'isalnum': return value.isalnum(*args, **kwargs)
    if name == 'isdigit': return value.isdigit(*args, **kwargs)
    if name == 'isspace': return value.isspace(*args, **kwargs)
    if name == 'isascii': return value.isascii(*args, **kwargs)
    if name == 'islower': return value.islower(*args, **kwargs)
    if name == 'isupper': return value.isupper(*args, **kwargs)
    if name == 'istitle': return value.istitle(*args, **kwargs)
    raise AssertionError(name)


def root_direct_args(kind, name, args, kwargs):
    if name == 'isalpha': return kind.isalpha(*args, **kwargs)
    if name == 'isalnum': return kind.isalnum(*args, **kwargs)
    if name == 'isdigit': return kind.isdigit(*args, **kwargs)
    if name == 'isspace': return kind.isspace(*args, **kwargs)
    if name == 'isascii': return kind.isascii(*args, **kwargs)
    if name == 'islower': return kind.islower(*args, **kwargs)
    if name == 'isupper': return kind.isupper(*args, **kwargs)
    if name == 'istitle': return kind.istitle(*args, **kwargs)
    raise AssertionError(name)


def expect_error(action, message):
    try:
        action()
    except TypeError as error:
        assert type(error) is TypeError and str(error) == message
        return str(error)
    raise AssertionError('expected TypeError: ' + message)


# 这些错误模板逐项核过唯一原流C；root缺self/错self优先于kwargs与位置参数数目。
for kind in (bytes, bytearray):
    value = kind(b'Ab')
    for name in METHODS:
        bound = getattr(value, name)
        root = getattr(kind, name)
        qualified = kind.__name__ + '.' + name + '()'
        errors = []
        for args, kwargs, message in [((None,), {}, qualified + ' takes no arguments (1 given)'),
                                      ((None, None), {}, qualified + ' takes no arguments (2 given)'),
                                      ((), {'x': 1}, qualified + ' takes no keyword arguments'),
                                      ((None,), {'x': 1}, qualified + ' takes no keyword arguments')]:
            errors.append(expect_error(lambda: direct_args(value, name, args, kwargs), message))
            errors.append(expect_error(lambda: bound(*args, **kwargs), message))
            errors.append(expect_error(lambda: root_direct_args(kind, name, (value,) + args, kwargs), message))
            errors.append(expect_error(lambda: root(value, *args, **kwargs), message))
        missing = 'unbound method ' + qualified + ' needs an argument'
        errors.append(expect_error(lambda: root(), missing))
        errors.append(expect_error(lambda: root(self=value), missing))
        errors.append(expect_error(lambda: root_direct_args(kind, name, (), {}), missing))
        errors.append(expect_error(lambda: root_direct_args(kind, name, (), {'self': value}), missing))
        other = bytearray(b'Ab') if kind is bytes else b'Ab'
        view = memoryview(b'Ab')
        for wrong, typename in [(None, 'NoneType'), ('Ab', 'str'), (65, 'int'),
                                (other, 'bytearray' if kind is bytes else 'bytes'), (view, 'memoryview')]:
            message = "descriptor '" + name + "' for '" + kind.__name__ + "' objects doesn't apply to a '" + typename + "' object"
            errors.append(expect_error(lambda: root(wrong), message))
            errors.append(expect_error(lambda: root_direct_args(kind, name, (wrong,), {}), message))
        wrong_message = "descriptor '" + name + "' for '" + kind.__name__ + "' objects doesn't apply to a 'NoneType' object"
        errors.append(expect_error(lambda: root(None, 1, x=2), wrong_message))
        errors.append(expect_error(lambda: root_direct_args(kind, name, (None, 1), {'x': 2}), wrong_message))
        view.release()
        assert value == kind(b'Ab') and len(errors) == 32
        print('B.matrix-' + kind.__name__ + '-' + name, errors)

# 空*args/**kwargs的函数先改同长内容再resize；分类实际调用看见最终值且不持导出。
for route in ('direct', 'saved', 'root-direct', 'root-saved'):
    value = bytearray(b'A')
    saved = value.isalpha
    root = bytearray.isalpha
    events = []

    def empty_args():
        events.append('star')
        value[0] = 49
        return ()

    def empty_kwargs():
        events.append('kwargs')
        value.append(50)
        return {}

    if route == 'direct':
        result = value.isalpha(*empty_args(), **empty_kwargs())
    elif route == 'saved':
        result = saved(*empty_args(), **empty_kwargs())
    elif route == 'root-direct':
        result = bytearray.isalpha(value, *empty_args(), **empty_kwargs())
    else:
        result = root(value, *empty_args(), **empty_kwargs())
    value.append(51)
    assert result is False and events == ['star', 'kwargs'] and value == bytearray(b'123')
    print('B.live-' + route, result, events, value)

# 即使必定绑定失败，也先完成全部实参副作用；kwargs优先级不能吞位置参数正文。
for route in ('direct', 'saved', 'root-direct', 'root-saved'):
    value = bytearray(b'A')
    saved = value.isalpha
    root = bytearray.isalpha
    events = []

    def positional():
        events.append('position')
        value[0] = 49
        return 7

    def keyword():
        events.append('keyword')
        value.append(50)
        return 8

    def call():
        if route == 'direct': return value.isalpha(positional(), x=keyword())
        if route == 'saved': return saved(positional(), x=keyword())
        if route == 'root-direct': return bytearray.isalpha(value, positional(), x=keyword())
        return root(value, positional(), x=keyword())

    message = expect_error(call, 'bytearray.isalpha() takes no keyword arguments')
    assert events == ['position', 'keyword'] and value == bytearray(b'12')
    value.append(51)
    print('B.reject-' + route, message, events, value)

# 普通可转bytes对象不是合法root self；不得调用其转换或真值协议。
events = []


class Trap:
    def __bytes__(self):
        events.append('bytes')
        return b'A'

    def __iter__(self):
        events.append('iter')
        return iter((65,))

    def __bool__(self):
        events.append('bool')
        return True


trap = Trap()
for kind in (bytes, bytearray):
    message = "descriptor 'isalpha' for '" + kind.__name__ + "' objects doesn't apply to a 'Trap' object"
    assert expect_error(lambda: kind.isalpha(trap), message) == message
assert events == []
print('B.no-coercion', events)

# 每路线新建异常，仅抛一次；用户参数正文异常不能被binder改成TypeError。
for route in ('direct', 'saved', 'root-direct', 'root-saved'):
    marker = ValueError('argument-body-' + route)
    value = bytearray(b'A')
    saved = value.isalpha
    root = bytearray.isalpha
    events = []

    def broken():
        events.append('broken')
        value[0] = 49
        raise marker

    try:
        if route == 'direct': value.isalpha(x=broken())
        elif route == 'saved': saved(x=broken())
        elif route == 'root-direct': bytearray.isalpha(value, x=broken())
        else: root(value, x=broken())
    except ValueError as caught:
        assert caught is marker and events == ['broken'] and value == bytearray(b'1')
    else:
        raise AssertionError('argument exception lost')
    print('B.argument-identity-' + route, events, value)

owner = bytes
root = owner.isalpha
owner = bytearray
assert root(b'Ab') is True
message = "descriptor 'isalpha' for 'bytes' objects doesn't apply to a 'bytearray' object"
assert expect_error(lambda: root(owner(b'Ab')), message) == message
print('B.saved-root', root(b'Ab'), message)


class Ordinary:
    def isalpha(self):
        return 'ordinary-body'


def same_site(value):
    return value.isalpha()


values = (b'Ab', bytearray(b'12'), Ordinary(), b'')
assert [same_site(value) for value in values] == [True, False, 'ordinary-body', False]
print('B.ordinary-site', [same_site(value) for value in values])
print('B.complete', True)
