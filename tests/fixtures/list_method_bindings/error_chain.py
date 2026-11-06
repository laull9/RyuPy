# 前三方法捕获打印绑定错误；最后模块handler触发pop错误，保留完整用户context链。
def clear_error(values):
    values.clear(1)


def reverse_error(values):
    method = values.reverse
    method(bad=2)


def copy_error(values):
    method = values.copy
    method(1, 2)


def pop_error(values):
    values.pop(0, 1, bad=3)


values = [10, 20]
for name, operation in [('clear', clear_error), ('reverse', reverse_error), ('copy', copy_error)]:
    try:
        operation(values)
    except TypeError as error:
        print('caught', name, type(error).__name__, str(error), error.__context__ is None, values)

try:
    raise ValueError('outer context')
except ValueError:
    pop_error(values)
