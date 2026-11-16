# 仅CP控制：exec真实X字节进入Y字典，非Ryu动态exec支持承诺。
from pathlib import Path
import warnings

root = Path(__file__).resolve().parent
code_x = compile((root / 'source_x.py').read_bytes(), str(root / 'source_x.py'), 'exec')
code_y = compile((root / 'source_y.py').read_bytes(), str(root / 'source_y.py'), 'exec')
print('host-registered')
events = []
missing = object()

class EntryMarker:
    def __init__(self, namespace, value):
        self.namespace = namespace
        self.value = value
    def __bool__(self):
        events.append('entry-bool')
        if self.value is missing:
            del self.namespace['__name__']
        else:
            self.namespace['__name__'] = self.value
        raise AttributeError('entry-attribute')

for label, value in (('text', 'entry-fallback'), ('empty', ''), ('zero', 0),
                     ('list', []), ('missing', missing), ('none-control', None)):
    ns = {'__name__': 'source_y'}
    exec(code_y, ns)
    events.clear()
    ns['__name__'] = EntryMarker(ns, value)
    try:
        exec(code_x, ns)
        actual = ns['Top'].__module__
        print('entry-fallback', label, 'ok', type(actual).__name__, repr(actual), actual is value, events)
    except Exception as error:
        print('entry-fallback', label, 'error', type(error).__name__, str(error), events)

ns = {'__name__': 'entry-name'}
exec(code_x, ns)
old = ns['emit']
old.__module__ = 'old-module'
with warnings.catch_warnings(record=True) as records:
    warnings.simplefilter('module')
    print('xy-top', ns['source_marker'], ns['Top'].__module__)
    print('xy-old', old())
    old.__module__ = ns['NativeMarker']()
    try:
        old()
    except ValueError as error:
        trace = error.__traceback__
        found = False
        while trace is not None:
            if trace.tb_frame.f_code.co_name == '__bool__':
                found = True
            trace = trace.tb_next
        print('native-truthy', 'ValueError', found)
    old.__module__ = 'old-module'
    print('native-recovery', old())
    ns['__name__'] = 'reentry-name'
    exec(code_x, ns)
    new = ns['emit']
    new.__module__ = 'new-module'
    print('xy-functions', old(), new(), old.__globals__ is ns, new.__globals__ is ns)
    exec(code_y, ns)
    print('xy-y', ns['emit_y']())
    print('xy-count', len(records))
exec(code_y, ns)
print('xy-default-code', ns['source_marker'])
print('XY done')
