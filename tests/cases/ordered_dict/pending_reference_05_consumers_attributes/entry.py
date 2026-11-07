# 补充：显式 dict 根方法与公开映射消费者的顺序不等价；实例属性不能污染隐式槽。
from collections import OrderedDict as OD, Counter, defaultdict
from types import MappingProxyType
import json


def collect(**kw):
    return list(kw.items())


d = OD(a=1, b=2, c=3)
d.move_to_end('a')
print('order_layers', list(d.items()), list(dict.items(d)))
print('dict_init', list(dict(d).items()))
print('dict_copy', list(dict.copy(d).items()))
e = {}
e.update(d)
print('dict_update', list(e.items()))
print('kwargs', collect(**d))
print('json', json.dumps(d))
print('counter', list(Counter(d).items()))
print('defaultdict', list(defaultdict(int, d).items()))
proxy = MappingProxyType(d)
print('mappingproxy', list(proxy.items()), list(proxy.copy().items()))
print('views_mapping', list(d.keys().mapping.items()))
copy = d.copy()
copy.move_to_end('b')
print('independent_copy', list(copy), list(d), copy is not d)
print('root_set_existing', dict.__setitem__(d, 'a', 10), list(d.items()))
print('root_get', dict.get(d, 'a'), dict.__getitem__(d, 'a'), dict.__len__(d))

saved = d.move_to_end
d.move_to_end = lambda *args: 'shadowed'
print('ordinary_method_shadow', d.move_to_end('b'))
print('saved_method', saved('b'), list(d))
d.__len__ = lambda: 999
d.__iter__ = lambda: iter(['wrong'])
d.__getitem__ = lambda key: 'wrong'
print('explicit_attributes', d.__len__(), list(d.__iter__()), d.__getitem__('a'))
print('implicit_slots', len(d), list(d), d['a'])
del d.move_to_end
print('method_restored', d.move_to_end.__self__ is d)
print('fromkeys_alias', (lambda x: x['a'] is x['b'])(OD.fromkeys(['a', 'b'], [])))
print('DONE 05_consumers_attributes')
