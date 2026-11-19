# 每次导入都建立新serial。
import mc_state
mc_state.runs += 1
serial = mc_state.runs
