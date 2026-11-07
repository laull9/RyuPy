# 常用映射API与空/非空错误顺序；默认值不复制，明确保留 KeyError 参数。
from collections import OrderedDict as OD


def attempt(label, fn):
    try:
        print(label, 'OK', repr(fn()))
    except Exception as exc:
        print(label, 'ERROR', type(exc).__name__, str(exc), exc.args)

shared = []
d = OD(a=1, b=2)
view = d.items()
print('lookup', d['a'], d.get('none'), d.get('none', shared) is shared, 'a' in d, 'none' not in d, len(d), bool(d))
print('setdefault_existing', d.setdefault('a', shared), list(d))
print('setdefault_new', d.setdefault('c', shared) is shared, d['c'] is shared)
print('pop', d.pop('b'), d.pop('missing', shared) is shared, list(view))
attempt('missing_pop', lambda: d.pop('missing'))
attempt('missing_item', lambda: d['missing'])
attempt('missing_delete', lambda: d.__delitem__('missing'))
print('clear', d.clear(), list(view), len(d), bool(d))
d['after'] = shared
print('same_view_after_clear', list(view), d['after'] is shared)
for filled in (False, True):
    obj = OD(a=1) if filled else OD()
    for name, fn in (
        ('item', lambda: obj[[]]),
        ('get', lambda: obj.get([], 'default')),
        ('contains', lambda: [] in obj),
        ('pop', lambda: obj.pop([], 'default')),
        ('setdefault', lambda: obj.setdefault([], 2)),
        ('move', lambda: obj.move_to_end([])),
        ('delete', lambda: obj.__delitem__([])),
    ):
        attempt(str(filled) + '_' + name, fn)
print('DONE mapping_methods')
