# 正在执行的模块对象和当前缓存条目从这里开始可以不同。
import sys
import mc_state
mc_state.events.append('subject:start')
before = 'original'
mc_state.original = sys.modules[__name__]
import mc_donor
sys.modules[__name__] = mc_donor
import mc_subject as same_body
from mc_subject import value
mc_state.inside = same_body
mc_state.exported = value
after = 'finished'
mc_state.events.append('subject:end')
