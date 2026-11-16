# 默认模块完整探针：嵌套/方法/生成器 global del 与动态异常目标，不以宿主替代正文。
from io import UnsupportedOperation as NativeError

value = 17

def factory():
    cell = ['kept']
    def inner():
        global value
        saved = value
        del value
        return saved, cell
    return inner

nested = factory()
assert nested() == (17, ['kept'])
try:
    value
except NameError as error:
    assert str(error) == "name 'value' is not defined"
else:
    assert False
value = 18

class Owner:
    def remove(self):
        global value
        del value

Owner().remove()
try:
    value
except NameError:
    pass
else:
    assert False
value = 19

def generate():
    global value
    yield value
    del value
    yield 'deleted'
    value = 20
    yield value

generator = generate()
assert next(generator) == 19
assert next(generator) == 'deleted'
try:
    value
except NameError:
    pass
else:
    assert False
assert next(generator) == 20 and value == 20
print('DEFAULT_GLOBAL_SCOPES_PASS')

# 删除用户异常公开名称后，except 必须先真实读取目标，不能保留旧 CLR typed catch。
class UserError(ValueError):
    pass

saved_user = UserError
del UserError
try:
    try:
        raise saved_user('old-user')
    except UserError:
        assert False
except NameError as error:
    assert str(error) == "name 'UserError' is not defined"
else:
    assert False

# 即使元组第一项匹配，也必须先求完整目标表达式，不能跳过已删除的第二项。
try:
    try:
        raise KeyError('tuple')
    except (KeyError, UserError):
        assert False
except NameError as error:
    assert str(error) == "name 'UserError' is not defined"
else:
    assert False
UserError = saved_user
try:
    raise saved_user('restored-user')
except UserError as error:
    assert str(error) == 'restored-user'
else:
    assert False
UserError = KeyError
try:
    raise saved_user('not-new-target')
except UserError:
    assert False
except ValueError as error:
    assert str(error) == 'not-new-target'
else:
    assert False
print('DEFAULT_USER_EXCEPTION_TARGET_PASS')

# 原生异常导入别名同样不能在 del 后从 import 静态身份复活。
saved_native = NativeError
del NativeError
try:
    try:
        raise saved_native('old-native')
    except NativeError:
        assert False
except NameError as error:
    assert str(error) == "name 'NativeError' is not defined"
else:
    assert False
NativeError = saved_native
try:
    raise saved_native('restored-native')
except NativeError as error:
    assert str(error) == 'restored-native'
else:
    assert False
print('DEFAULT_NATIVE_EXCEPTION_TARGET_PASS')

# 内建异常遮蔽删除后读取才回退；再次删除只看模块绑定，应是 NameError。
saved_value_error = ValueError
ValueError = TypeError
del ValueError
try:
    raise saved_value_error('builtin-restored')
except ValueError as error:
    assert str(error) == 'builtin-restored'
else:
    assert False
try:
    del ValueError
except NameError as error:
    assert str(error) == "name 'ValueError' is not defined"
else:
    assert False
assert ValueError is saved_value_error
ValueError = 42
try:
    try:
        raise saved_value_error('invalid-target')
    except ValueError:
        assert False
except TypeError as error:
    assert str(error) == 'catching classes that do not inherit from BaseException is not allowed'
else:
    assert False
del ValueError
assert ValueError is saved_value_error
print('DEFAULT_BUILTIN_EXCEPTION_TARGET_PASS')
print('MODULE_DELETE_DEFAULT_ALL_ASSERTIONS_PASSED')
