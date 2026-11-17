# ryupy: pending
# 局部别名、形参重绑定、嵌套身份条件和早返回均由真实静态CIL体执行。
fallback = object()
other = object()

def choose(x):
    alias = x
    if alias is None:
        alias = fallback
    elif alias is other:
        return x
    else:
        x = alias
    return alias if x is None else x

def compare(x):
    alias = x
    return alias is x

def negate(x):
    return x is not None

def implicit(x):
    if x is None:
        return
    alias = x
    pass

print(choose(None) is fallback)
print(choose(other) is other)
value = []
print(choose(value) is value)
print(compare(value), negate(None), negate(value))
print(implicit(None) is None, implicit(value) is None)
globals()["fallback"] = other
print(choose(None) is other)
