# ryupy: pending
# 通用expandtabs的真实源码合同：入口、码点、严格转换与同调用点重入。
events = []


class Index:
    def __index__(self):
        events.append('index')
        return 4


for text in ('', 'abc', 'a\tb\t'):
    events.clear()
    result = text.expandtabs(Index())
    assert events == ['index']
    assert result == ('a   b   ' if text == 'a\tb\t' else text)
assert 'a\tb\t'.expandtabs() == 'a       b       '
assert 'a\tb\t'.expandtabs(tabsize=4) == 'a   b   '
assert 'a\tb\t'.expandtabs(0) == 'ab'
assert 'a\tb\t'.expandtabs(-2147483648) == 'ab'
assert '\t'.expandtabs(True) == ' '
assert '\t'.expandtabs(False) == ''
assert 'abc'.expandtabs(2147483647) == 'abc'
# 容量边界仅由无分配C# Measure测试；此处不制造巨型输出。
for size in (-2147483649, 2147483648, -(2 ** 100), 2 ** 100):
    try:
        ''.expandtabs(size)
    except OverflowError as error:
        assert str(error) == 'Python int too large to convert to C int'
    else:
        raise AssertionError('参数应先拒绝')
for separator in ('\v', '\f', '\x85', '\u2028', '\u2029'):
    assert ('a' + separator + '\t').expandtabs(4) == 'a' + separator + '  '
for separator in ('\r', '\n'):
    assert ('abc' + separator + '\t').expandtabs(4) == 'abc' + separator + '    '
assert ('\U0001f600\t').expandtabs(4) == '\U0001f600   '
# 孤立surrogate通过chr生成，用ord验证，不借HIR文字丢失后的值自等。
for code in (0xD800, 0xDC00):
    expanded = (chr(code) + '\t').expandtabs(4)
    assert ord(expanded[0]) == code
    assert expanded[1:] == '   '


class Text(str):
    def parent(self):
        return super().expandtabs(4)


text = Text('a\t')
saved = text.expandtabs
text.expandtabs = lambda size: 'shadow'
assert text.expandtabs(4) == 'shadow'
assert saved(4) == 'a   '
assert str.expandtabs(text, 4) == 'a   '
assert text.parent() == 'a   '
assert type(saved(4)) is str
saved_exact = 'a\t'.expandtabs
assert saved_exact(tabsize=4) == 'a   '


def site(text, size):
    return text.expandtabs(size)


class Reenter:
    def __index__(self):
        events.append(site('inner\t', 8))
        return 4


events.clear()
assert site('outer\t', Reenter()) == 'outer   '
assert events == ['inner   ']
marker = LookupError('index-marker')


class Raising:
    def __index__(self):
        raise marker


try:
    ''.expandtabs(Raising())
except LookupError as error:
    assert error is marker
else:
    raise AssertionError('index异常不能丢失')
print('expandtabs core')

# 暂停前捕获原方法，恢复后不能重查实例新遮蔽。
text = Text('a\t')


def paused():
    return text.expandtabs((yield 'paused'))


pending = paused()
assert next(pending) == 'paused'
text.expandtabs = lambda size: 'shadow'
try:
    pending.send(4)
except StopIteration as done:
    assert done.value == 'a   '
else:
    raise AssertionError('生成器必须结束')


def duplicate():
    return 'a\t'.expandtabs(Index(), tabsize=4)


def unknown():
    return 'a\t'.expandtabs(bad=Index())


events.clear()
for call, message in ((duplicate, 'expandtabs() takes at most 1 argument (2 given)'),
                      (unknown, "'bad' is an invalid keyword argument for expandtabs()")):
    try:
        call()
    except TypeError as error:
        assert str(error) == message
    else:
        raise AssertionError('绑定必须拒绝')
assert events == []
print('expandtabs capture and binding')
