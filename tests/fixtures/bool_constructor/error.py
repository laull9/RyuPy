# 原context模块抛捕，保完整caller链而不碰旧首次全栈快照。
def convert():
    return bool(1, 2)
try:
    raise ValueError('original')
except ValueError:
    convert()
