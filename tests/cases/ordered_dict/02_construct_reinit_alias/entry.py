# 构造与重复初始化均按流更新；旧绑定、视图和共享值仍连接原对象。
from collections import OrderedDict as OD

shared = []
d = OD([('b', shared), ('a', 2), ('b', shared)], c=3, a=20)
alias = d
items = d.items()
keys = d.keys()
write = d.__setitem__
update = d.update
move = d.move_to_end
initialize = d.__init__
print('initial', list(d.items()), move.__self__ is d, write.__self__ is d)
assert list(d) == ['b', 'a', 'c']
write('b', shared)
move('b')
print('moved', list(keys), list(items), alias is d, d['b'] is shared)
assert initialize([('z', shared), ('a', 21)], end=9) is None
print('reinit', list(d.items()), list(items), alias is d)
assert initialize() is None
update(d)
print('self_update', list(d.items()), len(d))
del d['c']
write('c', 30)
shared.append('shared')
print('reinsert', list(d.items()), d['b'] is d['z'], alias is d)
assert list(d) == ['a', 'b', 'z', 'end', 'c']
print('keyword_data', list(OD(iterable=1, self=2).items()))
print('DONE construct_reinit_alias')
