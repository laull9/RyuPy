# 无暂停和跨暂停的星号容器都必须保持tuple/set类型，不能泄漏构建临时list。
value = (1, *[2, 3], 4)
print(value, type(value).__name__)
value = {1, *[1, 2], 3}
print(sorted(value), type(value).__name__)
print((*[],), {*[]})


def tuple_values(items):
    return (*items, (yield "tuple"), *(yield "尾部"))


items = [1, 2]
iterator = tuple_values(items)
print(next(iterator))
items.append(3)
print(iterator.send(4))
try:
    iterator.send([5, 6])
except StopIteration as error:
    print(error.value, type(error.value).__name__)


def set_values(items):
    return {0, *items, (yield "set"), *(yield "尾集合")}


items = [1, 1, 2]
iterator = set_values(items)
print(next(iterator))
items.append(9)
print(iterator.send(2))
try:
    iterator.send([3, 3])
except StopIteration as error:
    print(sorted(error.value), type(error.value).__name__)


# 首star之前的元素先全部求值再建立set；之后逐个插入，star迭代与hash交错。
log = []


class Key:
    def __init__(self, value):
        self.value = value

    def __hash__(self):
        log.append(("hash", self.value))
        return self.value

    def __eq__(self, other):
        return self.value == other.value


def key(value):
    log.append(("值", value))
    return Key(value)


def source():
    log.append("取源")
    yield key(3)
    log.append("源恢复")
    yield key(4)


value = {key(1), key(2), *source(), key(5)}
print(log, len(value))


log.clear()


def set_prefix():
    return {key(1), (yield "前缀"), *source()}


iterator = set_prefix()
print(next(iterator), log)
try:
    iterator.send(Key(2))
except StopIteration as error:
    print(log, len(error.value))


# 非迭代对象/不可哈希元素必须在后续yield之前报错，不能恢复成list继续执行。
def invalid_source():
    return {*1, (yield "不应暂停")}


def invalid_item():
    return {*[[]], (yield "不应暂停")}


for iterator in [invalid_source(), invalid_item()]:
    try:
        next(iterator)
    except TypeError:
        print("暂停前拒绝非法展开")
    print(next(iterator, "错误后耗尽"))


# 集合源的普通内建键仍保持去重，不改变结果类型。
print(sorted({0, *{1, 2}, *frozenset([2, 3])}))
