# 待跑合同输入；未执行，不能作为已验 CP 事实。
import sys
print('N:before', 'pkg' in sys.modules, 'pkg.child' in sys.modules)
import pkg.child
print('N:after', pkg.child.value, 'pkg.child' in sys.modules)
