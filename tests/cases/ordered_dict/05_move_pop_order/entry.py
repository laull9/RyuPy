# 两端重排和弹出，last 接收真实真值；保存方法仍作用于同一对象。
from collections import OrderedDict as OD

for last in (True, False, 1, 0, None, [], [1], '', 'tail'):
    d = OD(a=1, b=2, c=3)
    move = d.move_to_end
    pop = d.popitem
    print('move', repr(last), move('b', last), list(d.items()))
    print('pop', repr(last), pop(last), list(d.items()), pop.__self__ is d)
d = OD(a=1, b=2, c=3)
print('kw_move', d.move_to_end(key='a', last=True), list(d))
print('head', d.move_to_end(key='c', last=False), list(d))
print('pop_kw', d.popitem(last=False), list(d))
print('pop_default', d.popitem(), d.popitem(), bool(d))
try:
    d.popitem()
except KeyError as exc:
    print('empty', str(exc), exc.args)
else:
    raise AssertionError('空 popitem 必须 KeyError')
print('DONE move_pop_order')
