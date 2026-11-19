# ryupy: pending
# 所有非参数名字都经本函数globals和捕获builtins活查；保留函数到正常退出。
import builtins

def f(x):
    return abs

def g(x):
    return f

print(f(x=None) is abs)
print(g(None) is f)
saved = builtins.abs
builtins.abs = len
print(f(None) is len)
builtins.abs = saved
globals()["abs"] = len
print(f(None) is len)
globals()["f"] = len
print(g(None) is len)
