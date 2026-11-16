# 保留原接收者/分隔符身份及精确片段类型；Unicode使用动态chr，避免既有源码代理替换边界。
class Text(str):
    def __str__(self):
        raise AssertionError('不能转换真实文本')
    def __repr__(self):
        raise AssertionError('不能格式化真实文本')


for method in ('partition', 'rpartition'):
    root = getattr(str, method)
    for source_type in (str, Text):
        for sep_type in (str, Text):
            for case in ('hit', 'miss', 'whole', 'empty'):
                raw_sep = ''.join(['separator', '_dynamic', '_42'])
                sep = sep_type(raw_sep)
                raw_source = {'hit': 'left' + raw_sep + 'right',
                              'miss': 'a dynamic untouched payload 42',
                              'whole': ''.join(['separator', '_dynamic', '_42']), 'empty': ''}[case]
                source = source_type(raw_source)
                if case == 'whole':
                    assert source is not sep
                saved = getattr(source, method)
                result = saved(sep)
                rooted = root(source, sep)
                assert type(result) is tuple and len(result) == 3
                assert rooted == result
                if case in ('hit', 'whole'):
                    expected = ('left', sep, 'right') if case == 'hit' else ('', sep, '')
                    assert result == expected
                    assert result[1] is sep and rooted[1] is sep
                    assert type(result[0]) is str and type(result[2]) is str
                    assert type(result[1]) is sep_type
                else:
                    whole = 0 if method == 'partition' else 2
                    assert result[whole] is source and rooted[whole] is source
                    for index in (0, 1, 2):
                        if index != whole:
                            assert result[index] == '' and type(result[index]) is str
                # 根str转换仅用于安全打印；算法本身必须绕过__str__/__repr__覆盖。
                print(method + '-identity-' + source_type.__name__ + '-' + sep_type.__name__ + '-' + case,
                      [str.__str__(part) for part in result],
                      [type(part).__name__ for part in result],
                      [part is source for part in result], [part is sep for part in result])
    same = Text(''.join(['same', '_dynamic', '_value']))
    result = root(same, same)
    assert result[1] is same and type(result[0]) is str and type(result[2]) is str
    print(method + '-same-object', result[1] is same, type(result[1]).__name__)


# 正常astral不能匹配半对；拒绝非法候选后必须继续寻找后续/前面的合法候选。
high = chr(0xD83D)
low = chr(0xDE00)
scalar = chr(0x1F600)
maximum = chr(0x10FFFF)
for label, source, sep, first, last in [
    ('astral', 'a' + scalar + 'b' + scalar + 'c', scalar,
     ('a', scalar, 'b' + scalar + 'c'), ('a' + scalar + 'b', scalar, 'c')),
    ('combining', 'e\u0301é', 'é', ('e\u0301', 'é', ''), ('e\u0301', 'é', '')),
    ('no-normalization', 'e\u0301', 'é', ('e\u0301', '', ''), ('', '', 'e\u0301')),
    ('high-half', 'a' + scalar + 'b', high, ('a' + scalar + 'b', '', ''), ('', '', 'a' + scalar + 'b')),
    ('low-half', 'a' + scalar + 'b', low, ('a' + scalar + 'b', '', ''), ('', '', 'a' + scalar + 'b')),
    ('isolated-high', 'a' + high + 'b', high, ('a', high, 'b'), ('a', high, 'b')),
    ('isolated-low', 'a' + low + 'b', low, ('a', low, 'b'), ('a', low, 'b')),
    ('skip-first-high', scalar + 'x' + high + 'z', high,
     (scalar + 'x', high, 'z'), (scalar + 'x', high, 'z')),
    ('skip-last-high', high + 'x' + scalar, high,
     ('', high, 'x' + scalar), ('', high, 'x' + scalar)),
    ('skip-first-low', scalar + 'x' + low + 'z', low,
     (scalar + 'x', low, 'z'), (scalar + 'x', low, 'z')),
    ('skip-last-low', low + 'x' + scalar, low,
     ('', low, 'x' + scalar), ('', low, 'x' + scalar)),
    ('end-half', 'a' + scalar + 'b', 'a' + high,
     ('a' + scalar + 'b', '', ''), ('', '', 'a' + scalar + 'b')),
    ('start-half', 'a' + scalar + 'b', low + 'b',
     ('a' + scalar + 'b', '', ''), ('', '', 'a' + scalar + 'b')),
    ('skip-end-half', 'a' + scalar + 'a' + high + '!', 'a' + high,
     ('a' + scalar, 'a' + high, '!'), ('a' + scalar, 'a' + high, '!')),
    ('skip-start-half', low + 'bx' + scalar + 'b', low + 'b',
     ('', low + 'b', 'x' + scalar + 'b'), ('', low + 'b', 'x' + scalar + 'b')),
    # 非法候选和合法候选也能重叠，跳过分隔符全长会漏匹配。
    ('overlap-after-invalid', scalar + low + low, low + low,
     (scalar, low + low, ''), (scalar, low + low, '')),
    ('overlap-before-invalid', high + high + scalar, high + high,
     ('', high + high, scalar), ('', high + high, scalar)),
    ('maximum', 'x' + maximum + 'y', maximum, ('x', maximum, 'y'), ('x', maximum, 'y')),
]:
    for method, expected in [('partition', first), ('rpartition', last)]:
        result = getattr(source, method)(sep)
        assert result == expected and type(result) is tuple
        assert all(type(part) is str for part in result)
        assert ''.join(result) == source
        if expected[1]:
            assert result[1] is sep
        else:
            assert result[0 if method == 'partition' else 2] is source
        print(method + '-unicode-' + label, result)
print('identity-unicode-ok')
