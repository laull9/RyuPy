# 与右模块和内建类同名，但必须保持独立异常身份。
class ValueError(Exception):
    pass


def run():
    try:
        raise ValueError('左')
    except ValueError as error:
        print('左捕获', str(error))


def raise_error():
    raise ValueError('左跨模块')


def paused():
    try:
        yield '等待左异常'
    except ValueError as error:
        print('生成器左捕获', str(error))
        yield '已捕获'


class Child(ValueError):
    pass


def inherited():
    try:
        raise Child('派生')
    except ValueError as error:
        print('左父捕获派生', type(error) is Child, type(error).__name__, str(error))
