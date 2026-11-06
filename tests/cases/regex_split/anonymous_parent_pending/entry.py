# 独立pending：匿名父view依赖CP引用计数即时回收，CLR GC不承诺此时序（D001）。
import re

for kind in ('slice', 'char'):
    source = bytearray(b'a,b,c!')
    if kind == 'slice':
        subject = memoryview(source)[0:5]
    else:
        subject = memoryview(source).cast('c')
    expected = [b'a', b',', None, b'b', b',', None, b'c' if kind == 'slice' else b'c!']
    pieces = re.split(b'(,)(x)?', subject)
    assert pieces == expected
    source[0] = 90
    assert pieces == expected and pieces[0] is not subject
    print('snapshot-' + kind, pieces, [type(x).__name__ for x in pieces])
    subject.release()
    source.extend(b'?')
    print('resize-' + kind, bytes(source))
