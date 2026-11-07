# 首次普通 import 失败；重试必须新建真实模块，而非复用逃逸的失败对象。
import lifecycle_state
import sys
lifecycle_state.tries += 1
lifecycle_state.seen.append(sys.modules[__name__])
local_value = lifecycle_state.tries
def read():
    return local_value
if lifecycle_state.tries == 1:
    raise ValueError('first-import')
