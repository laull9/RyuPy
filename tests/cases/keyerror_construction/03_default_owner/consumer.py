# 纯静态默认域consumer：不定义KeyError或__builtins__，不得自动替换为instance来绕开冷ABI。
def call(value):
    return KeyError(value)


def save():
    return KeyError


def evaluate(argument):
    return KeyError(argument())
