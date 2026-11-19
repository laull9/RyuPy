# warnings共享异常边界：直接API与字符串协议均不得泄漏被吞IO帧或丢失活动异常。
import warnings
import sys
class Key:
    def __index__(self):
        return True
class Sink:
    def write(self, text):
        raise OSError('sink')
old = sys.stderr
try:
    raise RuntimeError('outer')
except RuntimeError as outer:
    try:
        sys.stderr = Sink()
        with warnings.catch_warnings():
            warnings.simplefilter('always')
            for i in range(1500):
                warnings.warn('direct')
                value = 'abc'[Key()]
        print('completed', i, value)
        try:
            raise
        except RuntimeError as error:
            print('active after io', error is outer)
    finally:
        sys.stderr = old
    with warnings.catch_warnings():
        warnings.simplefilter('error')
        warning = UserWarning('existing')
        try:
            warnings.warn(warning)
        except UserWarning as error:
            print('direct warning', error is warning, error.__context__ is outer)
        try:
            'abc'[Key()]
        except DeprecationWarning as error:
            print('index warning', error.__context__ is outer)
        try:
            raise ValueError('inner')
        except ValueError as inner:
            try:
                warnings.warn('nested')
            except UserWarning as error:
                print('nested warning', error.__context__ is inner)
        try:
            raise
        except RuntimeError as error:
            print('active after nested', error is outer)
print('done')
