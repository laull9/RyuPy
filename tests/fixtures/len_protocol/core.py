# len普通类冷协议；固定容器与数值行为作回归，不扩弃用warning或通用调用栈。
log = []
class Length:
    def __init__(self, value):
        self.value = value
    def __len__(self):
        log.append('len')
        return self.value
class Index:
    def __init__(self, value):
        self.value = value
    def __index__(self):
        log.append('index')
        return self.value
class IntOnly:
    def __int__(self):
        log.append('unused-int')
        return 2
class Missing:
    pass
class Disabled:
    __len__ = None
class DisabledIndex:
    __index__ = None
class Both:
    def __bool__(self):
        log.append('unused-bool')
        return False
    def __len__(self):
        log.append('both-len')
        return 3

def invoke(function, value):
    return function(value)
saved = len
item = Length(Index(2))
assert len(item) == 2
assert saved(item) == 2
assert invoke(saved, item) == 2
assert len(*[item], **{}) == 2
assert log == ['len', 'index', 'len', 'index', 'len', 'index', 'len', 'index']
log.clear()
assert item.__len__() is item.value
assert log == ['len']
for value, expected in [(0, 0), (1, 1), (False, 0), (True, 1), (2147483648, 2147483648), (9223372036854775807, 9223372036854775807)]:
    log.clear()
    result = len(Length(value))
    assert type(result) is int
    assert result == expected
    assert log == ['len']
log.clear()
assert len(Both()) == 3
assert log == ['both-len']

# 特殊方法只从类型找：实例同名属性无效，类槽重绑和继承立即生效。
class Parent:
    def __len__(self):
        log.append('parent')
        return 4
class Child(Parent):
    pass
child = Child()
child.__len__ = lambda: 99
log.clear()
assert len(child) == 4
assert log == ['parent']
Parent.__len__ = lambda self: 5
assert len(child) == 5
indexed = Index(6)
indexed.__index__ = lambda: 99
log.clear()
assert len(Length(indexed)) == 6
assert log == ['len', 'index']
Index.__index__ = lambda self: 7
log.clear()
assert len(Length(indexed)) == 7
assert log == ['len']
# 重新定义独立类型，后续非法返回仍观察一次index回调。
class ReturnedIndex:
    def __init__(self, value):
        self.value = value
    def __index__(self):
        log.append('index')
        return self.value
for expected in [0, 1, 2147483648, 9223372036854775807]:
    log.clear()
    result = len(Length(ReturnedIndex(expected)))
    assert type(result) is int
    assert result == expected
    assert log == ['len', 'index']

def mark(label, value):
    log.append(label)
    return value
def reject(label, operation, kind, expected):
    log.clear()
    try:
        operation()
    except (TypeError, ValueError, OverflowError) as error:
        assert type(error) is kind
        assert log == expected
        print(label, str(error), log)
    else:
        raise AssertionError(label)
reject('zero', lambda: len(), TypeError, [])
reject('many', lambda: len(mark('a', item), mark('b', 2)), TypeError, ['a', 'b'])
reject('keyword', lambda: len(obj=mark('kw', item)), TypeError, ['kw'])
reject('many-keyword', lambda: len(mark('a', item), mark('b', 2), obj=mark('kw', item)), TypeError, ['a', 'b', 'kw'])
reject('star-many', lambda: len(*mark('star', [item, 2])), TypeError, ['star'])
reject('missing', lambda: len(Missing()), TypeError, [])
reject('disabled', lambda: len(Disabled()), TypeError, [])
for label, value, kind in [('negative', -1, ValueError), ('negative-huge', -(10 ** 100), ValueError), ('overflow', 9223372036854775808, OverflowError), ('float', 1.5, TypeError), ('text', '2', TypeError), ('none', None, TypeError)]:
    reject(label, lambda: len(Length(value)), kind, ['len'])
reject('int-only', lambda: len(Length(IntOnly())), TypeError, ['len'])
reject('disabled-index', lambda: len(Length(DisabledIndex())), TypeError, ['len'])
for label, value, kind in [('index-negative', -1, ValueError), ('index-negative-huge', -(10 ** 100), ValueError), ('index-overflow', 9223372036854775808, OverflowError), ('index-float', 1.5, TypeError), ('index-nested', ReturnedIndex(2), TypeError)]:
    reject(label, lambda: len(Length(ReturnedIndex(value))), kind, ['len', 'index'])

# 原异常实例与ambient上下文保留；模块级handler避开已登记nested-caught整栈旧债。
length_failure = RuntimeError('length failed')
index_failure = RuntimeError('index failed')
class RaisedLength:
    def __len__(self):
        log.append('raise-len')
        raise length_failure
class RaisedIndex:
    def __index__(self):
        log.append('raise-index')
        raise index_failure
try:
    raise ValueError('ambient')
except ValueError as ambient:
    log.clear()
    try:
        len(Length(-1))
    except ValueError as invalid:
        assert invalid.__context__ is ambient
        assert log == ['len']
    else:
        raise AssertionError('validator context')
    log.clear()
    try:
        len(RaisedLength())
    except RuntimeError as caught:
        assert caught is length_failure
        assert caught.__context__ is ambient
        assert log == ['raise-len']
    else:
        raise AssertionError('length identity')
    log.clear()
    try:
        len(Length(RaisedIndex()))
    except RuntimeError as caught:
        assert caught is index_failure
        assert caught.__context__ is ambient
        assert log == ['len', 'raise-index']
    else:
        raise AssertionError('index identity')
    try:
        raise
    except ValueError as restored:
        assert restored is ambient

# 未改精确容器分派：字符串按码点，普通容器保原长度。
for value, expected in [([], 0), ([1, 2], 2), ((1, 2), 2), ({'a': 1}, 1), ({1, 2}, 2), (frozenset([1, 2]), 2), ('a\U0001f642', 2), (b'ab', 2), (bytearray(b'ab'), 2)]:
    assert len(value) == expected
mapping = {'a': 1, 'b': 2}
assert len(mapping.keys()) == 2
assert len(mapping.values()) == 2
assert len(mapping.items()) == 2
def numeric(n: int) -> int:
    total = 0
    for i in range(n):
        total += i
    return total
assert numeric(5) == 10
def shadow(len):
    return len(7, 8)
def replacement(left, right):
    return left + right
assert shadow(replacement) == 15
print('len-core-ok')
