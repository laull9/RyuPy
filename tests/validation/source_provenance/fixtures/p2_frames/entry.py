import warnings
from functools import partial
import a
import b

print('static-candidate', a.static_candidate(2))
a.__name__ = '__main__'
b.__name__ = 'hidden-b'
a.emit.__module__ = 'reflection-emitter'
a.stream.__module__ = 'reflection-generator'
b.decorated.__module__ = 'reflection-wrapper'
closure = a.make()
closure.__module__ = 'reflection-closure'
with warnings.catch_warnings(record=True) as records:
    print('direct', a.emit(1, 'level-1'))
    print('bridge-2', b.bridge(a.emit, 2, 'level-2'))
    print('bridge-3', b.bridge(a.emit, 3, 'level-3'))
    print('level-0', a.emit(0, 'level-0'))
    print('overrun', a.emit(100, 'overrun', UserWarning))
    print('decorated', b.decorated())
    print('closure', closure())
    print('partial', partial(a.emit, 1, 'partial')())
    print('native', a.native())
    generator = a.stream()
    print('generator-first', next(generator))
    a.__name__ = 'hidden-a'
    print('generator-second', next(generator))
    generator.close()
    print('record-count', len(records))
    for record in records:
        print('record', str(record.message), record.filename, record.lineno)
print('done')
