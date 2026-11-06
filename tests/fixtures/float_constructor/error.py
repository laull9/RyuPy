# 模块原context不引入既有首次全栈快照的独立债。
def convert():
    return float(1, 2)
try:
    raise ValueError('original')
except ValueError:
    convert()
