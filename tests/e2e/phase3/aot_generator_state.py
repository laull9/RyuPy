# 驻留状态生成器：暂停局部、表达式临时、发送、别名与错误关闭。
def count(n):
    i = 0
    while i < n:
        yield i
        i += 1
    return 99

g = count(3)
print(next(g), next(g), next(g))
try:
    next(g)
except StopIteration as error:
    print(error.value)
print(next(g, "done"))

def values(a):
    alias = a
    sent = yield a
    a.append(sent)
    yield alias
    yield 10 + (yield 20)

items = []
v = values(items)
print(next(v) is items)
print(v.send(3) is items, items)
print(next(v), v.send(7))
v.close()
print(next(v, "closed"))

bad = count(1)
try:
    bad.send(8)
except TypeError:
    print("initial send")
print(next(bad))
try:
    bad.throw(ValueError("injected"))
except ValueError as error:
    print(str(error))
print(next(bad, "closed"))

def error():
    yield 1
    raise StopIteration("body")
e = error()
print(next(e))
try:
    next(e)
except RuntimeError as exception:
    print(str(exception))

# EH与委托继续使用旧载体，混合嵌套仍消费同一生成器协议。
def delegated():
    try:
        yield from count(2)
    finally:
        print("finally")
print(list(delegated()))

# 动态整数的同一盒必须跨两次yield保留。
def identity(value):
    yield value
    yield value
number = int("1000000")
it = identity(number)
a = next(it)
print(a is number, next(it) is a)

# 重入由公共PyGenerator控制，回调的EH不改变当前生成器的表示。
def reenter(iterator):
    try:
        next(iterator)
    except ValueError as error:
        return str(error)
def callback(holder):
    yield reenter(holder[0])
holder = []
it = callback(holder)
holder.append(it)
print(next(it))
it.close()
