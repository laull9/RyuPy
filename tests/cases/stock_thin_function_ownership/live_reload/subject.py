# 显式 stock 普通函数首切输入：没有业务 closure、默认值或隐藏 owner。
offset = 10


def read(x):
    return x + offset


def echo(x):
    return x


def fail(x):
    raise x
