# ryupy: pending
# 首切独立合同：后顾无捕获，外围组和可变源保留身份、延迟读取与后续迭代可见性。
import re
for subject in (b'ab cb \xffb', bytearray(b'ab cb \xffb')):
    for pattern in (br'(?<=a)(b)', br'(?<!a)(b)', br'(?<=[a\xff])(b)', br'(?<=\w)(b)'):
        compiled = re.compile(pattern)
        print(type(subject).__name__, pattern,
              [(m.span(), m.groups(), m.string is subject, m.re is compiled)
               for m in compiled.finditer(subject)])
subject = bytearray(b'ab ab')
alias = subject
compiled = re.compile(br'(?<=a)(b)')
match = compiled.search(subject)
print('before', match.group(), match.groups(), match.regs, match.string is alias)
subject[0] = ord('z')
subject[1] = ord('c')
assert match.group() == b'c' and match.group(1) == b'c'
assert match.string is alias
print('after', match.group(), match.groups(), match.regs, match.string is alias)
subject = bytearray(b'ab ab')
iterator = compiled.finditer(subject)
first = next(iterator)
subject[3] = ord('z')
remaining = [m.span() for m in iterator]
assert remaining == []
print('remove-later', first.span(), remaining)
subject = bytearray(b'ab zb')
iterator = compiled.finditer(subject)
first = next(iterator)
subject[3] = ord('a')
remaining = [m.span() for m in iterator]
assert remaining == [(4, 5)]
print('create-later', first.span(), remaining)
events = []
def replace(match):
    events.append((match.span(), match.group(1)))
    return b'B'
print('callback', compiled.sub(replace, bytearray(b'ab ab')), events)
print('split', compiled.split(bytearray(b'ab ab')))
for pattern, subject in [(r'(?<=a)b', b'ab'), (br'(?<=a)b', 'ab')]:
    try:
        re.search(pattern, subject)
    except TypeError as exc:
        print(type(exc).__name__, str(exc))
    else:
        raise AssertionError('应拒绝混用文本和二进制')
