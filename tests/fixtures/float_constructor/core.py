# float固定签名与冷转换；动态身份不代表跨静态f64调用的通用对象身份。
import math
log = []
shared = 2.5
class Both:
    def __float__(self):
        log.append('float')
        return shared
    def __index__(self):
        log.append('index-unused')
        return 9
class Index:
    def __index__(self):
        log.append('index')
        return 9007199254740993
class Bad:
    def __float__(self):
        log.append('bad-float')
        return 3
    def __index__(self):
        log.append('index-unused')
        return 9
class Disabled:
    __float__ = None
    def __index__(self):
        log.append('index-unused')
        return 9
value = Both()
saved = float
def literal_same():
    item = 2.5
    return float(item) is item
def same(item):
    return float(item) is item
def same_typed(item: float) -> bool:
    return float(item) is item
def invoke(function, item):
    return function(item)
assert literal_same()
assert float(shared) is shared
assert saved(shared) is shared
assert same(shared)
assert same_typed(shared)
assert invoke(saved, shared) is shared
assert float(value) is shared
assert log == ['float']
log.clear()
assert saved(value) is shared
assert float(*[value], **{}) is shared
assert log == ['float', 'float']
log.clear()
assert float(Index()) == 9007199254740992.0
assert log == ['index']
log.clear()
def mark(label, item):
    log.append(label)
    return item
def reject(label, operation, kind, expected):
    log.clear()
    try:
        operation()
    except (TypeError, ValueError, OverflowError) as error:
        assert type(error) is kind
        assert log == expected
        print(label, log)
    else:
        raise AssertionError(label)
reject('many', lambda: float(mark('a', value), mark('b', 2), mark('c', 3)), TypeError, ['a', 'b', 'c'])
reject('keyword', lambda: float(x=mark('x', value)), TypeError, ['x'])
reject('many-keyword', lambda: float(mark('a', value), x=mark('x', 2)), TypeError, ['a', 'x'])
reject('star-many', lambda: float(*mark('star', [value, 2])), TypeError, ['star'])
reject('star-invalid', lambda: float(*mark('star', 3)), TypeError, ['star'])
reject('double-invalid', lambda: float(mark('a', value), **mark('kw', 3)), TypeError, ['a', 'kw'])
reject('bad-return', lambda: float(Bad()), TypeError, ['bad-float'])
reject('disabled', lambda: float(Disabled()), TypeError, [])
reject('none', lambda: float(None), TypeError, [])
reject('huge-int', lambda: float(10 ** 400), OverflowError, [])
assert float() == 0.0
assert float(*[]) == 0.0
assert float(True) == 1.0
for text in ['12.5', '1_2.5', '１２.５', '١٢.٥', '\u200312.5\u2003', b'12.5', bytearray(b'1_2.5')]:
    assert float(text) == 12.5
assert float('1_2.3_4e+0_1') == 123.4
assert repr(float('-0')) == '-0.0'
assert math.isinf(float('1e400'))
assert math.isnan(float('-NaN'))
assert float('1e-400') == 0.0
for text in ['_1', '1__2', '1_e2', '0x1p0', '1\x00', '\x1c12', b'\xa012']:
    reject('bad-text', lambda: float(text), ValueError, [])
def shadow(float):
    return float(7, 8)
def replacement(left, right):
    return left + right
assert shadow(replacement) == 15
def numeric(item: int) -> float:
    return float(item)
def floating(item: float) -> float:
    return float(item)
def boolean(item: bool) -> float:
    return float(item)
assert numeric(7) == 7.0
assert floating(-2.5) == -2.5
assert boolean(True) == 1.0
print('float-ok', numeric(7), floating(-2.5), boolean(True))
