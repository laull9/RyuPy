# ryupy: pending
# 后顾嵌套、前顾越过当前位置、外部固定宽引用、条件分支与原子组长尾。
import re
for pattern, subject in [
    (r'(?<=(?<=a)b)c', 'abc xbc'),
    (r'(?<=(?<!a)b)c', 'abc xbc'),
    (r'(?<=a(?=bc))b', 'abc abd'),
    (r'(?<=a(?!c))b', 'ab ac'),
    (r'(ab)(?<=\1)c', 'abc abx'),
    (r'(?P<x>ab)(?<=(?P=x))c', 'abc'),
    (r'(a)?(?<=(?(1)a|b))c', 'ac bc cc'),
    (r'(?<=(a|b))c\1', 'aca bcb acb'),
    (r'(?<=a{2}|b{2})c', 'aac bbc abc'),
    (r'(?<=(?>ab))c', 'abc'),
    (r'(?<=a{2}+)b', 'aab'),
    (r'(?<=(a))\1', 'aaa'),
    (r'(?<=a$)', 'a\n'),
]:
    compiled = re.compile(pattern)
    print(pattern, repr(subject), [(m.span(), m.groups(), m.regs) for m in compiled.finditer(subject)])
