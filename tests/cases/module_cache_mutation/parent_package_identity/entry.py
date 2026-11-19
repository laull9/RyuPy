# ryupy: pending
# 子缓存替换不会自动改父包属性，语法绑定与缓存对象必须分清。
import sys
import mc_state
import mc_pkg.child
old = mc_pkg.child
import mc_donor
sys.modules['mc_pkg.child'] = mc_donor
import mc_pkg.child as via_dotted_alias
from mc_pkg import child as via_parent
from mc_pkg.child import value as via_child
assert via_dotted_alias is old and via_parent is old
assert via_child == 'donor' and sys.modules['mc_pkg.child'] is mc_donor
assert mc_pkg.child is old and old.value == 'child:1'
del mc_pkg.child
from mc_pkg import child as fallback
assert fallback is mc_donor and not hasattr(mc_pkg, 'child')
assert mc_state.events == ['parent', 'child:1', 'donor']
del sys.modules['mc_pkg.child']
import mc_pkg.child as fresh
assert fresh is not old and fresh is mc_pkg.child
assert fresh is sys.modules['mc_pkg.child'] and fresh.value == 'child:2'
assert old.value == 'child:1'
assert mc_state.events == ['parent', 'child:1', 'donor', 'child:2']
print('parent binding/cache/from fallback remain distinct', True)
