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

# 与完整 nonpackage_parent 分开；本例不写 sys.modules，不删减原完整断言。
import fs_state
exc = error('nonpackage_parent', lambda: util.find_spec('fs_plain.child'), ModuleNotFoundError, "__path__ attribute not found on 'fs_plain' while trying to find 'fs_plain.child'")
check('error_name_and_cause', exc.name == 'fs_plain.child' and type(exc.__cause__) is AttributeError)
check('cause_message', str(exc.__cause__) == "module 'fs_plain' has no attribute '__path__'")
check('parent_side_effect_retained', fs_state.events == ['plain'] and 'fs_plain' in sys.modules and 'fs_plain.child' not in sys.modules)
import fs_plain
check('parent_not_reexecuted', fs_state.events == ['plain'])
