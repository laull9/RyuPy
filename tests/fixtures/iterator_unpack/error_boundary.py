# 模块级 ambient 异常避免引入旧 nested-caught 控制流债；末尾保完整用户回调链。
class Good:
    def __init__(self):
        self.index = 0
    def __iter__(self):
        return self
    def __next__(self):
        self.index += 1
        if self.index > 2:
            raise StopIteration('finished')
        return self.index

try:
    raise ValueError('ambient')
except ValueError as ambient:
    a, b = Good()
    print('success in handler', a, b)
    for attempt in range(2):
        try:
            a, b, c = Good()
        except ValueError as short:
            print('short context', str(short), short.__context__ is ambient)
    try:
        raise
    except ValueError as restored:
        print('ambient restored', restored is ambient)

failure = RuntimeError('next failed')
def callback():
    raise failure

class Bad:
    def __iter__(self):
        return self
    def __next__(self):
        return callback()

def unpack():
    a, b = Bad()

try:
    raise ValueError('outer')
except ValueError:
    unpack()
