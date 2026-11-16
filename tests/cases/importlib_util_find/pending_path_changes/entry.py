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

import fs_paths
initial = util.find_spec('fs_paths.child')
check('initial_found', initial is not None and 'fs_paths.child' not in sys.modules)
original_path = fs_paths.__path__
fs_paths.__path__ = []
try:
    check('path_rebind_observed', util.find_spec('fs_paths.child') is None)
finally:
    fs_paths.__path__ = original_path
check('path_restore_no_negative_cache', util.find_spec('fs_paths.child') is not None)
saved_paths = list(original_path)
original_path.clear()
try:
    check('path_mutation_observed', util.find_spec('fs_paths.child') is None)
finally:
    original_path.extend(saved_paths)
check('path_mutation_restore', util.find_spec('fs_paths.child') is not None)
check('no_parent_attachment', not hasattr(fs_paths, 'child'))
import fs_paths.child
saved = fs_paths.__path__
fs_paths.__path__ = []
try:
    check('cached_target_ignores_path_change', util.find_spec('fs_paths.child') is fs_paths.child.__spec__)
finally:
    fs_paths.__path__ = saved
