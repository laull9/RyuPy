# 显式实例函数提供当前默认导入函数入口。
import mc_state
mc_state.runs += 1
value = 17
def read():
    return value
