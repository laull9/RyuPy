# 新缓存别名供现有静态import/from读取；真实PE证明无需伪源码或未来模块图改造。
import sys
import mc_subject
sys.modules['mc_static_unknown_alias'] = mc_subject
import mc_static_unknown_alias
assert mc_static_unknown_alias is mc_subject
from mc_static_unknown_alias import value
assert value == 10
print('unknown static alias resolves cache before discovery', True)
