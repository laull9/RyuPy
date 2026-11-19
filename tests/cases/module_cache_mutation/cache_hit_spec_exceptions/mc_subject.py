# 使用显式实例函数的默认builtins取得真实导入函数。
import mc_state
mc_state.runs += 1
value = 17
def read():
    return value
