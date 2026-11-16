# 每次构造新用户异常；短项不求真，长项求真一次并传播完整上下文。
class ValueTruth:
    def __bool__(self):
        print('value-bool')
        raise ValueError('long-value-error')


def fail():
    import getopt
    value = ValueTruth()
    opts, tail = getopt.getopt(['-b', value], 'b:')
    print('short-identity', opts[0][1] is value, tail)
    getopt.getopt(['--beta', value], '', ['beta='])


try:
    raise RuntimeError('outer-context')
except RuntimeError:
    fail()
