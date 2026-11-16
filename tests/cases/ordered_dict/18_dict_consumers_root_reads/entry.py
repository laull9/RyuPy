# 双序：dict显式根视图为raw插入序，公开转换/复制/update/**为OD序。
from collections import OrderedDict as OD


def collect(**kw):
    return list(kw.items())

shared = []
d = OD(a=shared, b=2, c=3)
raw_keys = dict.keys(d)
raw_values = dict.values(d)
raw_items = dict.items(d)
raw_iterator = dict.__iter__(d)
raw_reverse = dict.__reversed__(d)
public_keys = d.keys()
d.move_to_end('a')
print('layers', list(public_keys), list(raw_keys), list(raw_values), list(raw_items))
print('raw_cursors', list(raw_iterator), list(raw_reverse))
print('raw_view_types', type(raw_keys).__name__, type(raw_values).__name__, type(raw_items).__name__)
print('raw_reverse_views', list(reversed(raw_keys)), list(reversed(raw_values)), list(reversed(raw_items)))
print('root_read', dict.__len__(d), dict.get(d, 'a') is shared, dict.__getitem__(d, 'a') is shared, dict.__contains__(d, 'a'), dict.get(d, 'missing', 8))
converted = dict(d)
copied = dict.copy(d)
target = {'z': 0}
target.update(d)
print('dict_constructor', type(converted) is dict, list(converted.items()), converted['a'] is shared)
print('dict_copy', type(copied) is dict, list(copied.items()), copied['a'] is shared)
print('dict_update', list(target.items()))
print('kwargs', collect(**d))
assert list(converted) == ['b', 'c', 'a'] and list(raw_keys) == ['a', 'b', 'c']
d['b'] = 20
del d['c']
d['c'] = 30
print('live_layers', list(raw_items), list(d.items()))
left, right = OD(a=1, b=2), OD(b=2, a=1)
print('root_equality', dict.__eq__(left, right), dict.__ne__(left, right), left == right, left != right)
print('root_foreign', dict.__eq__(left, []) is NotImplemented, dict.__ne__(left, []) is NotImplemented)
print('DONE dict_consumers_root_reads')
