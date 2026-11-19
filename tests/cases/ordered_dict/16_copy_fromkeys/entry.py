# 浅copy的顺序独立与值共享，fromkeys重复键和默认值身份。
from collections import OrderedDict as OD

shared = []
d = OD(a=shared, b=2, c=shared)
d.move_to_end('a')
copy = d.copy()
print('copy_initial', type(copy) is OD, copy is not d, copy == d, list(copy), copy['a'] is shared)
copy.move_to_end('b')
copy['new'] = 9
shared.append('visible')
print('copy_independent', list(copy.items()), list(d.items()), copy['a'] is d['c'])
assert list(copy) == ['c', 'a', 'b', 'new'] and list(d) == ['b', 'c', 'a']
f = OD.fromkeys(['b', 'a', 'b'], shared)
print('fromkeys', type(f) is OD, list(f.items()), f['a'] is f['b'], f['a'] is shared)
print('fromkeys_default', list(OD.fromkeys(['z', 'z', 'x']).items()))
print('fromkeys_alias', list(d.fromkeys(['x', 'y'], shared).items()), list(d))
print('numeric_fromkeys', list(OD.fromkeys([True, 1.0, 0, False, None], shared).items()))
class Keys:
    def __iter__(self):
        print('fromkeys_yield_a')
        yield 'a'
        print('fromkeys_yield_b')
        yield 'b'
print('streaming_fromkeys', list(OD.fromkeys(Keys(), 7).items()))
print('DONE copy_fromkeys')
