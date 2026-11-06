# 真实源代码：执行计数与函数始终读取当前目标字典。
try:
    runs += 1
except NameError:
    runs = 1
value = 40 + runs
def read():
    return value
