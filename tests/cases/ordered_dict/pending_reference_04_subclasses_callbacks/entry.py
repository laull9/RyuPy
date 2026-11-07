# CPython 3.12.13 派生与回调合同；用于规划边界，不声称 RyuPy 已支持这些能力。
from collections import OrderedDict as OD
import copy


class Child(OD):
    def __init__(self, *args, **kwargs):
        print('child_init', len(args), list(kwargs))
        self.tag = []
        super().__init__(*args, **kwargs)
    def __setitem__(self, key, value):
        print('child_set', key, value)
        super().__setitem__(key, value)
    def __delitem__(self, key):
        print('child_del', key)
        super().__delitem__(key)
    def __missing__(self, key):
        print('child_missing', key)
        return 'missing:' + key


c = Child([('a', 1), ('b', 2)], z=3)
print('child_type', type(c) is Child, isinstance(c, OD), isinstance(c, dict), repr(c))
c.update([('b', 20), ('d', 4)])
c.setdefault('e', 5)
print('child_pop', c.pop('a'))
print('child_popitem', c.popitem(False))
c.move_to_end('d')
print('child_after', list(c.items()))
print('child_missing_result', c['absent'], c.get('absent'), 'absent' in c)
for label, fn in (('method_copy', c.copy), ('copy_copy', lambda: copy.copy(c)), ('fromkeys', lambda: Child.fromkeys(['x', 'y'], 8)), ('union', lambda: c | {'x': 9}), ('reflected_union', lambda: {'x': 9} | c)):
    result = fn()
    print(label, type(result) is Child, list(result.items()), hasattr(result, 'tag'), result.tag is c.tag)
print('direct_root_set')
OD.__setitem__(c, 'root', 7)
print('direct_root_update')
OD.update(c, [('root_update', 8)])
print('direct_dict_set')
dict.__setitem__(c, 'raw', 9)
print('raw_observation', len(c), 'raw' in c, dict.__getitem__(c, 'raw'), list(dict.keys(c)), list(c.keys()))


class Source(OD):
    def keys(self):
        print('source_keys')
        return ['b', 'a']
    def __iter__(self):
        print('source_iter')
        return iter(['a', 'b'])
    def __getitem__(self, key):
        print('source_get', key)
        return super().__getitem__(key) * 10


source = Source(a=1, b=2)
print('source_to_od', list(OD(source).items()))
print('source_to_dict', list(dict(source).items()))


class Value:
    def __init__(self, name):
        self.name = name
    def __eq__(self, other):
        print('value_eq', self.name, other.name)
        return True


left = OD(a=Value('la'), b=Value('lb'))
right = OD(b=Value('rb'), a=Value('ra'))
print('mapping_before_order', left == right)
shared = Value('shared')
print('identity_shortcut', OD(a=shared) == OD(a=shared))


class Foreign:
    def __eq__(self, other):
        print('foreign_eq')
        return 'eq-result'
    def __ne__(self, other):
        print('foreign_ne')
        return 'ne-result'


print('foreign_expression_eq', OD() == Foreign())
print('foreign_expression_ne', OD() != Foreign())
print('DONE 04_subclasses_callbacks')
