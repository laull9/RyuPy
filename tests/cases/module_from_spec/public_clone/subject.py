# 每次真实正文执行产生独立函数、类及容器。
value = 10
items = []
def read():
    return value
def write(v):
    global value
    value = v
def closure():
    def inner():
        return value
    return inner
def stream():
    yield value
    yield value
class Reader:
    def read(self):
        return value
