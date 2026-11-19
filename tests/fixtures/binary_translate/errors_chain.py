
# 空输入也验证全部参数；严格冻结错误优先级及完整用户cause/context链。

def rejects(label, call, kind, message):
    try:
        call()
    except kind as error:
        assert str(error) == message
        print('translate.error', label, kind.__name__, str(error))
    else:
        raise AssertionError(label)

for source in (b'', bytearray()):
    rejects('missing', lambda: source.translate(), TypeError, 'translate() takes at least 1 positional argument (0 given)')
    rejects('extra', lambda: source.translate(None, b'', b''), TypeError, 'translate() takes at most 2 arguments (3 given)')
    rejects('table-keyword-only', lambda: source.translate(table=None), TypeError, 'translate() takes at least 1 positional argument (0 given)')
    rejects('table-keyword', lambda: source.translate(None, table=None), TypeError, "'table' is an invalid keyword argument for translate()")
    rejects('unknown-keyword', lambda: source.translate(None, unknown=0), TypeError, "'unknown' is an invalid keyword argument for translate()")
    rejects('duplicate-delete', lambda: source.translate(None, b'', delete=b''), TypeError, 'translate() takes at most 2 arguments (3 given)')
    rejects('table-type', lambda: source.translate(1), TypeError, "a bytes-like object is required, not 'int'")
    rejects('table-length', lambda: source.translate(b''), ValueError, 'translation table must be 256 characters long')
    rejects('table-before-delete', lambda: source.translate(b'', None), ValueError, 'translation table must be 256 characters long')
    rejects('table-type-before-delete', lambda: source.translate(1, None), TypeError, "a bytes-like object is required, not 'int'")
    rejects('delete-none', lambda: source.translate(None, None), TypeError, "a bytes-like object is required, not 'NoneType'")
    rejects('delete-text', lambda: source.translate(None, delete=''), TypeError, "a bytes-like object is required, not 'str'")

rejects('bytes-root-missing', lambda: bytes.translate(), TypeError, 'unbound method bytes.translate() needs an argument')
rejects('bytearray-root-missing', lambda: bytearray.translate(), TypeError, 'unbound method bytearray.translate() needs an argument')
rejects('bytes-root-receiver', lambda: bytes.translate(1, b''), TypeError, "descriptor 'translate' for 'bytes' objects doesn't apply to a 'int' object")
rejects('bytearray-root-receiver', lambda: bytearray.translate(b'', None), TypeError, "descriptor 'translate' for 'bytearray' objects doesn't apply to a 'bytes' object")

EVENTS = []
def mark(value):
    EVENTS.append(value)
    return value
try:
    b''.translate(mark(1), delete=mark('bad'))
except TypeError as error:
    assert str(error) == "a bytes-like object is required, not 'int'"
assert EVENTS == [1, 'bad']
print('translate.error-evaluation', EVENTS)

def inner():
    return bytearray().translate(None, delete=None)
def middle():
    return inner()
def outer():
    return middle()

try:
    raise ValueError('translate-cause')
except ValueError as cause:
    saved_cause = cause
try:
    raise RuntimeError('translate-ambient') from saved_cause
except RuntimeError:
    print('translate.chain')
    outer()
