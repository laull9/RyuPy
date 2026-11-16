# ryupy: pending
# 已缓存完整名字优先于父包查找，不能提前初始化父包。
import sys
import mc_state
import mc_donor
sys.modules['mc_pkg.child'] = mc_donor
from mc_pkg.child import value
assert value == 'donor' and 'mc_pkg' not in sys.modules
assert mc_state.events == ['donor']
from mc_pkg import child
import mc_pkg
assert child is mc_donor and not hasattr(mc_pkg, 'child')
assert mc_state.events == ['donor', 'parent']
assert sys.modules['mc_pkg.child'] is mc_donor
print('cached child bypasses missing parent until parent import', True)
