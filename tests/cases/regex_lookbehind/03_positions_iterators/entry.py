# pos 不是后顾的左边界；endpos 限制右边界，所有入口保持同一语义。
import re
for pattern, subject in [(r'(?<=a)b', 'abxab'), (r'(?<!a)b', 'bbabb'),
                         (r'(?<=a)', 'aaa'), (r'(?<=a)|b', 'ababa')]:
    compiled = re.compile(pattern)
    for pos, endpos in [(-2, 99), (0, 0), (0, 5), (1, 2), (2, 4), (4, 5), (5, 5), (9, 10), (3, 1)]:
        for name in ('match', 'search', 'fullmatch'):
            match = getattr(compiled, name)(subject, pos, endpos)
            print(pattern, name, pos, endpos, None if match is None else
                  (match.span(), match.group(), match.pos, match.endpos))
        print('iter', pos, endpos, [(m.span(), m.group()) for m in compiled.finditer(subject, pos, endpos)])
    print('module', re.findall(pattern, subject), [m.span() for m in re.finditer(pattern, subject)])
assert re.compile(r'(?<=a)b').fullmatch('ab', 1, 2).span() == (1, 2)
