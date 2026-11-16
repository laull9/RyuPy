# 成功依赖不因调用方失败回滚。
import mc_state
mc_state.events.append('donor')
value = 'donor'
