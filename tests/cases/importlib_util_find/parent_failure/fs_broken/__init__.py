# 失败父包保留副作用但从缓存清理。
import fs_state
fs_state.events.append('parent')
raise RuntimeError('parent failed')
