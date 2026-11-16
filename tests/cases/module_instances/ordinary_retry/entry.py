# ryupy: pending
# 真实普通import失败重试；只给目标模块显式实例能力，不修改Python执行逻辑。
import sys
import fixture_control
try:
    import retry_module
except RuntimeError as error:
    assert str(error) == 'ordinary-failure'
else:
    raise AssertionError('first import must fail')
old = fixture_control.saved[0]
old_dict, old_items, old_spec = old.__dict__, old.items, old.__spec__
assert type(old_dict) is dict
assert 'retry_module' not in sys.modules
assert sys.modules['fixture_control'] is fixture_control
assert old.partial == 'written' and old.attempt == 1
assert 'completed' not in old_dict
print('failed instance preserved')
import retry_module
fresh = retry_module
assert fresh is not old and fresh.__dict__ is not old_dict
assert fresh.items is not old_items and fresh.items == [2]
assert fresh.completed is True
assert old.__dict__ is old_dict and old.items is old_items
assert old.items == [1] and old.attempt == 1 and old.__spec__ is old_spec
assert 'completed' not in old_dict
assert sys.modules['retry_module'] is fresh
assert fixture_control.saved == [old, fresh] and fixture_control.attempts == 2
import retry_module as again
assert again is fresh and fixture_control.attempts == 2
print('fresh retry isolated')
