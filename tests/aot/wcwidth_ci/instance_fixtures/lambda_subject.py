# 固定参数闭合 lambda，所有返回路径都在 Native 合同中实际调用。
def make():
    return lambda value: value.encode()

def zero():
    return lambda: 7

def positional():
    return lambda value, /: value.encode()

def keyword():
    return lambda *, value: value.encode()

def many():
    return lambda f, x, /, y, *, z: f(x, y, z=z)
