# ryupy: pending
"""每次新异常：普通binder错误保ambient，终尾保完整cause/context与用户实参/根方法帧。"""
def direct_failure(value):
    return value.isalpha(1)


def saved_failure(value):
    method = value.isupper
    return method(x=2)


def root_failure(value):
    return bytearray.istitle(value, 1)


def saved_root_failure(value):
    method = bytes.isascii
    return method(value, x=2)


# 在模块建立ambient，四次各自新建，不重复抛同一异常或继承旧用例context。
for label, action, value, message in [
        ('direct', direct_failure, b'A', 'bytes.isalpha() takes no arguments (1 given)'),
        ('saved', saved_failure, bytearray(b'A'), 'bytearray.isupper() takes no keyword arguments'),
        ('root-direct', root_failure, bytearray(b'A'), 'bytearray.istitle() takes no arguments (1 given)'),
        ('root-saved', saved_root_failure, b'A', 'bytes.isascii() takes no keyword arguments')]:
    ambient = RuntimeError('ambient-' + label)
    try:
        raise ambient
    except RuntimeError:
        try:
            action(value)
        except TypeError as error:
            assert type(error) is TypeError and str(error) == message
            assert error.__context__ is ambient and error.__cause__ is None
            assert error.__suppress_context__ is False
            print('T.caught-' + label, str(error), error.__context__ is ambient)
        else:
            raise AssertionError('binding error lost')


def argument_leaf():
    raise ValueError('ascii-argument-body')


def argument_middle():
    return bytearray(b'A').isdigit(argument_leaf())


def argument_outer():
    return argument_middle()


def binder_middle():
    method = bytearray.istitle
    return method(b'A', 1, x=2)


def binder_outer():
    return binder_middle()


# 下面有意exit1；唯一参考未执行此组合，完整真实链由本应用首次CP守门。
try:
    raise LookupError('ascii-cause')
except LookupError as caught:
    saved_cause = caught

print('T.chain', 'next exception is intentional')
try:
    raise RuntimeError('ascii-ambient') from saved_cause
except RuntimeError:
    try:
        argument_outer()
    except ValueError:
        binder_outer()
