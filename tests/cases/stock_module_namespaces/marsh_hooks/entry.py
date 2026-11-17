# ryupy: pending
# 不同布局的相对from-import与替换builtins映射；值可以不是模块，逐个属性副作用可观察。
import builtins
import sys
import types
import fenland.worker as worker
original = builtins.__import__
trace = []
value = object()
payload = types.ModuleType('renamed_cache')
sys.modules['renamed_cache.finish'] = value
boom = ValueError('attribute callback')
def read(name):
    trace.append(('attribute', name))
    if name == 'start':
        return 8
    if name == 'explode':
        raise boom
    raise AttributeError(name)
payload.__getattr__ = read
def route(name, g=None, l=None, fromlist=(), level=0):
    trace.append(('hook', name, g['__name__'], l is None, fromlist, level))
    return payload
try:
    builtins.__import__ = route
    assert worker.relative() is value
    from opaque_bridge import start, finish
    assert (start, finish) == (8, value)
    start = 0
    try:
        from opaque_bridge import start, explode
    except ValueError as error:
        assert error is boom
    assert start == 8
    payload.__file__ = '/fixed/source.py'
    try:
        from opaque_bridge import missing
    except ImportError as error:
        print(type(error).__name__, str(error), error.name, error.path)
    del payload.__file__
    payload.__spec__ = types.SimpleNamespace(_initializing=True)
    # 无文件位置时不应出现partial-initialization文案。
    try:
        from opaque_bridge import missing
    except ImportError as error:
        print(str(error), error.name, error.path)
finally:
    builtins.__import__ = original
    del sys.modules['renamed_cache.finish']
print(trace)
