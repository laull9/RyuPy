# 待跑合同输入；未执行，不能作为已验 CP 事实。
import sys
print('C:before')
import pkg.child
saved = pkg.child
print('C:after-parent', pkg.__path__, 'pkg.child' in sys.modules)
import pkg.child
print('C:cached-same', saved is pkg.child)
# hook 改写也不能使缓存命中进入查找；本臂不请求任何新模块。
sys.meta_path = []
import pkg.child
print('C:cached-with-hooks', saved is pkg.child)
