# 普通源码目标只在异常分派读取，词法局部/cell/free/global分别走原绑定。
def quiet():
    try:
        pass
    except Missing:
        return 'bad'
    return 'quiet'

def builtin_target(error):
    try:
        raise error
    except ValueError:
        return 'value'
    except BaseException:
        return 'other'

def missing_target(error):
    try:
        raise error
    except Missing:
        return 'bad'
    except BaseException:
        return 'must not reach'

def local_target(Target, error):
    try:
        raise error
    except Target:
        return 'match'
    except BaseException:
        return 'next'

def unbound_target(error):
    try:
        raise error
    except Target:
        return 'bad'
    Target = ValueError

def close(Target):
    def run(error):
        try:
            raise error
        except Target:
            return 'free'
        except BaseException:
            return 'other'
    return run

def cell_target(Target, error):
    def read():
        return Target
    try:
        raise error
    except Target:
        return read() is Target

class Handler:
    def run(self, Target, error):
        try:
            raise error
        except Target:
            return 'method'

# 内层目标读取失败后必须恢复外层handler的ambient对象。
def ambient(original, other):
    try:
        raise original
    except ValueError:
        try:
            missing_target(other)
        except NameError as lookup:
            assert lookup.__context__ is other
        try:
            raise
        except ValueError as active:
            return active is original

def handler_error(original):
    try:
        raise original
    except ValueError:
        raise TypeError('handler')

# with合成handler不属于本切源码except；生成器仅继承模块能力。
events = []
class Manager:
    def __enter__(self):
        events.append('enter')
        return self
    def __exit__(self, kind, value, traceback):
        events.append('exit')
        return False

def with_wrap(error):
    with Manager():
        raise error

def with_generator():
    with Manager():
        yield 1
