# ryupy: pending
# 独立长尾：Python 相邻孤立代理与真实 astral 是不同字符串，不把 UTF-16 合并域当支持。
import re

pair = chr(0xd83d) + chr(0xde00)
astral = chr(0x1f600)
print('distinct', len(pair), len(astral), pair == astral,
      [ord(c) for c in pair], [ord(c) for c in astral])
assert len(pair) == 2 and len(astral) == 1 and pair != astral
for label, subject in [('pair', pair + 'x'), ('astral', astral + 'x')]:
    for name, pattern in [('dot', r'(?<=.)x'), ('two', r'(?<=.{2})x'),
                          ('pair-literal', '(?<=' + pair + ')x'),
                          ('astral-literal', '(?<=' + astral + ')x')]:
        compiled = re.compile(pattern)
        print(label, name, [m.span() for m in compiled.finditer(subject)])
        for pos in range(len(subject) + 1):
            match = compiled.match(subject, pos, len(subject))
            print('pos', pos, None if match is None else match.span())
# CPython 不能把两个代理与一个 astral 分支视为等宽，即使 UTF-16 表示相同。
try:
    re.compile('(?<=' + pair + '|' + astral + ')x')
except re.error as exc:
    print('width', exc.msg, exc.pattern, exc.pos, exc.lineno, exc.colno)
else:
    raise AssertionError('相邻代理分支必须保持两个码点宽度')
