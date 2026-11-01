# 移植 termcolor 2.4.0 tests/test_termcolor.py 的颜色/背景/属性断言。
# 原测试名 test_color/test_on_color/test_attrs；显式筛选参数见下方断言表。
# MIT 许可见 licenses/termcolor-2.4.0.txt；用 force_color 替代 pytest TTY monkeypatch。
# 额外组合断言验证真实 ANSI 嵌套顺序，不以非 TTY plain 返回冒称着色支持。
from termcolor import colored

for color, expected in [
    ('black', '\x1b[30mtext\x1b[0m'),
    ('red', '\x1b[31mtext\x1b[0m'),
    ('green', '\x1b[32mtext\x1b[0m'),
    ('blue', '\x1b[34mtext\x1b[0m'),
    ('white', '\x1b[97mtext\x1b[0m'),
]:
    assert colored('text', color=color, force_color=True) == expected
for background, expected in [
    ('on_red', '\x1b[41mtext\x1b[0m'),
    ('on_blue', '\x1b[44mtext\x1b[0m'),
]:
    assert colored('text', on_color=background, force_color=True) == expected
for attr, expected in [
    ('bold', '\x1b[1mtext\x1b[0m'),
    ('dark', '\x1b[2mtext\x1b[0m'),
    ('underline', '\x1b[4mtext\x1b[0m'),
    ('blink', '\x1b[5mtext\x1b[0m'),
    ('reverse', '\x1b[7mtext\x1b[0m'),
    ('concealed', '\x1b[8mtext\x1b[0m'),
]:
    assert colored('text', attrs=[attr], force_color=True) == expected
assert colored('text', 'red', 'on_blue', ['bold', 'underline'], force_color=True) == '\x1b[4m\x1b[1m\x1b[44m\x1b[31mtext\x1b[0m'
assert colored('text', 'red', force_color=True, no_color=True) == 'text'
assert colored(42, 'green', force_color=True) == '\x1b[32m42\x1b[0m'
print('termcolor 强制 ANSI：16 场景通过')
