"""仅调用固定解释器API生成黑盒测试事实；不提取或复制textwrap实现/正则。"""
import json
from pathlib import Path
import sys
import textwrap

if sys.version_info[:3] != (3, 12, 13):
    raise SystemExit('必须使用固定CPython3.12.13')
words = ['a', 'abc', 'a-b', 'a-b-c', 'ab-cd', 'ab-c', 'a-bc', 'ab-cd-ef', 'foo--bar',
         'a---b', '--ab', 'ab--', 'ab--cd--ef', '12-34', 'ab-1c', 'a_-bc', '中日-韩国',
         '😀😀abcd', '\u0301ab-cd', 'a\u00a0', '\u00a0', 'x \u00a0 y', '  a b  ',
         'goof-ball', 'a\tb', 'a\nb\rc', 'hello world', 'a\v\fb', '', '   ']
rows = []
for text in words:
    for width in [1, 2, 3, 5, 8]:
        for options in [{}, {'break_long_words': False}, {'break_on_hyphens': False},
                        {'drop_whitespace': False}, {'initial_indent': '>', 'subsequent_indent': '😀'},
                        {'expand_tabs': False}, {'replace_whitespace': False}, {'tabsize': 2}]:
            rows.append({'text': text, 'width': width, 'options': options,
                         'expected': textwrap.wrap(text, width, **options)})
path = Path(__file__).parent / 'data/textwrap_cpython_3_12_13.json'
path.write_text(json.dumps(rows, ensure_ascii=True, separators=(',', ':')) + '\n')
print(len(rows), 'cases', path)
# 小字母表穷举摘要：固定分隔字节不属于输入，验证词/连字符/空格组合的断行位置。
import itertools
value = 14695981039346656037
count = 0
for length in range(1, 7):
    for chars in itertools.product('ab- ', repeat=length):
        text = ''.join(chars)
        for width in (1, 2, 3, 5):
            for options in ({}, {'break_long_words': False}, {'break_on_hyphens': False}, {'drop_whitespace': False}):
                for line in textwrap.wrap(text, width, **options):
                    for byte in line.encode('utf-8') + b'\xff':
                        value = ((value ^ byte) * 1099511628211) & ((1 << 64) - 1)
                value = ((value ^ 254) * 1099511628211) & ((1 << 64) - 1)
                count += 1
print('exhaustive cases', count, 'fnv64', value)
