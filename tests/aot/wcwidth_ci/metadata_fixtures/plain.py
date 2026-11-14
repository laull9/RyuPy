# 同一模块的裸名与外部属性必须共享真实槽，删除后不回退初值。
assert __spec__ is None
assert __loader__ is None
assert __cached__ is None
try:
    __path__
except NameError:
    pass
else:
    raise AssertionError("ordinary path")

def check():
    return __spec__

def delete():
    global __spec__
    del __spec__

value = 7

# 每个槽独立读取，避免第一个未支持fallback掩盖其它名字的结果。
def read_loader():
    return __loader__

def read_cached():
    return __cached__

def read_path():
    return __path__

def delete_loader():
    global __loader__
    del __loader__

def delete_cached():
    global __cached__
    del __cached__

def delete_path():
    global __path__
    del __path__
