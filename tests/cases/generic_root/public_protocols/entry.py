# 独立审计：支持域按未修改CP原源逐字差分。
import typing as t
G = t.Generic
T = t.TypeVar('T')
A = G[T]
print('tuple-identity', G.__mro__ is G.__mro__, G.__bases__ is G.__bases__)
print('alias-identity', A.__args__ is A.__args__, A.__parameters__ is A.__parameters__, A.__args__ is A.__parameters__, t.get_args(A) is A.__args__)
x = G()
def error(label, f):
    try:
        f()
    except Exception as e:
        print(label, type(e).__name__, str(e))
error('int', lambda: int(x))
error('float', lambda: float(x))
error('iter', lambda: iter(x))
error('missing-Generic', lambda: getattr(x, 'Generic'))
G.extra = 12
print('extra', G.extra, x.extra, A.extra)
for module in ('changed', None, 42, ['module'], 'builtins'):
    G.__module__ = module
    print('module', repr(G), repr(A), repr(x).split(' object at ')[0])
G.__module__ = 'typing'
print('snapshot', A.__module__)
