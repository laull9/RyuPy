# 通用字符串下标错误及Optional非法tuple的repr按100码点截断。
import typing as t
def error(label, action):
    try:
        action()
    except TypeError as value:
        print(label, str(value))
    else:
        assert False
for key in [(int,), 'key', 1.5, None, []]:
    error('string-key', lambda: 'abc'[key])
for text in ['😀' * 110, 'a' * 110]:
    key = (text,)
    try:
        t.Optional[key]
    except TypeError as value:
        expected = 'typing.Optional requires a single type. Got ' + repr(key)[:100] + '.'
        assert str(value) == expected
        print('optional-key', str(value))
    else:
        assert False
