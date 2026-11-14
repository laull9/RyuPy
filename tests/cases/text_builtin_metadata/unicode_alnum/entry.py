# Unicode 字母数字按码点分类；孤代理项由 chr 构造，不依赖字面量物化。
items = [
    ('empty', '', False),
    ('ascii', 'Az09', True),
    ('underscore', '_', False),
    ('mixed_underscore', 'a_1', False),
    ('space', ' ', False),
    ('chinese', '中文', True),
    ('greek', 'Αλφα', True),
    ('supplementary_letter', chr(0x10400), True),
    ('decimal', '١२９', True),
    ('superscript', '²³', True),
    ('fraction', '½⅔', True),
    ('roman', 'ⅧⅫ', True),
    ('combining', chr(0x301), False),
    ('combining_word', 'e' + chr(0x301), False),
    ('emoji', '😀', False),
    ('high_surrogate', chr(0xd800), False),
    ('low_surrogate', chr(0xdc00), False),
]
for label, value, expected in items:
    saved = value.isalnum
    direct = value.isalnum()
    assert direct is expected
    assert saved() is expected
    assert str.isalnum(value) is expected
    print(label, direct)
class S(str):
    def isalnum(self):
        return 'override'
s = S('中文²')
assert s.isalnum() == 'override'
assert str.isalnum(s) is True
print('override', s.isalnum(), str.isalnum(s))
for call in [lambda: 'x'.isalnum(1), lambda: 'x'.isalnum(x=1),
             lambda: str.isalnum(1), lambda: str.isalnum(),
             lambda: str.isalnum('x', 1)]:
    try:
        call()
    except TypeError:
        print('invalid', 'TypeError')
    else:
        raise AssertionError('必须拒绝非法参数或 receiver')
print('unicode-alnum: ok')
