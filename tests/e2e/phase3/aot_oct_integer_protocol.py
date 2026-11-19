# oct的通用整数协议：无十进制位数限制、类型槽与警告、绑定/错误、保存对象及名字遮蔽。
import warnings

for value in [0, 1, 7, 8, -1, -8, True, False, 9223372036854775807, -9223372036854775808]:
    print('integer', repr(value), oct(value))
for bits in [63, 64, 127, 4096, 15000]:
    for sign in [1, -1]:
        value = sign * ((1 << bits) + 7)
        text = oct(value)
        print('wide', bits, sign, len(text), text[:18], text[-18:], int(text, 8) == value)

class Index:
    def __init__(self, result):
        self.result = result
        self.calls = 0
    def __index__(self):
        self.calls += 1
        return self.result
    def __int__(self):
        raise RuntimeError('不得调用__int__')
    def __format__(self, spec):
        raise RuntimeError('不得调用原对象__format__')

item = Index(-0o753)
item.__index__ = lambda: 0
print('type-slot', oct(item), item.calls)
class Inherited(Index):
    pass
item = Inherited(1 << 130)
print('inherited', oct(item), item.calls)
for value in ['bad', 1.5, None, [], object(), Index('bad')]:
    try:
        print(oct(value))
    except TypeError as error:
        print('invalid', str(error))

class IntOnly:
    def __int__(self):
        raise RuntimeError('不得调用__int__')
try:
    oct(IntOnly())
except TypeError as error:
    print('int-only', str(error))

marker = ValueError('index body')
class Failed:
    def __index__(self):
        raise marker
try:
    oct(Failed())
except ValueError as error:
    print('same-error', error is marker)

class MissingCall:
    __index__ = None
try:
    oct(MissingCall())
except TypeError as error:
    print('not-callable', str(error))

for action in ['ignore', 'always', 'error']:
    with warnings.catch_warnings(record=True) as records:
        warnings.simplefilter(action)
        item = Index(True)
        try:
            print('strict-subclass', action, oct(item))
        except DeprecationWarning as error:
            print('strict-subclass-error', str(error))
        print('warnings', item.calls, len(records))
        if len(records):
            print(records[0].category.__name__, str(records[0].message))
        print('direct-bool', oct(True), len(records))

for args, kwargs in [((), {}), ((1, 2), {}), ((), {'number': 1}), ((1,), {'number': 2})]:
    try:
        oct(*args, **kwargs)
    except TypeError as error:
        print('binding', str(error))
saved = oct
print('identity', saved is oct, saved.__name__, saved(0o755))
