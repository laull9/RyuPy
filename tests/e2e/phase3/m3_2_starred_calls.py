# 混合位置项必须在各star位置立即展开；唯一star位置项按CPython延至调用。
def collect(*args, **kwargs):
    return args, kwargs


items = [1]


def mixed():
    return collect(*items, (yield "arg"))


it = mixed()
print(next(it))
items.append(2)
try:
    it.send(9)
except StopIteration as error:
    print(error.value)

items = [3]


def single():
    return collect(*items, flag=(yield "keyword"))


it = single()
print(next(it))
items.append(4)
try:
    it.send(8)
except StopIteration as error:
    print(error.value)

# 多star依次展开；yield产生的源展开后才能继续求下一参数。
log = []


def source(number):
    log.append("start" + str(number))
    yield number
    log.append("end" + str(number))


def stars():
    return collect(*source(1), *(yield "source"), *source(3), flag=(yield "flag"))


it = stars()
print(next(it), log)
print(it.send(source(2)), log)
try:
    it.send(7)
except StopIteration as error:
    print(error.value, log)

# 非iterable混合star必须在后续yield之前失败。
def invalid():
    return collect(*1, (yield "不应出现"))


try:
    next(invalid())
except TypeError:
    print("先展开失败")

# 方法绑定仍在所有参数之前；暂停期间替换方法不改变此次绑定。
class Receiver:
    def work(self, *args):
        return "old", args


receiver = Receiver()


def method():
    return receiver.work(*[1], (yield "method"))


it = method()
print(next(it))
Receiver.work = lambda self, *args: ("new", args)
try:
    it.send(2)
except StopIteration as error:
    print(error.value)

# 内建通用调用亦须展开早于后参恢复。
def builtin():
    print(*["prefix"], (yield "print"))


it = builtin()
print(next(it))
try:
    it.send("suffix")
except StopIteration:
    pass

# 唯一star的非法源直到关键字恢复后才失败，不能一律提前展开。
def delayed_invalid():
    return collect(*1, flag=(yield "先关键字"))


it = delayed_invalid()
print(next(it))
try:
    it.send(0)
except TypeError:
    print("后展开失败")

# 源码关键字写在star前，Python仍先准备位置组再求关键字组。
log.clear()


def keyword_before_star():
    return collect(flag=(yield "kw"), *source(5), *source(6))


it = keyword_before_star()
print(next(it), log)
try:
    it.send(7)
except StopIteration as error:
    print(error.value)

# 非callable目标在参数全部准备后才报错，不能绑定时过早失败。
log.clear()
not_callable = 7


def bad_callable():
    return not_callable(*source(8), *source(9), flag=(yield "先准备"))


it = bad_callable()
print(next(it), log)
try:
    it.send(0)
except TypeError:
    print("最后调用失败")
