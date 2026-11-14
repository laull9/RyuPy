# CPython 3.12.13 OrderedDict 类型、顺序、值别名、复制与比较的完整参考程序。
from collections import OrderedDict as OD
from collections import OrderedDict
from collections.abc import Mapping, MutableMapping


def attempt(label, fn):
    try:
        result = fn()
        print(label, 'OK', repr(result))
    except Exception as exc:
        print(label, 'ERROR', type(exc).__name__, str(exc), repr(exc.args))


print('type', OD is OrderedDict, OD is not dict, repr(OD), OD.__module__, OD.__name__)
print('ancestry', OD.__bases__, OD.__mro__, issubclass(OD, dict), isinstance(OD(), dict))
print('abc', isinstance(OD(), Mapping), isinstance(OD(), MutableMapping))
shared = []
d = OD([('b', shared), ('a', 2), ('b', shared)], c=3, a=20)
alias = d
view = d.items()
bound = d.move_to_end
print('initial', repr(d), list(d), list(d.items()), bound.__self__ is d)
print('numeric_keys', list(OD([(True, 'true'), (1, 'int'), (1.0, 'float'), (None, 'none')]).items()))
d['b'] = shared
print('overwrite_order', list(d))
bound('b')
print('moved', list(d), list(view), alias is d, d['b'] is shared)
d.__init__([('z', shared), ('a', 21)], end=9)
print('reinit', list(d.items()), list(view), alias is d)
del d['c']
d['c'] = 30
print('reinsert', list(d.items()))
print('reverse', list(reversed(d)), list(reversed(d.keys())), list(reversed(d.values())), list(reversed(d.items())))
print('views', type(d.keys()).__name__, type(d.values()).__name__, type(d.items()).__name__)
left = OD([('a', 1), ('b', 2)])
right = OD([('b', 2), ('a', 1)])
plain = {'b': 2, 'a': 1}
print('equality', left == right, left != right, left == plain, plain == left, left.__eq__(plain), left.__ne__(plain))
print('foreign_eq', left.__eq__([]), left.__ne__([]), left == [], left != [])
print('view_equality', left.keys() == right.keys(), left.items() == right.items(), left.values() == left.values())
v = left.values()
print('same_values_view', v == v)
attempt('ordering', lambda: left < right)
attempt('hash', lambda: hash(d))
c = d.copy()
print('copy', type(c) is OD, c is not d, c == d, c['b'] is shared)
c.move_to_end('a', False)
print('copy_independent', list(c), list(d))
d.tag = shared
print('attributes', d.tag is shared, d.__dict__, hasattr(d.copy(), 'tag'))
f = OD.fromkeys(['b', 'a', 'b'], shared)
print('fromkeys', list(f), f['a'] is shared, f['a'] is f['b'])
print('dict_conversion', type(dict(d)) is dict, list(dict(d)), dict(d)['b'] is shared)
merged = left | {'a': 8, 'c': 3}
reflected = {'z': 0, 'a': 7} | left
print('union', type(merged) is OD, list(merged.items()), type(reflected) is OD, list(reflected.items()))
before = left
left |= [('b', 9), ('d', 4)]
print('iunion', left is before, list(left.items()))
print('alias_origin', OD[str, int].__origin__ is OD, OD[str, int].__args__, type(OD[str, int]()) is OD)
recursive = OD()
recursive['self'] = recursive
print('recursive_repr', repr(recursive))
print('root_dict_observation', list(dict.keys(d)), list(d.keys()), dict.__eq__(OD(a=1, b=2), OD(b=2, a=1)))
print('DONE 01_core')
