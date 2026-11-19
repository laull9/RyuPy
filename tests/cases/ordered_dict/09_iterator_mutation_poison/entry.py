# 尚有 next-key 的游标遇结构修改首次 RuntimeError，随后永久 StopIteration。
from collections import OrderedDict as OD


def step(it):
    try:
        return ('OK', next(it))
    except Exception as exc:
        return ('ERROR', type(exc).__name__, str(exc))

for operation in ('move', 'insert', 'delete', 'clear', 'replace_same_size', 'delete_readd', 'move_restore'):
    for backward in (False, True):
        d = OD(a=1, b=2, c=3)
        it = reversed(d.items()) if backward else iter(d.items())
        print('first', operation, backward, step(it))
        if operation == 'move':
            d.move_to_end('a')
        elif operation == 'insert':
            d['new'] = 4
        elif operation == 'delete':
            del d['b']
        elif operation == 'clear':
            d.clear()
        elif operation == 'replace_same_size':
            del d['b']
            d['new'] = 4
        elif operation == 'delete_readd':
            del d['b']
            d['b'] = 2
        else:
            d.move_to_end('a')
            d.move_to_end('a', False)
        first = step(it)
        second = step(it)
        d['after_error'] = 5
        third = step(it)
        print('after', list(d), first, second, third)
        assert first[1] == 'RuntimeError'
        assert second[1] == 'StopIteration' and third[1] == 'StopIteration'
print('DONE iterator_mutation_poison')
