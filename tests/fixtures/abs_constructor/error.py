# 模块原异常避免既有首次快照包含未传播caller的独立旧债。
def convert():
    return abs()
try:
    raise ValueError('original')
except ValueError:
    convert()
