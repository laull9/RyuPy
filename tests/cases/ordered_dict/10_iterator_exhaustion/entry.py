# 无 next-key 时先结束：空游标、已yield尾键、已耗尽均不得追溯失效。
from collections import OrderedDict as OD


def source(d, kind):
    if kind == 'keys':
        return d.keys()
    if kind == 'values':
        return d.values()
    if kind == 'items':
        return d.items()
    return d


def step(it):
    try:
        return ('OK', next(it))
    except Exception as exc:
        return ('ERROR', type(exc).__name__, str(exc))

for kind in ('direct', 'keys', 'values', 'items'):
    for backward in (False, True):
        d = OD()
        view = source(d, kind)
        it = reversed(view) if backward else iter(view)
        d['new'] = 1
        first, second = step(it), step(it)
        print('initial_empty', kind, backward, first, second)
        assert first[1] == 'StopIteration' and second[1] == 'StopIteration'
        d = OD(a=1)
        view = source(d, kind)
        it = reversed(view) if backward else iter(view)
        print('last_yielded', kind, backward, step(it))
        d['b'] = 2
        d.move_to_end('a')
        first, second = step(it), step(it)
        print('no_next_key', first, second)
        assert first[1] == 'StopIteration' and second[1] == 'StopIteration'
        view = source(d, kind)
        it = reversed(view) if backward else iter(view)
        print('drain', list(it))
        d.clear()
        d['c'] = 3
        print('already_exhausted', step(it), step(it))
print('DONE iterator_exhaustion')
