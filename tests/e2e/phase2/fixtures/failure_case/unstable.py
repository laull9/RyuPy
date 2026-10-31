# 第一次初始化失败，第二次借助保留的依赖状态成功。
from . import state

state.attempts += 1
if state.attempts == 1:
    raise RuntimeError("initialization failed")
ready = True
