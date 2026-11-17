# ryupy: pending
# 任意hook返回值、dotted-as、整批fromlist、缓存回退及失败前缀都保持CP协议。
import builtins
import sys
import types
import courier_box
saved = builtins.__import__
events = []
marker = object()
virtual = types.ModuleType('cache_lane')
virtual.first = marker
nested = types.SimpleNamespace(twig=types.SimpleNamespace(leaf=marker))
sys.modules['cache_lane.cached'] = marker
sys.modules['cache_lane.empty'] = None
failure = LookupError('getter identity')
def getter(name):
    events.append(('get', name))
    if name == 'bad':
        raise failure
    raise AttributeError(name)
virtual.__getattr__ = getter
def replacement(name, g=None, l=None, fromlist=(), level=0):
    events.append(('second', name))
    return 29
def importer(name, g=None, l=None, fromlist=(), level=0):
    events.append(('call', name, g is globals(), l is g, l is None, fromlist, level))
    if name == 'branch_tree.twig.leaf':
        return nested
    if name == 'flip_switch':
        builtins.__import__ = replacement
        return 17
    if name in ('parcel_box', 'relative_leaf'):
        return virtual
    if name == 'numeric_result':
        return 41
    return saved(name, g, l, fromlist, level)
try:
    builtins.__import__ = importer
    import numeric_result as number
    assert number == 41
    import branch_tree.twig.leaf as leaf
    # hook返回根对象；IMPORT_FROM逐层消耗真实返回值。
    assert leaf is marker
    from parcel_box import first, cached, empty
    assert first is marker and cached is marker and empty is None
    first = 'before'
    try:
        from parcel_box import first, absent
    except ImportError as error:
        print(type(error).__name__, str(error), error.name, error.path)
    assert first is marker
    try:
        from parcel_box import bad
    except LookupError as error:
        assert error is failure
    assert courier_box.fetch() is marker
    import flip_switch as before
    import flip_switch as after
    assert (before, after) == (17, 29)
    builtins.__import__ = importer
    del builtins.__import__
    try:
        import numeric_result
    except ImportError as error:
        print(type(error).__name__, str(error))
    builtins.__import__ = None
    try:
        import numeric_result
    except TypeError as error:
        print(type(error).__name__, str(error))
finally:
    builtins.__import__ = saved
    del sys.modules['cache_lane.cached']
    del sys.modules['cache_lane.empty']
print(events)
