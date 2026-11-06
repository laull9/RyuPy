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
absolute = util.find_spec(name='fs_rel.child', package=None)
relative = util.find_spec('.child', package='fs_rel')
check('equivalent_names', absolute == relative and absolute is not relative)
upward = util.find_spec('..child', 'fs_rel.inner')
check('upward_without_inner_import', upward == absolute and 'fs_rel.inner' not in sys.modules)
check('dot_returns_parent', util.find_spec('.', 'fs_rel') is sys.modules['fs_rel'].__spec__)
check('two_dots_return_parent', util.find_spec('..', 'fs_rel.inner') is sys.modules['fs_rel'].__spec__)
error('no_package', lambda: util.find_spec('.child'), ImportError, "no package specified for '.child' (required for relative module names)")
error('empty_package', lambda: util.find_spec('.child', ''), ImportError, "no package specified for '.child' (required for relative module names)")
error('beyond_top', lambda: util.find_spec('..child', 'fs_rel'), ImportError, 'attempted relative import beyond top-level package')
error('only_dots_beyond_top', lambda: util.find_spec('...', 'fs_rel.inner'), ImportError, 'attempted relative import beyond top-level package')
check('errors_no_child_execution', fs_state.events == ['parent'] and 'fs_rel.child' not in sys.modules)
import fs_rel.child
check('real_child_import', fs_state.events == ['parent', 'child'])
