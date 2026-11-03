# 原上下文在module产生；int错误仍验证真实调用者帧及完整异常链。
def convert():
    return int('10', 1)
try:
    raise ValueError('original')
except ValueError:
    convert()
