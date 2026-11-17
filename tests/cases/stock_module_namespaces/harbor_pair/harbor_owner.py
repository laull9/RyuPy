# 权威名字来自模块定义点，修改字典时保存的函数仍读取同一个后端。
offset = 5
problem = KeyError
def measure(values):
    return len(values) + offset
def expose():
    return globals()
def fail(kind):
    raise kind('harbor')
