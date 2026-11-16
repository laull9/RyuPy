# ADR0106 精确标量键的相等折叠、首键身份及大整数区分。
from collections import OrderedDict as OD

shared = []
d = OD([(True, 'bool'), (1, 'int'), (1.0, 'float'), (None, shared), ('1', 'str'), (False, 'false'), (0.0, 'zero'), (-2, 'neg'), (2.5, 'fraction')])
print('folded', list(d.items()), len(d), next(iter(d)) is True)
assert list(d) == [True, None, '1', False, -2, 2.5]
assert next(iter(d)) is True and d[1] == 'float' and d[0] == 'zero'
d.move_to_end(1.0)
print('equal_move', list(d), list(d)[-1] is True)
del d[1]
d[1.0] = 'new'
print('reinsert_type', type(list(d)[-1]).__name__, list(d.items()))
large = 9007199254740993
near = 9007199254740992.0
big = 1267650600228229401496703205376
n = OD([(large, 'integer'), (near, 'float'), (big, 'big'), (None, shared)])
print('large', list(n.items()), n[large], n[near], n[big], n[None] is shared)
assert len(n) == 4 and n[large] != n[near]
first = ''.join(['long-', 'key'])
equal = ''.join(['long-', 'key'])
s = OD([(first, 1), (equal, 2)])
print('string_key', next(iter(s)) is first, list(s.items()))
assert next(iter(s)) is first
print('DONE scalar_keys')
