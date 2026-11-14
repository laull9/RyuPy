# 父包主动导入目标，并修改缓存规格以区分第二次缓存查询与 finder 顺序。
import fs_state
fs_state.events.append('parent-start')
from . import child
saved_spec = child.__spec__
marker = []
child.__spec__ = marker
fs_state.events.append('parent-end')
