# M2.1：完整的参数绑定与错误消息。

def bind(a, /, b, *rest, c=3, **kw):
    return a, b, rest, c, sorted(kw.items())


print(bind(1, 2))
print(bind(1, 2, 3, 4, c=5, z=9))
print(bind(*[1, 2], **{"c": 7, "y": 8}))

# 只按位置传入的参数不能写成关键字
try:
    bind(a=1, b=2)
except TypeError as error:
    print("posonly:", error)

# 重复绑定
try:
    bind(1, 2, b=3)
except TypeError as error:
    print("duplicate:", error)

# 未知关键字
def simple(x):
    return x

try:
    simple(1, y=2)
except TypeError as error:
    print("unknown:", error)

# 缺少必需参数
try:
    bind(1)
except TypeError as error:
    print("missing:", error)

# 过多位置参数
try:
    simple(1, 2)
except TypeError as error:
    print("too many:", error)

# 默认值只在定义时求值一次（可变默认值共享）
def remember(value, history=[]):
    history.append(value)
    return history

print(remember(1), remember(2), remember(3))

# 一等函数：作为实参传递、作为返回值
def twice(f):
    return lambda_free(f)


def lambda_free(f):
    return f(2) + f(2)


def inc(x):
    return x + 1

print(twice(inc), callable(inc), callable(3))
