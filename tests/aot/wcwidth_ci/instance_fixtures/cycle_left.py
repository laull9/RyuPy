# 初始化中发布的真实模块应被环上的另一方读取。
value = 'left'
import cycle_right
def read():
    return cycle_right.seen
