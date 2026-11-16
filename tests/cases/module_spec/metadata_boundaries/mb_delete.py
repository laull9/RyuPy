# 普通模块函数的裸名字读取与外部模块属性共享同一全局字典。
def read_spec():
    return __spec__

def read_loader():
    return __loader__

def delete_spec_bare():
    global __spec__
    del __spec__

def delete_loader_bare():
    global __loader__
    del __loader__
