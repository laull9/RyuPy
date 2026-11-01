# 目标先读后写；暂停或普通RHS重绑定不能改变已选定的对象和下标。
items = [10, 20]
index = 0
old = items


def paused():
    items[index] += (yield "rhs")


it = paused()
print(next(it))
items = [100, 200]
index = 1
try:
    it.send(7)
except StopIteration:
    pass
print(old, items)


def rebind():
    global items, index
    items = [300, 400]
    index = 0
    return 5


old = items
items[index] += rebind()
print(old, items)


class Box:
    def __init__(self, value):
        self.value = value


box = Box(8)
previous = box


def attribute():
    box.value += (yield "attr")


it = attribute()
print(next(it))
box = Box(100)
try:
    it.send(2)
except StopIteration:
    pass
print(previous.value, box.value)


def nested():
    data = [1, 2]
    position = 0
    original = data

    def change():
        nonlocal data, position
        data = [30, 40]
        position = 1

    def work():
        data[position] += (yield "cell")

    it = work()
    print(next(it))
    change()
    try:
        it.send(6)
    except StopIteration:
        pass
    print(original, data)


nested()


def target_steps():
    (yield "base")[(yield "index")] += (yield "value")


data = [9]
it = target_steps()
print(next(it), it.send(data), it.send(0))
try:
    it.send(4)
except StopIteration:
    pass
print(data)

# 下标协议读写使用同一个切片对象，读取在暂停前发生。
log = []


class Indexed:
    def __getitem__(self, key):
        self.key = key
        log.append("get")
        return 10

    def __setitem__(self, key, value):
        print("同一个key", key is self.key, value)
        log.append("set")


def bound(value):
    log.append(value)
    return value


holder = Indexed()


def indexed_target():
    holder[bound(1):bound(4):bound(2)] += (yield "indexed")


it = indexed_target()
print(next(it), log)
try:
    it.send(3)
except StopIteration:
    pass
print(log)

# 切片各分量求值一次，切片对象与原容器共同跨暂停保留。
sequence = [1, 2, 3]
original = sequence
start = 0
stop = 2


def slice_target():
    sequence[start:stop] += (yield "slice")


it = slice_target()
print(next(it))
sequence = [8, 9]
start = 1
stop = 1
try:
    it.send([4])
except StopIteration:
    pass
print(original, sequence)

# 左侧读取失败应发生在RHS暂停之前。
def missing_target():
    [][0] += (yield "不应出现")


it = missing_target()
try:
    next(it)
except IndexError:
    print("先读失败")
print(next(it, "已关闭"))

# 原地操作已生效后，写回失败不能回滚list的修改。
inner = [1]
outer = (inner,)


def failed_write():
    outer[0] += (yield "写回")


it = failed_write()
print(next(it))
try:
    it.send([2])
except TypeError:
    print(inner, outer[0] is inner)
