# CP3.12表示格式、重排、共享值与自环防护，所有输出不含地址。
from collections import OrderedDict as OD

print('empty', repr(OD()), str(OD()))
d = OD([('quoted', "a'b"), (None, 1), (True, [2, 3]), (2.5, 'value')])
print('filled', repr(d), str(d))
d.move_to_end('quoted')
print('moved', repr(d))
recursive = OD()
recursive['self'] = recursive
print('self', repr(recursive), str(recursive))
assert repr(recursive) == "OrderedDict({'self': ...})"
shared = []
outer = OD(a=shared, b=shared)
shared.append(outer)
print('nested_cycle', repr(outer))
print('repeat_repr', repr(recursive), repr(OD(ok=1)))
print('DONE repr_cycles')
