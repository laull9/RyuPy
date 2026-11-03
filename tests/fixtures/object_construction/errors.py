# 验证初始化返回及参数绑定失败context；不依赖未实现的tb_frame。
class BadReturn:
    def __init__(self):
        return 7
class BindNew:
    def __new__(cls, required):
        return object.__new__(cls)
class BindInit:
    def __init__(self, required):
        pass
class NewError:
    def __new__(cls):
        raise RuntimeError('new-error')
class InitError:
    def __init__(self):
        raise RuntimeError('init-error')
for cls in [BadReturn, BindNew, BindInit, NewError, InitError]:
    try:
        raise ValueError('active')
    except ValueError as active:
        try:
            cls()
        except (TypeError, RuntimeError) as error:
            assert error.__context__ is active
            print(cls.__name__, type(error).__name__)
        else:
            raise AssertionError('error missing')
log = []
def arg(value):
    log.append(value)
    return value
class NonCallable:
    __new__ = None
try:
    NonCallable(arg(1), value=arg(2))
except TypeError:
    assert log == [1, 2]
else:
    raise AssertionError('noncallable new')
print('errors-ok')
