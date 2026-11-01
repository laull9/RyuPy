# 无 import / 动态操作的模块：失败重试也必须清除 typed 整数槽的绑定标记。
try:
    print(number)
except NameError:
    number = 77
    raise ValueError('静态失败')
