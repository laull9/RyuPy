# 首切独立合同：变宽错误空位置元数据；完整模式语法错误先于后顾宽度检查。
import re
for pattern in (r'(?<=a*)b', r'(?<!a+)b', r'(?<=a?)b', r'(?<=a{1,2})b',
                r'(?<=a|bc)d', '(?x)\n(?<=a|bc)d', '😀(?<=a|bc)d',
                br'(?<=a*)b', br'(?<!a{1,2})b', br'(?<=a|bc)d'):
    try:
        re.compile(pattern)
    except re.error as exc:
        assert exc.msg == 'look-behind requires fixed-width pattern'
        assert exc.pattern is None and exc.pos is None
        assert exc.lineno is None and exc.colno is None
        print('width', repr(pattern), type(exc).__name__, exc.args, exc.msg,
              exc.pattern, exc.pos, exc.lineno, exc.colno)
    else:
        raise AssertionError('应拒绝变宽后顾')
# 后顾自身合法或变宽都不吞掉其后出现的语法错误；emoji 的位置按码点计。
for pattern in (r'(?<=a*)b[', r'(?<!a+)b\q', r'(?<=a|bc)d(', r'(?<=a)b)',
                '😀(?<=a*)[', '(?x)\n(?<=a*)\n[', '(?x)\n😀(?<=a*)\n\\q',
                br'(?<=a*)b[', br'(?<!a+)b\q', b'(?x)\n(?<=a*)\n['):
    try:
        re.compile(pattern)
    except re.error as exc:
        assert exc.msg != 'look-behind requires fixed-width pattern'
        assert exc.pattern == pattern and exc.pos is not None
        print('syntax', repr(pattern), type(exc).__name__, exc.args, exc.msg,
              repr(exc.pattern), exc.pos, exc.lineno, exc.colno, str(exc))
    else:
        raise AssertionError('应拒绝完整模式语法错误')
# 同宽选择和零宽重复必须与上述错误分开。
for pattern in (r'(?<=a{0})b', r'(?<=a{2,2})b', r'(?<=ab|cd)e'):
    print('valid', pattern, [m.span() for m in re.finditer(pattern, 'aab abcde')])
