# 通用正负后顾：固定重复、等宽分支、空后顾和输入起点。
import re
for pattern in (r'(?<=ab)c', r'(?<!ab)c', r'(?<=a{2})b', r'(?<=ab|cd)e',
                r'(?<!ab|cd)e', r'(?<=)a', r'(?<!)a', r'(?<=ab)', r'(?<!ab)'):
    compiled = re.compile(pattern)
    for subject in ('', 'a', 'c', 'abc', 'aac', 'aab', 'cde', 'abe xce', 'abab'):
        print(pattern, repr(subject), [(m.span(), m.group()) for m in compiled.finditer(subject)])
assert re.search(r'(?<=ab)c', 'abc').span() == (2, 3)
assert re.match(r'(?<!ab)c', 'c').span() == (0, 1)
