# ryupy: pending
# bytes/bytearray 的身份和延迟读取不得被后顾快照优化破坏。
import re
for subject in (b'ab cb \xffb', bytearray(b'ab cb \xffb')):
    for pattern in (br'(?<=a)b', br'(?<!a)b', br'(?<=[a\xff])b', br'(?<=\w)b'):
        compiled = re.compile(pattern)
        print(type(subject).__name__, pattern,
              [(m.span(), m.group(), m.string is subject) for m in compiled.finditer(subject)])
subject = bytearray(b'ab ab')
compiled = re.compile(br'(?<=(a))b')
match = compiled.search(subject)
print('before', match.group(), match.groups(), match.string is subject)
subject[0] = ord('z')
subject[1] = ord('c')
print('after', match.group(), match.groups(), match.span(), match.string is subject)
subject = bytearray(b'ab ab')
iterator = compiled.finditer(subject)
first = next(iterator)
subject[3] = ord('z')
print('iterator', first.span(), [m.span() for m in iterator])
for pattern, subject in [(r'(?<=a)b', b'ab'), (br'(?<=a)b', 'ab')]:
    try:
        re.search(pattern, subject)
    except TypeError as exc:
        print(type(exc).__name__, str(exc))
