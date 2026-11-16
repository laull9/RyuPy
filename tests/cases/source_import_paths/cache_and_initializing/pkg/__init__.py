# 待跑合同输入；未执行，不能作为已验 CP 事实。
print('C:parent-enter')
from . import child
__path__ = []
print('C:parent-exit')
