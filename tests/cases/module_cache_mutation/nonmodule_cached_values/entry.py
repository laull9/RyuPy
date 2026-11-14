# ryupy: pending
# 缓存命中不要求模块类型，也不能把一般假值当作未命中。
import sys
import mc_state
class Holder:
    pass
holder = Holder()
holder.value = ['holder']
for value in (False, 0, '', [], {}, 42, holder):
    sys.modules['mc_target'] = value
    import mc_target as current
    assert current is value and sys.modules['mc_target'] is value
    assert mc_state.runs == 0
from mc_target import value as exported
assert exported is holder.value
sys.modules['mc_target'] = 0
try:
    from mc_target import value as missing
except ImportError as exc:
    assert str(exc) == "cannot import name 'value' from '<unknown module name>' (unknown location)"
else:
    raise AssertionError('missing export must fail')
assert mc_state.runs == 0
print('arbitrary cached values and from lookup', True)
