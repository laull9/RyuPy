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

# 四个槽都必须走生成的删除/读取路径；不只测运行时手写委托。
def read_rest():
    return __loader__, __cached__, __path__

def delete_rest():
    global __loader__, __cached__, __path__
    del __loader__
    del __cached__
    del __path__
