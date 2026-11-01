# 固定3.12.13的空槽、注册变化与有限重入行为。
import atexit
atexit._clear()
def older():
    print('older')
def newer():
    print('newer')
    atexit.unregister(older)
    atexit.register(print, 'not run')
atexit.register(older)
atexit.register(newer)
print(atexit._ncallbacks())
atexit._run_exitfuncs()
print('after', atexit._ncallbacks())
atexit.register(older)
atexit.register(older)
atexit.unregister(older)
print('empty slots', atexit._ncallbacks())
atexit._clear()
count = 0
def recursive():
    global count
    count += 1
    print('recursive', count)
    if count == 1:
        atexit._run_exitfuncs()
atexit.register(older)
atexit.register(recursive)
atexit._run_exitfuncs()
print('done')
class Comparison:
    def __init__(self, n):
        self.n = n
    def __eq__(self, other):
        print('compare', self.n)
        if self.n == 2:
            raise ValueError('compare')
        return True
    def __call__(self):
        print('call', self.n)
for n in [1, 2, 3]:
    atexit.register(Comparison(n))
try:
    atexit.unregister(Comparison(0))
except ValueError:
    print('comparison failed')
atexit._run_exitfuncs()
# 手动运行保留调用者当前异常，不把“保存token”误作清空异常上下文。
def inherit_exception():
    try:
        raise
    except ValueError as error:
        print('inherited', str(error))
atexit.register(inherit_exception)
try:
    raise ValueError('outer')
except ValueError:
    atexit._run_exitfuncs()
    try:
        raise
    except ValueError as error:
        print('restored', str(error))
