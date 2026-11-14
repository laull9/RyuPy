# ryupy: pending
# 后顾捕获的跨度可早于整体匹配；负后顾及失败分支不得泄漏捕获。
import re
for pattern, subject in [(r'(?<=(a))b', 'ab'), (r'(?<=(?P<left>a))(?P<right>b)', 'ab'),
                         (r'(?<!(a))b', 'cb'), (r'(?:(?<=(a))c|b)', 'ab'),
                         (r'(?<=(a)(b))c', 'abc')]:
    compiled = re.compile(pattern)
    match = compiled.search(subject)
    alias = match
    print(pattern, match.span(), match.groups(), match.groupdict(), match.regs,
          match.lastindex, match.lastgroup, alias is match, match.re is compiled, match.string is subject)
    print('spans', [match.span(i) for i in range(compiled.groups + 1)])
    print('expand', match.expand(r'<\g<0>>'))
events = []
compiled = re.compile(r'(?<=(a))b')
saved_search = compiled.search
saved_sub = compiled.sub
def replace(match):
    events.append((match.span(), match.span(1), match.group(1)))
    return match.group(1).upper()
print('bound', saved_search('ab').span(), saved_sub(replace, 'ab ab'), events)
print('subn', compiled.subn(r'<\1>', 'ab ab', count=1))
print('split', compiled.split('ab ab'))
