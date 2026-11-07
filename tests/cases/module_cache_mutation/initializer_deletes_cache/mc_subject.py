# 删除后仍执行剩余正文。
import sys
import mc_state
mc_state.runs += 1
mc_state.saved.append(sys.modules[__name__])
del sys.modules[__name__]
finished = True
