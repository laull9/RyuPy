# ryupy: pending
# CPython 编译错误的类型、参数、位置字段完整保留，不替换成 NI。
import re
for pattern in (r'(?<=a*)b', r'(?<!a+)b', r'(?<=a?)b', r'(?<=a{1,2})b',
                r'(?<=a|bc)d', r'(a+)(?<=\1)b', r'(?<=(a)\1)b',
                r'(?<=\1)(a)', r'(?<=a', r'(?<=\q)a',
                '(?x)\n(?<=a|bc)d', br'(?<=a*)b'):
    try:
        re.compile(pattern)
    except re.error as exc:
        print(repr(pattern), type(exc).__name__, exc.args, exc.msg, repr(exc.pattern),
              exc.pos, exc.lineno, exc.colno, str(exc))
    else:
        raise AssertionError(('应当拒绝', pattern))
# 零宽断言不增加宽度；固定重复和等宽分支允许编译。
for pattern in (r'(?<=a{0})b', r'(?<=a{2,2})b', r'(?<=ab|cd)e', r'(?<=(?=a))a'):
    compiled = re.compile(pattern)
    print('valid', pattern, [m.span() for m in compiled.finditer('aab abcde')])
