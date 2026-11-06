# 模块级ambient到index回调的完整链；不删用户帧或吞并原异常。
failure = RuntimeError('index failed')
def callback():
    print('callback')
    raise failure
class Index:
    def __index__(self):
        print('index')
        return callback()
class Length:
    def __len__(self):
        print('len')
        return Index()
def measure():
    return len(Length())
try:
    raise ValueError('outer')
except ValueError:
    measure()
