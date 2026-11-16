# ryupy: pending
# 五搜索的窗口、绑定、raw文本与双端代理边界；独立定向验收，不自动接入大e2e。
methods = ('count', 'find', 'rfind', 'index', 'rindex')


def check(text, sub, start, end, expected):
    for name, wanted in zip(methods, expected):
        try:
            actual = getattr(text, name)(sub, start, end)
        except ValueError as error:
            assert wanted is None
            assert str(error) == 'substring not found'
        else:
            assert actual == wanted, (name, actual, wanted)


check('a😀a😀z', '😀', 1, 4, (2, 1, 3, 1, 3))
check('a😀a😀z', 'a', -3, -1, (1, 2, 2, 2, 2))
check('aaa', 'aa', None, None, (1, 0, 1, 0, 1))
check('abc', '', 3, None, (1, 3, 3, 3, 3))
check('abc', '', 4, None, (0, -1, -1, None, None))
check('abc', '', 2, 1, (0, -1, -1, None, None))
check('abc', '', -(1 << 100), 1 << 100, (4, 0, 3, 0, 3))
check('abc', 'a', 1 << 100, None, (0, -1, -1, None, None))
check('', '', None, None, (1, 0, 0, 0, 0))
check('', 'x', None, None, (0, -1, -1, None, None))
check('abc', 'b', True, 2, (1, 1, 1, 1, 1))
check('abc', '', 0, -(1 << 100), (1, 0, 0, 0, 0))
for half in ('\ud83d', '\ude00'):
    check('x😀y', half, 1, 2, (0, -1, -1, None, None))
check('\ud800x\udc00', '\ud800', 0, 3, (1, 0, 0, 0, 0))
check('\ud800x\udc00', '\udc00', 0, 3, (1, 2, 2, 2, 2))
# 被拒的半pair候选之后/之前仍能找到真正孤立代理。
check('😀\ud83d', '\ud83d', None, None, (1, 1, 1, 1, 1))
check('\ude00😀', '\ude00', None, None, (1, 0, 0, 0, 0))


class Text(str):
    def __str__(self):
        raise AssertionError('不应调用__str__')

    def __len__(self):
        raise AssertionError('不应调用__len__')


text, sub = Text('a😀a'), Text('a')
for name, wanted in zip(methods, (2, 0, 2, 0, 2)):
    assert getattr(text, name)(sub) == wanted
    saved = getattr('a😀a', name)
    assert saved(sub, None, None) == wanted
    assert getattr(str, name)(text, sub, None, None) == wanted
    for args, kwargs, message in (
        ((), {}, name + '() takes at least 1 argument (0 given)'),
        (('a', 0, 3, 4), {}, name + '() takes at most 3 arguments (4 given)'),
        ((), {'sub': 'a'}, name + '() takes no keyword arguments'),
        (('a', 0, 3, 4), {'sub': 'a'}, name + '() takes no keyword arguments'),
    ):
        try:
            saved(*args, **kwargs)
        except TypeError as error:
            assert str(error) == message
        else:
            raise AssertionError('应拒绝错误绑定')
    try:
        getattr(str, name)(42, 'a')
    except TypeError as error:
        assert str(error) == "descriptor '" + name + "' for 'str' objects doesn't apply to a 'int' object"
    else:
        raise AssertionError('应拒绝错误receiver')


class Override(str):
    def count(self, *args):
        return 91


changed = Override('aba')
assert changed.count('a', 0, 3) == 91
assert str.count(changed, 'a', 0, 3) == 2
changed.count = lambda *args: 92
assert changed.count('a') == 92
assert str.count(changed, 'a') == 2
assert [1, 2, 1].count(1) == 2
assert (1, 2, 1).index(2) == 1
print('text search core')
