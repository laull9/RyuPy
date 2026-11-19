# find_spec 完整源已通过冻结 runtime 的真实 PE 差分。
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

import fs_state
check('initial_absence', 'fs_eager' not in sys.modules and 'fs_eager.child' not in sys.modules)
found = util.find_spec('fs_eager.child')
parent = sys.modules['fs_eager']
child = sys.modules['fs_eager.child']
check('parent_body_order', fs_state.events == ['parent-start', 'child', 'parent-end'])
check('parent_attached_own_import', parent.child is child)
check('finder_after_parent_returns_fresh', found is not parent.saved_spec and found == parent.saved_spec)
check('finder_not_second_cache_lookup', found is not child.__spec__ and child.__spec__ is parent.marker)
check('new_call_reads_current_cache', util.find_spec('fs_eager.child') is parent.marker)
child.__spec__ = None
error('next_call_none_spec', lambda: util.find_spec('fs_eager.child'), ValueError, 'fs_eager.child.__spec__ is None')
child.__spec__ = parent.saved_spec
import fs_eager.child
check('no_reexecution', fs_state.events == ['parent-start', 'child', 'parent-end'])
