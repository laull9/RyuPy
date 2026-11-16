# ryupy: pending
# find_spec 合同：断言保留 CPython 行为，不用显式边界改写 CP 结果。
import sys
import importlib.util as util

def check(label, value):
    assert value, label
    print(label, value)

def error(label, action, expected, message=None):
    try:
        action()
    except Exception as exc:
        assert type(exc) is expected, (label, type(exc))
        if message is not None:
            assert str(exc) == message, (label, str(exc))
        print(label, type(exc).__name__, str(exc))
        return exc
    raise AssertionError(label)

from importlib.machinery import ModuleSpec
events = []
chosen = ModuleSpec('virtual_find_928471', None)
class First:
    def find_spec(self, fullname, path, target=None):
        events.append(('first', fullname, path, target))
        return None
class Second:
    def find_spec(self, fullname, path, target=None):
        events.append(('second', fullname, path, target))
        if fullname == 'virtual_broken_928471':
            raise RuntimeError('finder failed')
        if fullname == chosen.name:
            return chosen
first, second = First(), Second()
sys.meta_path.insert(0, second)
sys.meta_path.insert(0, first)
try:
    check('finder_return_identity', util.find_spec(chosen.name) is chosen)
    check('finder_order_and_arguments', events == [('first', chosen.name, None, None), ('second', chosen.name, None, None)])
    check('finder_does_not_cache', chosen.name not in sys.modules)
    error('finder_error_propagates', lambda: util.find_spec('virtual_broken_928471'), RuntimeError, 'finder failed')
    check('finder_error_not_cached', 'virtual_broken_928471' not in sys.modules)
    class Holder:
        pass
    holder = Holder()
    marker = []
    holder.__spec__ = marker
    sys.modules[chosen.name] = holder
    before = len(events)
    check('cache_bypasses_hooks', util.find_spec(chosen.name) is marker and len(events) == before)
    sys.modules[chosen.name] = None
    check('none_bypasses_hooks', util.find_spec(chosen.name) is None and len(events) == before)
finally:
    sys.meta_path.remove(first)
    sys.meta_path.remove(second)
    sys.modules.pop(chosen.name, None)
