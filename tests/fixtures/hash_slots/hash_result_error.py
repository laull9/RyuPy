# 用户hash回调的原始异常与context在真实进程保留源码帧；按basename/line/function投影对照。
class Failure:
    def __hash__(self):
        raise RuntimeError('hash-body')
def caller():
    return hash(Failure())
try:
    raise ValueError('active')
except ValueError:
    caller()
