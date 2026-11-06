# 第一次故意失败，第二次按正常导入重新执行。
import ms_state
ms_state.attempts += 1
if ms_state.attempts == 1:
    ms_state.failed_specs.append(__spec__)
    ms_state.failed_loaders.append(__loader__)
    raise ValueError('first import failure')
ready = True
