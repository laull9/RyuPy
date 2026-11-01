from io import UnsupportedOperation as Unsupported
# 用户同名类不是内建 ValueError，不能接受额外父边捕获。
class ValueError(Exception):
    pass
try:
    raise Unsupported("not user error")
except ValueError:
    print("wrong shadow")
except OSError:
    print("shadow not matched")
