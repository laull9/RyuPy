# GetoptError真实对象与字段协议；args重赋值不在本轮。
import getopt

def report(label, fn):
    try:
        value = fn()
    except Exception as error:
        print(label, 'error', type(error).__name__,
              error.args, str(error), getattr(error, 'msg', None),
              getattr(error, 'opt', None))
    else:
        print(label, 'ok', value)


class Message(str):
    pass


def objects():
    print('type', getopt.error is getopt.GetoptError,
          issubclass(getopt.GetoptError, Exception), getopt.GetoptError.__module__,
          getopt.GetoptError.__name__, getopt.GetoptError.msg, getopt.GetoptError.opt)
    for error in [getopt.GetoptError('message'), getopt.GetoptError('message', 'x'),
                  getopt.GetoptError(msg='message', opt='x')]:
        print('construct', type(error) is getopt.GetoptError, error.msg, error.opt,
              error.args, str(error), isinstance(error, Exception))
    error = getopt.GetoptError('original', 'a')
    error.msg = 'changed'
    error.opt = 'b'
    print('write-fields', error.msg, error.opt, error.args, str(error))
    del error.msg
    del error.opt
    print('delete-fallback', error.msg, error.opt, error.args, str(error))
    error.msg = 'changed'
    error.opt = 'b'
    error.msg = 7
    print('raw-nonstring-msg', error.msg, error.opt, error.args)
    print('direct-nonstring-str', error.__str__())
    report('str-nonstring-msg', lambda: str(error))
    message = Message('subclass-message')
    subclass_error = getopt.GetoptError(message)
    print('subclass-msg', subclass_error.msg is message,
          subclass_error.args[0] is message, subclass_error.__str__() is message,
          str(subclass_error) is message, type(str(subclass_error)).__name__)
    raw = getopt.GetoptError(7, None)
    print('construct-nonstrings', raw.msg, raw.opt, raw.args)
    report('construct-zero', lambda: getopt.GetoptError())
    report('construct-three', lambda: getopt.GetoptError('a', 'b', 'c'))
    report('construct-unknown', lambda: getopt.GetoptError('a', unknown=1))
    report('construct-duplicate', lambda: getopt.GetoptError('a', msg='b'))


objects()

# 长参数用户错误逐轮fresh；外层except在模块，函数仅有内层捕获。
class ValueTruth:
    def __init__(self, error):
        self.error = error
        self.calls = 0

    def __bool__(self):
        self.calls += 1
        raise self.error


def check_value(outer):
    original = ValueError('fresh-value-error')
    value = ValueTruth(original)
    opts, tail = getopt.getopt(['-b', value], 'b:')
    print('short-object', opts[0][1] is value, value.calls, tail)
    try:
        getopt.getopt(['--beta', value], '', ['beta='])
    except ValueError as error:
        print('long-object', error is original, error.__context__ is outer,
              error.__cause__ is None, error.__suppress_context__, value.calls)


for index in range(2):
    try:
        raise RuntimeError('fresh-outer')
    except RuntimeError as outer:
        check_value(outer)
