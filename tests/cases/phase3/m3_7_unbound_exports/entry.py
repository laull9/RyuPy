# 未绑定槽的模块协议与模块内部 NameError 是两个不同的异常边界。
import helper

print(hasattr(helper, 'injected_later'))
print(getattr(helper, 'injected_later', '默认值'))
print(hasattr(helper, 'conditional_number'))
print(getattr(helper, 'conditional_number', '未赋整数'))
try:
    helper.injected_later
except AttributeError:
    print('属性未绑定')
try:
    helper.read_missing()
except NameError:
    print('全局未绑定')
try:
    from helper import injected_later
except ImportError:
    print('导入未绑定')

helper.injected_later = None
print(hasattr(helper, 'injected_later'), helper.read_missing() is None)
helper.injected_later = 42
from helper import injected_later
print(helper.read_missing(), injected_later)
helper.conditional_number = 9
print(helper.conditional_number)
