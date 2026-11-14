# 真实receiver、继承/本类描述符元数据、全部核心方法签名与关键字支持。
from collections import OrderedDict as OD


def attempt(label, fn):
    try:
        print(label, 'OK', repr(fn()))
    except Exception as exc:
        print(label, 'ERROR', type(exc).__name__, str(exc), exc.args)

d = OD(a=1, b=2)
print('type_metadata', OD.__base__ is dict, OD.__class__ is type, d.__class__ is OD)
for name in ('__init__', '__len__', '__iter__', '__reversed__', '__getitem__', '__setitem__', '__delitem__', '__contains__', '__eq__', '__ne__', 'get', 'pop', 'setdefault', 'update', 'clear', 'copy', 'keys', 'values', 'items', 'move_to_end', 'popitem', 'fromkeys'):
    descriptor = getattr(OD, name)
    bound = getattr(d, name)
    print('metadata', name, descriptor.__name__, descriptor.__qualname__, getattr(descriptor, '__objclass__', None), bound.__name__, bound.__qualname__, bound.__self__ is d, bound.__self__ is OD)
for label, fn in (
    ('construct_many', lambda: OD([], [])),
    ('init_many', lambda: d.__init__([], [])),
    ('update_many', lambda: d.update([], [])),
    ('move_missing', lambda: d.move_to_end()),
    ('move_many', lambda: d.move_to_end('a', True, False)),
    ('move_badkw', lambda: d.move_to_end('a', other=1)),
    ('move_duplicate', lambda: d.move_to_end('a', key='a')),
    ('move_missing_badkw', lambda: d.move_to_end(other=1)),
    ('popitem_many', lambda: d.popitem(True, False)),
    ('popitem_badkw', lambda: d.popitem(other=1)),
    ('popitem_duplicate', lambda: d.popitem(True, last=False)),
    ('get_missing', lambda: d.get()),
    ('get_many', lambda: d.get('a', 1, 2)),
    ('get_keyword', lambda: d.get(key='a')),
    ('pop_missing', lambda: d.pop()),
    ('pop_many', lambda: d.pop('a', 1, 2)),
    ('pop_badkw', lambda: d.pop('a', other=1)),
    ('setdefault_missing', lambda: d.setdefault()),
    ('setdefault_many', lambda: d.setdefault('a', 1, 2)),
    ('keys_many', lambda: d.keys(1)),
    ('values_keyword', lambda: d.values(other=1)),
    ('items_many', lambda: d.items(1)),
    ('clear_many', lambda: d.clear(1)),
    ('copy_keyword', lambda: d.copy(other=1)),
    ('fromkeys_missing', lambda: OD.fromkeys()),
    ('fromkeys_many', lambda: OD.fromkeys([], 1, 2)),
    ('fromkeys_badkw', lambda: OD.fromkeys([], other=1)),
    ('len_many', lambda: d.__len__(1)),
    ('item_missing', lambda: d.__getitem__()),
    ('item_keyword', lambda: d.__getitem__(key='a')),
    ('set_missing', lambda: d.__setitem__('a')),
    ('del_missing', lambda: d.__delitem__()),
    ('contains_missing', lambda: d.__contains__()),
    ('eq_missing', lambda: d.__eq__()),
    ('ne_many', lambda: d.__ne__(d, d)),
    ('iter_many', lambda: d.__iter__(1)),
    ('reversed_many', lambda: d.__reversed__(1)),
    ('root_move_receiver', lambda: OD.move_to_end({}, 'a')),
    ('root_pop_receiver', lambda: OD.pop({}, 'a')),
    ('root_keys_receiver', lambda: OD.keys({})),
    ('root_set_receiver', lambda: OD.__setitem__({}, 'a', 1)),
):
    attempt(label, fn)
print('pop_keywords', OD(a=1).pop(key='a', default=8))
print('setdefault_keywords', OD().setdefault(key='x', default=7))
print('fromkeys_keywords', list(OD.fromkeys(iterable=['a', 'b'], value=9).items()))
print('update_keywords_are_data', list(OD(self=1, iterable=2).items()))
print('unchanged', list(d.items()))
print('DONE binding_metadata_errors')
