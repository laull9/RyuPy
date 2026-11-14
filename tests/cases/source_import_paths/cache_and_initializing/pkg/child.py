# 待跑合同输入；未执行，不能作为已验 CP 事实。
import sys
print('C:child-enter', 'pkg.child' in sys.modules)
import pkg
saved = pkg.__path__
pkg.__path__ = []
initializing = sys.modules['pkg.child']
import pkg.child
print('C:child-reentered', initializing is sys.modules['pkg.child'])
pkg.__path__ = saved
print('C:child-exit')
