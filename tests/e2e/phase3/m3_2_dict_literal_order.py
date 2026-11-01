# 小字典先求所有key/value再hash；暂停也不能提前插入前缀。
log = []


class Key:
    def __init__(self, value):
        self.value = value

    def __hash__(self):
        log.append("hash" + str(self.value))
        return self.value

    def __eq__(self, other):
        log.append("eq")
        return self.value == other.value


def key(value):
    log.append("key" + str(value))
    return Key(value)


def value(number):
    log.append("value" + str(number))
    return number


result = {key(1): value(1), key(2): value(2)}
print(log)
log.clear()


def paused():
    return {key(1): value(1), key(2): (yield "value"), (yield "key"): value(3)}


it = paused()
print(next(it), log)
print(it.send(2), log)
try:
    it.send(Key(3))
except StopIteration as error:
    print(list(error.value.values()), log)

# 无法hash的早期key仍应先完成小字典其余值求值。
log.clear()
try:
    result = {[]: value(1), key(2): value(2)}
except TypeError:
    print(log)

# 后续表达式抛错时，前面的key尚未hash。
log.clear()
try:
    result = {key(1): 1, key(2): 1 // 0}
except ZeroDivisionError:
    print(log)

# 重复键替换末值但保留首位置，插入时顺序不变。
log.clear()
result = {key(1): value(1), key(2): value(2), key(1): value(3)}
print(list(result.values()), log)

# 15/16对边界：固定CPython小字典批量hash，16–17对逐对插入。
log.clear()
result = {
    key(1): value(1), key(2): value(2), key(3): value(3),
    key(4): value(4), key(5): value(5), key(6): value(6),
    key(7): value(7), key(8): value(8), key(9): value(9),
    key(10): value(10), key(11): value(11), key(12): value(12),
    key(13): value(13), key(14): value(14), key(15): value(15),
}
print(len(result), log[:6], log[-3:])
log.clear()


def large():
    return {
        key(1): value(1), key(2): (yield "large"), key(3): value(3),
        key(4): value(4), key(5): value(5), key(6): value(6),
        key(7): value(7), key(8): value(8), key(9): value(9),
        key(10): value(10), key(11): value(11), key(12): value(12),
        key(13): value(13), key(14): value(14), key(15): value(15),
        key(16): value(16),
    }


it = large()
print(next(it), log)
try:
    it.send(2)
except StopIteration as error:
    print(len(error.value), log[:7], log[-3:])

# 16项字典首项hash失败：后面的表达式/暂停都不能执行。
log.clear()


def large_error():
    return {
        []: value(1), key(2): (yield "不应出现"), 3: 3, 4: 4,
        5: 5, 6: 6, 7: 7, 8: 8, 9: 9, 10: 10, 11: 11, 12: 12,
        13: 13, 14: 14, 15: 15, 16: 16,
    }


try:
    next(large_error())
except TypeError:
    print(log)

# 18+纯标量字面量键没有用户hash/eq，直接插入仍保留结果和值求值顺序。
def scalar_large():
    return {0: value(0), True: 1, "x": 2, None: 3, 4: 4, 5: 5,
            6: 6, 7: 7, 8: 8, 9: 9, 10: 10, 11: 11, 12: 12,
            13: 13, 14: 14, 15: 15, 16: (yield "scalar"), 1: 99}


log.clear()
it = scalar_large()
print(next(it), log)
try:
    it.send(42)
except StopIteration as error:
    print(len(error.value), error.value[True], error.value[16], list(error.value.keys())[:4])

# 模块名字读取要保存值，而非暂停后重新读取绑定。
original_key = "old"
original_value = [1]


def names():
    return {original_key: original_value, "later": (yield "names")}


it = names()
print(next(it))
original_key = "new"
original_value = [9]
try:
    it.send(2)
except StopIteration as error:
    print(error.value)
