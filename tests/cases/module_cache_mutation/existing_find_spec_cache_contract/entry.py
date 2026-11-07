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

import fs_cached
original = fs_cached.__spec__
check('cached_original', util.find_spec('fs_cached') is original)
original.name = 'mutable.name'
check('cached_no_revalidation', util.find_spec('fs_cached') is original)
marker = []
fs_cached.__spec__ = marker
check('arbitrary_spec_identity', util.find_spec(name='fs_cached') is marker)
marker.append('visible')
check('arbitrary_spec_alias', util.find_spec('fs_cached') == ['visible'])
for value in (False, 0, '', (), {}):
    fs_cached.__spec__ = value
    check('false_spec_is_not_none', util.find_spec('fs_cached') is value)
fs_cached.__spec__ = None
error('none_spec', lambda: util.find_spec('fs_cached'), ValueError, 'fs_cached.__spec__ is None')
del fs_cached.__spec__
exc = error('missing_spec', lambda: util.find_spec('fs_cached'), ValueError, 'fs_cached.__spec__ is not set')
check('missing_spec_no_explicit_cause', exc.__cause__ is None)
fs_cached.__spec__ = original
sys.modules['fs_cached'] = None
check('cached_none', util.find_spec('fs_cached') is None)
error('none_blocks_import', lambda: __import__('fs_cached'), ModuleNotFoundError, 'import of fs_cached halted; None in sys.modules')
sys.modules['fs_cached'] = fs_cached
# 缓存键可以不属于已编译域；命中不查找父包，也不要求缓存对象是模块。
class Holder:
    pass
holder = Holder()
holder.__spec__ = marker
sys.modules['unregistered_parent.child'] = holder
check('cache_precedes_parent', util.find_spec('unregistered_parent.child') is marker and 'unregistered_parent' not in sys.modules)
holder.__spec__ = None
error('relative_error_uses_input', lambda: util.find_spec('.child', 'unregistered_parent'), ValueError, '.child.__spec__ is None')
sys.modules['unregistered_parent.child'] = None
check('relative_none_cache', util.find_spec('.child', package='unregistered_parent') is None)
del sys.modules['unregistered_parent.child']
check('restored', util.find_spec('fs_cached') is original)
