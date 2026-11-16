# 缓存命中期间本正文不应执行。
import mc_state
mc_state.events.append('target')
value = 'target'
