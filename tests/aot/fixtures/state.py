# 跨模块真实槽读取/公开setter与函数内部读取使用同一状态。
value = 3


def read():
    return value


def increment(amount=1):
    global value
    value += amount
    return value
