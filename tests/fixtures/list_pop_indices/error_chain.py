# 未捕获__index__错误必须保留全部用户帧与外层context，不重包为索引类型错误。
events = []


class Broken:
    def __index__(self):
        events.append('index')
        print('callback', events)
        raise LookupError('index chain failed')


def middle(values):
    return values.pop(Broken())


def outer(values):
    return middle(values)


try:
    raise ValueError('outer context')
except ValueError:
    outer([10, 20])
