# 首次替换自己的缓存后抛错；第二次正常完成。
import sys
import mc_state
mc_state.runs += 1
serial = mc_state.runs
mc_state.events.append('subject:' + str(serial))
mc_state.saved.append(sys.modules[__name__])
if serial == 1:
    sys.modules['mc_failed_alias'] = sys.modules[__name__]
    import mc_donor
    sys.modules[__name__] = mc_donor
    raise RuntimeError('initialization failed after replacement')
