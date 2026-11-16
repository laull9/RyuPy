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
for attempt in (1, 2):
    error('parent_exception', lambda: util.find_spec('fs_broken.child'), RuntimeError, 'parent failed')
    check('failed_parent_removed', 'fs_broken' not in sys.modules and 'fs_broken.child' not in sys.modules)
    check('parent_retry_side_effect', len(fs_state.events) == attempt and fs_state.events[-1] == 'parent')
# 真实失败导入是图登记和失败语义断言，绝不是永假 import。
try:
    import fs_broken.child
except RuntimeError as exc:
    check('ordinary_parent_failure', str(exc) == 'parent failed' and fs_state.events == ['parent', 'parent', 'parent'])
else:
    raise AssertionError('父包必须失败')
