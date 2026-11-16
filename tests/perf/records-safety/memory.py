# 产品AOT同源码off/on握手。released仅源码清根，不宣称GC已回收。
import sys

def increment(d, key):
    d[key] = d.get(key, 0) + 1

def create(size):
    roots = []
    for i in range(size):
        d = {}
        first = tuple(['ann', 'buy'])
        increment(d, first)
        increment(d, tuple(['ann', 'buy']))
        roots.append((d, first))
    return roots

def verify(roots, phase):
    total = 0
    for d, first in roots:
        if phase == 'held':
            assert next(iter(d)) is first
            assert d[first] == 2
            total += d[first]
        else:
            assert len(d) == 0
    return total

size = int(sys.argv[1])
phase = sys.argv[2]
for batch in range(4):
    roots = create(size)
    assert verify(roots, 'held') == size * 2
    roots = None
roots = create(size)
if phase == 'cleared':
    for item in roots:
        item[0].clear()
    item = None
if phase == 'released':
    assert verify(roots, 'held') == size * 2
    roots = None
print('READY')
assert input() == 'ACK'
if roots is None:
    print('DONE', size, phase, 0)
else:
    print('DONE', size, phase, verify(roots, phase))
