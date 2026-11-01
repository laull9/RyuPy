# 失败模块重试不得读到首次初始化遗留的静态字段值。
import state
import sys
state.attempts += 1
if state.attempts == 1:
    number = 77
    raise ValueError('首次失败')

print('重试属性', hasattr(sys.modules[__name__], 'number'))
try:
    print(number)
except NameError:
    print('重试仍未绑定')
number = 9
