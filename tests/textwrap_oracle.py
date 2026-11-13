"""固定 CPython 同次生成 textwrap 逐项预期；只保留手写输入，不写基线文件或摘要。"""
import itertools
import json
import sys
import textwrap

if sys.implementation.name != 'cpython' or sys.version_info[:3] != (3, 12, 13):
    raise SystemExit('必须使用固定 CPython 3.12.13')

# 原1200项输入矩阵：保留Unicode、空白、连字符与所有已支持选项组合。
WORDS = ['a', 'abc', 'a-b', 'a-b-c', 'ab-cd', 'ab-c', 'a-bc', 'ab-cd-ef', 'foo--bar',
         'a---b', '--ab', 'ab--', 'ab--cd--ef', '12-34', 'ab-1c', 'a_-bc', '中日-韩国',
         '😀😀abcd', '\u0301ab-cd', 'a\u00a0', '\u00a0', 'x \u00a0 y', '  a b  ',
         'goof-ball', 'a\tb', 'a\nb\rc', 'hello world', 'a\v\fb', '', '   ']
MATRIX_OPTIONS = [{}, {'break_long_words': False}, {'break_on_hyphens': False},
                  {'drop_whitespace': False}, {'initial_indent': '>', 'subsequent_indent': '😀'},
                  {'expand_tabs': False}, {'replace_whitespace': False}, {'tabsize': 2}]


def cases(mode):
    if mode == 'matrix':
        yield from itertools.product(WORDS, [1, 2, 3, 5, 8], MATRIX_OPTIONS)
    elif mode == 'exhaustive':
        # 原4字母表/长度1..6/4宽度/4选项的87360个输入，逐结果比较断行边界。
        for length in range(1, 7):
            for chars in itertools.product('ab- ', repeat=length):
                for width, options in itertools.product([1, 2, 3, 5], MATRIX_OPTIONS[:4]):
                    yield ''.join(chars), width, options
    else:
        raise SystemExit('未知textwrap oracle模式')


rows = [{'text': text, 'width': width, 'options': options,
         'expected': textwrap.wrap(text, width, **options)}
        for text, width, options in cases(sys.argv[1])]
json.dump(rows, sys.stdout, ensure_ascii=True, separators=(',', ':'))
sys.stdout.write('\n')
