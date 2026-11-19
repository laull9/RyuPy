# ryupy: pending
# 同一from语句在首属性读取期间替换缓存，后续属性仍取最初对象。
import sys
import mc_state
import mc_replacement
class Holder:
    @property
    def a(self):
        mc_state.events.append('a')
        sys.modules['mc_target'] = mc_replacement
        return 'old-a'
    @property
    def b(self):
        mc_state.events.append('b')
        return 'old-b'
holder = Holder()
sys.modules['mc_target'] = holder
from mc_target import a, b
assert a == 'old-a' and b == 'old-b'
assert sys.modules['mc_target'] is mc_replacement
assert mc_state.events == ['a', 'b']
from mc_target import a as next_a, b as next_b
assert next_a == 'new-a' and next_b == 'new-b'
assert mc_state.events == ['a', 'b']
print('from import captures one module per statement', True)
