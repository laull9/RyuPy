# CPython 3.12.13 OrderedDict 活视图、正反游标与失效时点；每次异常后的游标继续观测。
from collections import OrderedDict as OD


def step(it):
    try:
        return ('OK', next(it))
    except Exception as exc:
        return ('ERROR', type(exc).__name__, str(exc))


def trial(label, operation, factory=iter):
    d = OD(a=1, b=2, c=3)
    it = factory(d)
    print(label, 'type', type(it).__name__, iter(it) is it, 'first', step(it))
    operation(d)
    print(label, 'state', list(d.items()), 'rest', step(it), step(it), step(it), step(it))


trial('overwrite', lambda d: d.__setitem__('b', 20), lambda d: iter(d.items()))
trial('move_tail_noop', lambda d: d.move_to_end('c'))
trial('move_head_noop', lambda d: d.move_to_end('a', False))
trial('move_change', lambda d: d.move_to_end('a'))
trial('insert', lambda d: d.__setitem__('d', 4))
trial('delete', lambda d: d.__delitem__('b'))
trial('clear', lambda d: d.clear())
trial('delete_add_same_size', lambda d: (d.__delitem__('b'), d.__setitem__('d', 4)))
trial('delete_readd_same_key', lambda d: (d.__delitem__('b'), d.__setitem__('b', 2)))
trial('move_and_restore', lambda d: (d.move_to_end('a'), d.move_to_end('a', False)))
trial('reverse_items', lambda d: d.__setitem__('b', 20), lambda d: reversed(d.items()))
trial('reverse_move', lambda d: d.move_to_end('a'), reversed)
for factory in (iter, reversed):
    d = OD(a=1)
    it = factory(d)
    print('last_yielded', factory.__name__, step(it))
    d['b'] = 2
    print('before_exhaustion', factory.__name__, step(it), step(it))
    it = factory(d)
    list(it)
    d['c'] = 3
    print('exhausted_then_add', factory.__name__, step(it), step(it))
    empty = OD()
    cursor = factory(empty)
    empty['new'] = 1
    print('empty_not_started_then_add', factory.__name__, step(cursor), step(cursor))
d = OD(a=1, b=2)
keys, values, items = d.keys(), d.values(), d.items()
d.move_to_end('a')
d['b'] = 20
print('live_views', list(keys), list(values), list(items))
print('view_mapping', keys.mapping is keys.mapping, type(keys.mapping).__name__, list(keys.mapping))
print('DONE 02_iterators')
