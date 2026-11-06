# 原生异常身份与普通源模块初始化时机分开验证，不依赖sys.modules。
import getopt as reference


def module_alias():
    import getopt as options
    try:
        options.getopt(['-z'], 'a')
    except options.GetoptError as error:
        print('module', type(error) is reference.GetoptError, error.args)


def direct_alias():
    from getopt import GetoptError as Error
    from getopt import error as Legacy
    print('identity', Error is reference.GetoptError, Legacy is Error)
    try:
        raise Error('local', 'x')
    except (Error, ValueError) as error:
        print('tuple', type(error) is reference.GetoptError, error.args)


def other_native():
    from io import UnsupportedOperation as IoError
    try:
        raise IoError('other-native')
    except IoError as error:
        print('generic-native', str(error))


def ancestor():
    import getopt as options
    try:
        raise options.GetoptError('ancestor')
    except Exception as error:
        print('ancestor', isinstance(error, BaseException), error.args)


def load_source():
    import probe_side_effect as local_probe
    print('source-value', local_probe.value)


print('defined')
try:
    print(options)
except NameError:
    print('no-global-before')
module_alias()
direct_alias()
other_native()
ancestor()
try:
    print(options)
except NameError:
    print('no-global-after')
options = 'module-write-allowed'
print(options)
load_source()
load_source()
