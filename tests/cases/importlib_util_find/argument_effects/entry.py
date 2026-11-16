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

import fs_cached
events = []
def mark(label, value):
    events.append(label)
    return value
class BodyHook:
    def startswith(self, prefix):
        events.append('body-hook')
        raise AssertionError('错误绑定不得进入正文')
error('expressions_before_binding', lambda: util.find_spec(mark('name', BodyHook()), mark('package', None), mark('extra', None)), TypeError, 'find_spec() takes from 1 to 2 positional arguments but 3 were given')
check('left_to_right_no_body', events == ['name', 'package', 'extra'])
events.clear()
error('duplicate_values_evaluated', lambda: util.find_spec(mark('first', BodyHook()), name=mark('duplicate', 'x')), TypeError, "find_spec() got multiple values for argument 'name'")
check('duplicate_no_body', events == ['first', 'duplicate'])
events.clear()
error('unknown_value_evaluated', lambda: util.find_spec(mark('name', BodyHook()), unknown=mark('unknown', True)), TypeError, "find_spec() got an unexpected keyword argument 'unknown'")
check('unknown_no_body', events == ['name', 'unknown'])
events.clear()
result = mark('callee', util.find_spec)(name=mark('name', 'fs_cached'), package=mark('package', object()))
check('successful_evaluation_order', events == ['callee', 'name', 'package'] and result is fs_cached.__spec__)
class IgnoredPackage:
    def __bool__(self):
        raise AssertionError('绝对名不检查 package 真值')
check('absolute_no_package_truth', util.find_spec('fs_cached', IgnoredPackage()) is fs_cached.__spec__)
