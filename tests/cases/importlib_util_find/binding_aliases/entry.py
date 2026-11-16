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

from importlib.util import find_spec
import importlib.util as again
import fs_cached
check('function_identity', find_spec is util.find_spec and again.find_spec is find_spec)
aliases = [find_spec, {'find': util.find_spec}]
check('container_call', aliases[0]('fs_cached') is fs_cached.__spec__ and aliases[1]['find'](package=None, name='fs_cached') is fs_cached.__spec__)
check('star_call', find_spec(*('fs_cached',), **{'package': None}) is fs_cached.__spec__)
error('missing_name', lambda: find_spec(), TypeError, "find_spec() missing 1 required positional argument: 'name'")
error('only_package', lambda: find_spec(package='fs_cached'), TypeError, "find_spec() missing 1 required positional argument: 'name'")
error('too_many_positional', lambda: find_spec('fs_cached', None, None), TypeError, 'find_spec() takes from 1 to 2 positional arguments but 3 were given')
error('duplicate_name', lambda: aliases[0]('fs_cached', name='other'), TypeError, "find_spec() got multiple values for argument 'name'")
error('duplicate_package', lambda: aliases[1]['find']('fs_cached', None, package='other'), TypeError, "find_spec() got multiple values for argument 'package'")
error('unknown_keyword', lambda: find_spec('fs_cached', unknown=True), TypeError, "find_spec() got an unexpected keyword argument 'unknown'")
error('name_int', lambda: find_spec(1), AttributeError, "'int' object has no attribute 'startswith'")
error('name_none', lambda: find_spec(None), AttributeError, "'NoneType' object has no attribute 'startswith'")
error('name_bytes', lambda: find_spec(b'fs_cached'), TypeError, 'startswith first arg must be bytes or a tuple of bytes, not str')
error('relative_package_int', lambda: find_spec('.child', 1), AttributeError, "'int' object has no attribute 'rsplit'")
error('relative_package_none', lambda: find_spec('.child', None), ImportError, "no package specified for '.child' (required for relative module names)")
check('absolute_ignores_package', find_spec('fs_cached', package=[]) is fs_cached.__spec__)
