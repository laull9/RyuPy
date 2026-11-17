# ryupy: pending
# 全局读取必须来自本次真实模块字典；定义后重绑定仍可见。
value = 11

def f(x):
    return value

print(f(None))
globals()["value"] = 23
print(f(None))
print(f.__name__, f.__module__)
