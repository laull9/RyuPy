# 包内相对导入与函数 global 写入分别保留模块身份。
from . import state

calls = 0


def increment():
    global calls
    calls += 1
    state.value += 1
    return calls
