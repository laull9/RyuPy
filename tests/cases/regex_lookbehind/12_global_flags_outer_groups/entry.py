# ryupy: pending
# 首切独立合同：全局标志、字符类、转义与外围捕获，不依赖后顾内部捕获或局部标志。
import re
for label, pattern, flags, subject in [
    ('ignorecase', r'(?<=a)b', re.I, 'ab Ab AB'),
    ('unicode-word', r'(?<=\w)x', 0, 'ax Ωx 𐐀x _x 😀x'),
    ('ascii-word', r'(?<=\w)x', re.A, 'ax Ωx 𐐀x _x 😀x'),
    ('unicode-digit', r'(?<=\d)x', 0, '1x ２x'),
    ('ascii-digit', r'(?<=\d)x', re.A, '1x ２x'),
    ('dot', r'(?<=.)x', 0, 'ax\nx 😀x'),
    ('dotall', r'(?<=.)x', re.S, 'ax\nx 😀x'),
    ('verbose', '(?<= [a-c] # 注释\n ) x', re.X, 'ax bx dx'),
    ('escaped', r'(?<=\x41\u0042)c', re.I, 'ABc abC'),
    ('positive-class', r'(?<=[a-c\t\n/])X', 0, 'aX dX\tX\nX/X'),
    ('negative-class', r'(?<=[^a-z])x', 0, 'ax 1x Ωx 😀x'),
    ('escaped-bracket', r'(?<=\[)x', 0, '[x]'),
    ('combined', r'(?<=[a-z])x', re.I | re.A, 'Ax Kx kX'),
    ('bytes-ignorecase', br'(?<=a)b', re.I, b'ab Ab AB'),
    ('bytes-class', br'(?<=[a\xff])b', 0, b'ab \xffb cb'),
    ('bytes-dotall', br'(?<=.)x', re.S, b'ax\nx'),
]:
    compiled = re.compile(pattern, flags)
    print(label, [(m.span(), m.group()) for m in compiled.finditer(subject)])
# 内部非捕获分组不占编号，前后外围捕获的编号及名字保持原模式顺序。
for pattern, subject in [
    (r'(a)(?<=a)(b)', 'ab'),
    (r'(?P<left>a)(?<=(?:a|b))(?P<right>b)', 'ab'),
    (r'(z)?(?<!a)(b)', 'zb'),
    (r'(a)(?<=a)(b)\2', 'abb'),
    (r'(?<=a)(b)|(c)', 'ab c'),
]:
    compiled = re.compile(pattern)
    match = compiled.search(subject)
    assert match is not None
    print('groups', pattern, compiled.groups, compiled.groupindex, match.span(),
          match.groups(), match.groupdict(), match.regs, match.lastindex, match.lastgroup)
    print('all', [(m.span(), m.groups()) for m in compiled.finditer(subject)])
assert re.compile(r'(a)(?<=a)(b)').search('ab').groups() == ('a', 'b')
