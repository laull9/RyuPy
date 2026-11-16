# ryupy: pending
# 抛错时无条件删除当前目标槽；依赖、别名与已逃逸对象保留。
import sys
import mc_state
try:
    import mc_subject
except RuntimeError as exc:
    assert str(exc) == 'initialization failed after replacement'
else:
    raise AssertionError('first import must fail')
import mc_donor
old = mc_state.saved[0]
assert 'mc_subject' not in sys.modules
assert sys.modules['mc_donor'] is mc_donor
assert sys.modules['mc_failed_alias'] is old
assert old.serial == 1 and mc_state.events == ['subject:1', 'donor']
import mc_subject as fresh
assert fresh is not old and fresh.serial == 2
assert sys.modules['mc_subject'] is fresh
assert sys.modules['mc_failed_alias'] is old and old.serial == 1
assert mc_state.events == ['subject:1', 'donor', 'subject:2']
print('failure removes replacement but retains dependency and alias', True)
