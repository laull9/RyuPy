# bool绑定与共享真值协议；不修改独立len()或通用展开错误帧。
log = []
class Both:
    def __bool__(self):
        log.append('bool')
        return False
    def __len__(self):
        log.append('unused-len')
        return 2
class Length:
    def __init__(self, value):
        self.value = value
    def __len__(self):
        log.append('len')
        return self.value
class Index:
    def __index__(self):
        log.append('index')
        return 2
class BadBool:
    def __bool__(self):
        log.append('bad-bool')
        return 1
    def __len__(self):
        log.append('unused-len')
        return 2
class Disabled:
    __bool__ = None
    def __len__(self):
        log.append('unused-len')
        return 2
class IntOnly:
    def __int__(self):
        log.append('unused-int')
        return 2
class Empty:
    pass
value = Both()
saved = bool
def invoke(function, item):
    return function(item)
assert bool() is False
assert bool(*[]) is False
assert bool(value) is False
assert saved(value) is False
assert invoke(saved, value) is False
assert bool(*[value], **{}) is False
assert log == ['bool', 'bool', 'bool', 'bool']
log.clear()
assert bool(Length(Index())) is True
assert log == ['len', 'index']
log.clear()
assert bool(Length(False)) is False
assert bool(Length(True)) is True
assert bool(Length(9223372036854775807)) is True
assert log == ['len', 'len', 'len']
log.clear()
assert bool(Empty()) is True
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
reject('many', lambda: bool(mark('a', value), mark('b', 2)), TypeError, ['a', 'b'])
reject('keyword', lambda: bool(x=mark('x', value)), TypeError, ['x'])
reject('many-keyword', lambda: bool(mark('a', value), mark('b', 2), x=mark('x', 3)), TypeError, ['a', 'b', 'x'])
reject('star-many', lambda: bool(*mark('star', [value, 2])), TypeError, ['star'])
reject('star-invalid', lambda: bool(*mark('star', 3)), TypeError, ['star'])
reject('double-invalid', lambda: bool(mark('a', value), **mark('kw', 3)), TypeError, ['a', 'kw'])
reject('bad-bool', lambda: bool(BadBool()), TypeError, ['bad-bool'])
reject('disabled', lambda: bool(Disabled()), TypeError, [])
reject('negative', lambda: bool(Length(-1)), ValueError, ['len'])
reject('negative-huge', lambda: bool(Length(-(10 ** 100))), ValueError, ['len'])
reject('overflow', lambda: bool(Length(9223372036854775808)), OverflowError, ['len'])
reject('bad-length', lambda: bool(Length(1.5)), TypeError, ['len'])
reject('int-only', lambda: bool(Length(IntOnly())), TypeError, ['len'])
reject('not-negative', lambda: not Length(-1), ValueError, ['len'])
reject('and-negative', lambda: Length(-1) and 1, ValueError, ['len'])
reject('or-negative', lambda: Length(-1) or 1, ValueError, ['len'])
log.clear()
token = []
assert (value and mark('unused', token)) is value
assert (value or mark('right', token)) is token
assert log == ['bool', 'bool', 'right']
log.clear()
if value:
    raise AssertionError('wrong branch')
assert log == ['bool']
log.clear()
assert not value
assert log == ['bool']
# 实例同名属性不能替代特殊类槽；后改类槽应生效。
value.__bool__ = lambda: True
assert bool(value) is False
Both.__bool__ = lambda self: True
assert bool(value) is True
# 重入调用一次，外层返回仍须实际bool。
class Reentrant:
    def __bool__(self):
        log.append('outer')
        return bool(Length(Index()))
log.clear()
assert bool(Reentrant()) is True
assert log == ['outer', 'len', 'index']
def shadow(bool):
    return bool(7, 8)
def replacement(left, right):
    return left + right
assert shadow(replacement) == 15
print('bool-ok')
