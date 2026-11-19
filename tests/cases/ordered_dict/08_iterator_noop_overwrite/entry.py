# overwrite 与端点 no-op 不失效；每个游标独立，values/items 读取活值。
from collections import OrderedDict as OD


def step(it):
    try:
        return ('OK', next(it))
    except Exception as exc:
        return ('ERROR', type(exc).__name__, str(exc))

for kind in ('direct', 'keys', 'values', 'items'):
    for backward in (False, True):
        d = OD(a=1, b=2, c=3)
        source = d
        if kind == 'keys':
            source = d.keys()
        elif kind == 'values':
            source = d.values()
        elif kind == 'items':
            source = d.items()
        it = reversed(source) if backward else iter(source)
        print('start', kind, backward, type(it).__name__, iter(it) is it, step(it))
        d['b'] = 20
        d.move_to_end('c')
        d.move_to_end('a', False)
        print('continue', step(it), step(it), step(it), step(it))
d = OD(a=1, b=2)
x, y = iter(d), iter(d)
print('independent', next(x), next(x), next(y), next(y))
print('DONE iterator_noop_overwrite')
