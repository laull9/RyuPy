# 同一源码双模块实例：函数、类、闭包、生成器均须读取所属实例的全局。
import builtins
builtins.importlib_design_events.append('body')
value = 10
items = []
def read():
    return value
def write(x):
    global value
    value = x
class Reader:
    def read(self):
        return value
def closure():
    def inner():
        return value
    return inner
def stream():
    yield value
    yield value
