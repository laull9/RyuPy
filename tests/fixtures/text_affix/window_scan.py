# ryupy: pending
# 有界扫描应用：常用窗口、按序候选、真实子类文本及非BMP边界，不展开绑定矩阵。
class Poison:
    def __str__(self):
        raise AssertionError('候选不应调用str')


class RawText(str):
    def __str__(self):
        raise AssertionError('真实文本不应调用str覆盖')


record = '[alpha]'
window = (record.startswith(('z', 'alpha'), 1, 6), record.endswith('alpha', 1, 6))
assert window == (True, True)
print('scan', window)

huge = 10 ** 100
bounds = (
    'abcd'.endswith('bc', -3, -1),
    'abcd'.startswith('a', None, None),
    'abc'.startswith('b', True),
    'abc'.endswith('a', False, True),
    'abcd'.endswith('cd', -huge, huge),
    'abcd'.startswith('', huge),
    'abcd'.endswith('', 0, -huge),
)
assert bounds == (True, True, True, True, True, False, True)
print('bounds', bounds)

empty = ('abcd'.startswith('', 4), 'abcd'.endswith('', 5), 'abcd'.startswith('', 3, 2))
assert empty == (True, False, False)
print('empty', empty)

text = 'A\U0001f600B\U0001d11eC'
unicode_window = (
    text.startswith('\U0001f600', 1, 2),
    text.endswith('\U0001d11e', 1, 4),
    text.endswith('\U0001f600B', -4, -2),
    text.startswith('', 5),
    text.endswith('', 6),
)
assert unicode_window == (True, True, True, True, False)
# 正常scalar匹配孤代理候选必须False；不把本域划入独立代理拼接D009。
scalar = '\U0001f600'
partial = (scalar.startswith('\ud83d'), scalar.startswith('\ude00'),
           scalar.endswith('\ud83d'), scalar.endswith('\ude00'))
assert partial == (False, False, False, False)
print('unicode', unicode_window, partial)

raw = RawText('abc')
assert raw.startswith(RawText('a'), None, None)
assert str.endswith(raw, RawText('c'), 0, 3)
assert raw.endswith(('z', RawText('c'), Poison()))
assert raw.startswith(('a', Poison()))
assert not raw.endswith(())
try:
    raw.startswith(('z', Poison()))
except TypeError as error:
    assert str(error) == 'tuple for startswith must only contain str, not Poison'
    print('candidate', str(error))
else:
    raise AssertionError('应访问并拒绝第二候选')
print('text-affix-window-ok')
