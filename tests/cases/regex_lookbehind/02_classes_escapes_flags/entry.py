# ryupy: pending
# 类、转义、局部和全局标志均参与后顾，不只处理某个 HTML 字面量。
import re
for pattern, flags, subject in [
    (r'(?<=[a-c\t\n/])X', 0, 'aX dX\tX\nX/X'),
    (r'(?<=[^a-z])x', 0, 'ax 1x Ωx'),
    (r'(?<=\w)x', 0, 'ax Ωx 1x _x'),
    (r'(?<=\w)x', re.ASCII, 'ax Ωx 1x _x'),
    (r'(?<=\d\s)x', 0, '1 x ２\tx'),
    (r'(?<=\x41\u0042)c', re.I, 'ABc abC'),
    (r'(?<=.)x', 0, 'ax\nx'),
    (r'(?<=.)x', re.S, 'ax\nx'),
    (r'(?<=^a)b', re.M, 'ab\nab'),
    (r'(?i:(?<=a))b', 0, 'ab Ab AB'),
    (r'(?<=(?i:a))b', 0, 'ab Ab AB'),
    ('(?<= [a-c] # 后顾字符\n ) x', re.X, 'ax bx dx'),
    (r'(?<=\[)x', 0, '[x]'),
]:
    compiled = re.compile(pattern, flags)
    print(repr(pattern), int(flags), [(m.span(), m.group()) for m in compiled.finditer(subject)])
