# 实例模块正文创建自己的字典、函数与可变状态。
import mc_state
mc_state.runs += 1
value = 10
items = []
def read():
    return value
def write(value_arg):
    global value
    value = value_arg
