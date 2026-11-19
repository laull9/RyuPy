"""T2：干净进程核精确split与默认root/继承/module结果；不制造性能压力。"""
import textwrap
from textwrap import TextWrapper as TW


# 精确True与整数1分开；这些块来自唯一CP原流，不以最终行相等替代分块。
SPLITS = [
    ("T2.split.00.true", "", True, []),
    ("T2.split.00.false", "", False, []),
    ("T2.split.00.one", "", 1, []),
    ("T2.split.01.true", "  a b  ", True, ["  ", "a", " ", "b", "  "]),
    ("T2.split.01.false", "  a b  ", False, ["  ", "a", " ", "b", "  "]),
    ("T2.split.01.one", "  a b  ", 1, ["  ", "a", " ", "b", "  "]),
    ("T2.split.02.true", "a\tb\nc\u000bd\fe\rf", True, ["a", "\t", "b", "\n", "c", "\u000b", "d", "\f", "e", "\r", "f"]),
    ("T2.split.02.false", "a\tb\nc\u000bd\fe\rf", False, ["a", "\t", "b", "\n", "c", "\u000b", "d", "\f", "e", "\r", "f"]),
    ("T2.split.02.one", "a\tb\nc\u000bd\fe\rf", 1, ["a", "\t", "b", "\n", "c", "\u000b", "d", "\f", "e", "\r", "f"]),
    ("T2.split.03.true", "a b c", True, ["a b", " ", "c"]),
    ("T2.split.03.false", "a b c", False, ["a b", " ", "c"]),
    ("T2.split.03.one", "a b c", 1, ["a b", " ", "c"]),
    ("T2.split.04.true", "goof-ball", True, ["goof-", "ball"]),
    ("T2.split.04.false", "goof-ball", False, ["goof-ball"]),
    ("T2.split.04.one", "goof-ball", 1, ["goof-ball"]),
    ("T2.split.05.true", "ab-cd-ef", True, ["ab-", "cd-", "ef"]),
    ("T2.split.05.false", "ab-cd-ef", False, ["ab-cd-ef"]),
    ("T2.split.05.one", "ab-cd-ef", 1, ["ab-cd-ef"]),
    ("T2.split.06.true", "a-b-c", True, ["a-b-c"]),
    ("T2.split.06.false", "a-b-c", False, ["a-b-c"]),
    ("T2.split.06.one", "a-b-c", 1, ["a-b-c"]),
    ("T2.split.07.true", "foo--bar", True, ["foo", "--", "bar"]),
    ("T2.split.07.false", "foo--bar", False, ["foo--bar"]),
    ("T2.split.07.one", "foo--bar", 1, ["foo--bar"]),
    ("T2.split.08.true", "a------b", True, ["a", "------", "b"]),
    ("T2.split.08.false", "a------b", False, ["a------b"]),
    ("T2.split.08.one", "a------b", 1, ["a------b"]),
    ("T2.split.09.true", "你好-世界 😀ab", True, ["你好-", "世界", " ", "😀ab"]),
    ("T2.split.09.false", "你好-世界 😀ab", False, ["你好-世界", " ", "😀ab"]),
    ("T2.split.09.one", "你好-世界 😀ab", 1, ["你好-世界", " ", "😀ab"]),
]
for label, text, flag, expected in SPLITS:
    wrapper = TW(break_on_hyphens=flag)
    actual = wrapper._split(text)
    again = wrapper._split(text)
    assert type(actual) is list and actual == expected and again == expected
    assert actual is not again
    print(label, actual)


class Plain(TW):
    pass


NORMAL = [
    ("T2.normal.00", "hello world", {"width": 2}, ["he", "ll", "o ", "wo", "rl", "d"]),
    ("T2.normal.01", "a\tb", {"width": 4}, ["a", "b"]),
    ("T2.normal.02", "a\tb", {"width": 3, "expand_tabs": False}, ["a b"]),
    ("T2.normal.03", "a\nb c", {"width": 3, "replace_whitespace": False}, ["a\nb", "c"]),
    ("T2.normal.04", "a b c", {"width": 3}, ["a b", "c"]),
    ("T2.normal.05", "你好世界abc", {"width": 3}, ["你好世", "界ab", "c"]),
    ("T2.normal.06", "😀😀abcd", {"width": 3}, ["😀😀a", "bcd"]),
    ("T2.normal.07", "goof-ball a-b-c", {"width": 5, "break_on_hyphens": False}, ["goof-", "ball", "a-b-c"]),
    ("T2.normal.08", "abcdef", {"width": 2, "break_long_words": False}, ["abcdef"]),
    ("T2.normal.09", "  a b  ", {"width": 3, "drop_whitespace": False}, ["  a", " b", "  "]),
    ("T2.normal.10", "abc", {"width": 1, "initial_indent": ">>", "subsequent_indent": "--"}, [">>a", "--b", "--c"]),
    ("T2.normal.11", "", {"width": 2}, []),
]
for label, text, options, expected in NORMAL:
    wrapper = TW(**options)
    inherited = Plain(**options)
    assert type(wrapper) is TW and type(inherited) is Plain and isinstance(inherited, TW)
    assert wrapper.wrap(text) == expected and inherited.wrap(text) == expected
    assert wrapper.fill(text) == '\n'.join(expected)
    assert textwrap.wrap(text, **options) == expected
    assert textwrap.fill(text, **options) == '\n'.join(expected)
    print(label, wrapper.wrap(text), repr(wrapper.fill(text)), inherited.wrap(text),
          textwrap.wrap(text, **options), repr(textwrap.fill(text, **options)))

# 只改已存在的配置字段，不增加root方法/属性槽；新值仍须每次读取。
live = TW(3)
assert live.wrap('ab cd') == ['ab', 'cd']
live.width = 8
assert live.wrap('ab cd') == ['ab cd']
live.initial_indent = '>'
assert live.wrap('ab cd') == ['>ab cd']
print('T2.live-config', live.width, live.initial_indent, live.wrap('ab cd'))
