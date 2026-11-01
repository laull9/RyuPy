# 注册身份、参数引用、相等注销及真实进程退出的LIFO执行。
import atexit
log = []
def callback(value, *, suffix):
    print(value, suffix)
value = []
print(atexit.register(callback, value, suffix='first') is callback)
value.append(1)
atexit.register(print, 'last registered')
class EqualCallback:
    def __init__(self, n):
        self.n = n
    def __eq__(self, other):
        if not isinstance(other, EqualCallback):
            return False
        print('eq', self.n, other.n)
        return self.n % 2 == other.n % 2
    def __call__(self):
        print('remaining', self.n)
atexit.register(EqualCallback(1))
atexit.register(EqualCallback(3))
atexit.register(EqualCallback(2))
print(atexit.unregister(EqualCallback(5)))
print('body')
