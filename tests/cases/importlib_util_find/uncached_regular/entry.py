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
from importlib.machinery import ModuleSpec
check('initial_absence', 'fs_pkg' not in sys.modules and 'fs_plain' not in sys.modules)
plain = util.find_spec('fs_plain')
plain_again = util.find_spec(name='fs_' + 'plain', package=object())
check('plain_fresh_equal', plain is not plain_again and plain == plain_again)
check('plain_loader_fresh_equal', plain.loader is not plain_again.loader and plain.loader == plain_again.loader)
check('plain_no_execution', fs_state.events == [] and 'fs_plain' not in sys.modules)
check('plain_metadata', type(plain) is ModuleSpec and plain.name == 'fs_plain' and plain.parent == '' and plain.has_location and plain.submodule_search_locations is None)
check('plain_source', plain.origin.endswith('/fs_plain.py') and plain.cached.endswith('/__pycache__/fs_plain.cpython-312.pyc') and plain.loader is not None)
package = util.find_spec('fs_pkg')
package_again = util.find_spec('fs_pkg')
check('package_fresh_equal', package is not package_again and package == package_again)
check('package_metadata', package.parent == 'fs_pkg' and type(package.submodule_search_locations) is list)
check('package_location_lists', package.submodule_search_locations == package_again.submodule_search_locations and package.submodule_search_locations is not package_again.submodule_search_locations)
check('package_no_execution', fs_state.events == [] and 'fs_pkg' not in sys.modules)
child = util.find_spec('fs_pkg.child')
check('parent_import_only', fs_state.events == ['parent'] and 'fs_pkg' in sys.modules and 'fs_pkg.child' not in sys.modules)
parent = sys.modules['fs_pkg']
check('child_not_attached', not hasattr(parent, 'child'))
child_again = util.find_spec('.child', 'fs_pkg')
check('child_fresh_equal', child is not child_again and child == child_again)
check('child_loader_fresh_equal', child.loader is not child_again.loader and child.loader == child_again.loader)
child.name = 'changed'
check('fresh_specs_independent', child_again.name == 'fs_pkg.child')
check('parent_once', fs_state.events == ['parent'] and not hasattr(parent, 'child'))
# 真正导入在查询断言之后；同时让现有编译图自然登记目标，不添加永假 import。
import fs_plain
import fs_pkg.child
check('later_import_bodies', fs_state.events == ['parent', 'plain', 'child'])
check('later_parent_attachment', parent.child is sys.modules['fs_pkg.child'])
check('later_cached_identity', util.find_spec('fs_plain') is fs_plain.__spec__ and util.find_spec('fs_pkg.child') is fs_pkg.child.__spec__)
