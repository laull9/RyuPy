# 字符串下标类型级__index__：精确/派生默认入口、单次调用及警告和异常顺序。
import warnings
class Index:
    def __init__(self, value):
        self.value = value
        self.calls = 0
    def __index__(self):
        self.calls += 1
        return self.value
class Text(str):
    pass
for text in ['a😀猫', Text('a😀猫'), '']:
    for value in [0, 1, -1, 9, 9223372036854775807, -9223372036854775808, 10**40, -(10**40), 'bad']:
        key = Index(value)
        try:
            print('value', len(text), ord(text[key]))
        except (IndexError, TypeError) as error:
            print('error', type(error).__name__, str(error))
        print('calls', key.calls)
for value in [True, False, 10**40, -(10**40)]:
    try:
        print('direct', ord('abc'[value]))
    except IndexError as error:
        print('direct', type(error).__name__, str(error))
key=Index(1)
key.__index__ = lambda: 0
print('type-slot', ord('abc'[key]), key.calls)
marker=ValueError('index body')
class Failed:
    def __index__(self):
        print('failed once')
        raise marker
try:
    'abc'[Failed()]
except ValueError as error:
    print('same error', error is marker)
class IntOnly:
    def __int__(self):
        print('must not call int')
        return 0
try:
    'abc'[IntOnly()]
except TypeError as error:
    print('int-only', str(error))
class Container:
    def __getitem__(self, key):
        print('user getitem', key is marker)
        return 7
print('custom', Container()[marker])
for action in ['ignore', 'always', 'error']:
    with warnings.catch_warnings(record=True) as records:
        warnings.simplefilter(action)
        key=Index(True)
        try:
            print('bool-result', action, 'abc'[key])
        except DeprecationWarning as error:
            print('warning-error', str(error))
        print('warning-count', key.calls, len(records))
        if len(records):
            print(records[0].category.__name__, str(records[0].message))
        print('direct-bool', 'abc'[True], len(records))
key=Index(-1)
print('root', str.__getitem__(Text('a😀'), key), key.calls)
class ParentIndex:
    def __index__(self):
        print('inherited once')
        return 1
class ChildIndex(ParentIndex):
    pass
print('inherited', 'abc'[ChildIndex()])
def property_result():
    print('property callable once')
    return -1
class PropertyIndex:
    @property
    def __index__(self):
        print('property getter once')
        return property_result
print('property', 'abc'[PropertyIndex()])
class NoCall:
    __index__ = None
try:
    'abc'[NoCall()]
except TypeError as error:
    print('noncall', str(error))
with warnings.catch_warnings(record=True) as records:
    warnings.simplefilter('error')
    key=Index(True)
    try:
        ''[key]
    except DeprecationWarning:
        print('warning before empty-index', key.calls)
