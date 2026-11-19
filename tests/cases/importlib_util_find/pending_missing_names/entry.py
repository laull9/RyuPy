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

check('unknown_top_level', util.find_spec('absent_find_928471') is None)
import fs_present
check('unknown_child', util.find_spec('fs_present.absent_find_928471') is None)
check('missing_query_no_parent_attachment', not hasattr(fs_present, 'absent_find_928471'))
exc = error('missing_parent', lambda: util.find_spec('absent_parent_928471.child'), ModuleNotFoundError, "No module named 'absent_parent_928471'")
check('missing_parent_metadata', exc.name == 'absent_parent_928471' and exc.__cause__ is None)
# 真实 None 不能改成 NotImplementedError golden；缓存写入后必须立刻可见。
class Holder:
    pass
holder = Holder()
holder.__spec__ = []
sys.modules['absent_find_928471'] = holder
check('no_negative_cache', util.find_spec('absent_find_928471') is holder.__spec__)
del sys.modules['absent_find_928471']
check('absent_again', util.find_spec('absent_find_928471') is None)
