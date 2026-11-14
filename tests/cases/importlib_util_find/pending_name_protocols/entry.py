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
events = []
class Name(str):
    def startswith(self, prefix):
        events.append(('startswith', prefix))
        return super().startswith(prefix)
name = Name('fs_cached')
check('string_subclass_protocol', util.find_spec(name) is fs_cached.__spec__)
check('startswith_once_absolute', events == [('startswith', '.')])
class Package:
    def __bool__(self):
        events.append('bool')
        return True
    def rsplit(self, separator, count):
        events.append(('rsplit', separator, count))
        return ['fs_cached']
events.clear()
check('package_protocol', util.find_spec('.', Package()) is fs_cached.__spec__)
check('package_hook_order', events == ['bool', ('rsplit', '.', 0)])
class BrokenPackage:
    def __bool__(self):
        events.append('broken-bool')
        raise RuntimeError('package truth failed')
error('package_truth_error', lambda: util.find_spec('.child', BrokenPackage()), RuntimeError, 'package truth failed')
check('truth_error_phase', events[-1] == 'broken-bool')
