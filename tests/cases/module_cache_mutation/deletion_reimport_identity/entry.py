# ryupy: pending
# 删除后不复活旧对象；缓存失效不销毁仍被引用的旧模块。
import sys
import mc_state
import mc_subject
old = mc_subject
assert old.serial == 1
del sys.modules['mc_subject']
assert 'mc_subject' not in sys.modules and old.serial == 1
import mc_subject as fresh
assert fresh is not old and fresh.serial == 2
assert sys.modules['mc_subject'] is fresh
assert mc_state.runs == 2
import mc_subject as again
assert again is fresh and mc_state.runs == 2
del sys.modules['mc_subject']
try:
    del sys.modules['mc_subject']
except KeyError as exc:
    assert exc.args == ('mc_subject',)
else:
    raise AssertionError('missing delete must fail')
import mc_subject as third
assert third is not old and third is not fresh
assert third.serial == 3 and old.serial == 1 and fresh.serial == 2
print('delete/missing/fresh module identity', True)
