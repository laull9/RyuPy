# 静态from与传递模块的inspect需求必须保留；公开对象与参数绑定按CP同次比较。
from helper import inspect_function


def target(value=4, *, flag=True):
    return value, flag


print(inspect_function(target))
