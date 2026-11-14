# 原生OD可被dict根写造成双层分离；本切允许NI，但不改变这里的CP正预期。
from collections import OrderedDict as OD


def attempt(label, fn):
    try:
        print(label, 'OK', repr(fn()))
    except Exception as exc:
        print(label, 'ERROR', type(exc).__name__, str(exc), exc.args)

for operation in ('set_existing', 'set_new', 'delete', 'update', 'clear', 'pop', 'popitem', 'setdefault', 'init'):
    d = OD(a=1, b=2, c=3)
    d.move_to_end('a')
    print('before', operation, list(d.items()), list(dict.items(d)))
    if operation == 'set_existing':
        result = dict.__setitem__(d, 'a', 10)
    elif operation == 'set_new':
        result = dict.__setitem__(d, 'raw', 9)
    elif operation == 'delete':
        result = dict.__delitem__(d, 'b')
    elif operation == 'update':
        result = dict.update(d, [('a', 10), ('raw', 9)])
    elif operation == 'clear':
        result = dict.clear(d)
    elif operation == 'pop':
        result = dict.pop(d, 'b')
    elif operation == 'popitem':
        result = dict.popitem(d)
    elif operation == 'setdefault':
        result = dict.setdefault(d, 'raw', 9)
    else:
        result = dict.__init__(d, [('a', 10), ('raw', 9)])
    print('root_result', result, len(d), list(dict.items(d)))
    attempt('public_keys', lambda: list(d.keys()))
    attempt('public_items', lambda: list(d.items()))
    attempt('public_repr', lambda: repr(d))
print('DONE pending_dict_root_mutations')
