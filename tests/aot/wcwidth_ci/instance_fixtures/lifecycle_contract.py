# 三个独立观察点，不以重执行合同冒充普通 import 失败重试。
import sys
import cycle_left
import cycle_right
assert cycle_left.read() == 'left'
assert cycle_right.read() is cycle_left
print('cycle same instance ok')
import lifecycle_state
import reentry_subject
assert reentry_subject.read() is reentry_subject
assert lifecycle_state.seen == ['enter']
print('initialization reentry ok')
retained_state = lifecycle_state
retained_seen = lifecycle_state.seen
try:
    import retry_subject
except ValueError as error:
    assert str(error) == 'first-import'
else:
    raise AssertionError('首次 import 必须失败')
assert 'retry_subject' not in sys.modules
assert sys.modules['lifecycle_state'] is retained_state
assert lifecycle_state.seen is retained_seen
assert lifecycle_state.seen[1].read() == 1
import retry_subject
assert retry_subject.read() == 2
assert lifecycle_state.tries == 2
assert lifecycle_state.seen[1] is not retry_subject
assert lifecycle_state.seen[2] is retry_subject
assert lifecycle_state.seen[1].read.__globals__ is lifecycle_state.seen[1].__dict__
assert retry_subject.read.__globals__ is retry_subject.__dict__
assert sys.modules['lifecycle_state'] is retained_state
assert lifecycle_state.seen is retained_seen
assert sys.modules['retry_subject'] is retry_subject
old_module = lifecycle_state.seen[1]
old_read = old_module.read
old_module.local_value = 41
retry_subject.local_value = 82
assert old_read() == 41 and retry_subject.read() == 82
print('ordinary import retry fresh instance ok')
