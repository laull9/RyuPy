# 删除缓存后生成新的子模块实例。
import mc_state
mc_state.runs += 1
value = 'child:' + str(mc_state.runs)
mc_state.events.append(value)
