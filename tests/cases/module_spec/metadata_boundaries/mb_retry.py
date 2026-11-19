# 首次模块被导入缓存移除后，旧模块对象仍由另一个模块保留。
import sys
import mb_state
mb_state.attempts += 1
if mb_state.attempts == 1:
    mb_state.old_modules.append(sys.modules[__name__])
    mb_state.old_specs.append(__spec__)
    mb_state.old_loaders.append(__loader__)
    raise ValueError('retained first module')
ready = True
