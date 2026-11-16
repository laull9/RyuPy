# None不是正文执行失败，初始化收尾不得偷偷恢复原对象。
import sys
import mc_state
mc_state.runs += 1
mc_state.original = sys.modules[__name__]
sys.modules[__name__] = None
finished = True
