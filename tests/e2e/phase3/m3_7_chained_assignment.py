# RHS一次、目标左到右且轮到才求目标对象/下标；前目标失败阻止后续目标。
log = []
class Box:
    pass
box = Box()
class Store:
    def __setitem__(self, key, value):
        log.append(('store', key))
        self.saved = value
store = Store()
def rhs():
    log.append('rhs')
    return []
def target():
    log.append('target')
    return box
def container():
    log.append('container')
    return store
def index():
    log.append('index')
    return 3

target().value = container()[index()] = saved = rhs()
print(log, box.value is store.saved, saved is box.value)

log.clear()
try:
    ()[0] = target().value = rhs()
except TypeError:
    print(log)

def fail():
    log.append('fail')
    raise ValueError('rhs')
log.clear()
try:
    target().value = container()[index()] = fail()
except ValueError:
    print(log)

items = [0, 0]
i = 0
i = items[i] = 1
print(i, items)
a = a.value = Box()
print(a is a.value)

def typed() -> bool:
    left = right = 1000000
    return left is right
print(typed())

def closure():
    left = right = []
    def captured():
        return left
    return right, captured
right, captured = closure()
print(right is captured())

# 多个解包目标仍复用既有平坦解包能力，不新增嵌套解包语义。
whole = left, *middle, right = [1, 2, 3, 4]
print(whole, left, middle, right)

first = None
items = [None, None]
original_items = items
def paused():
    global first
    first = items[(yield 'index')] = rhs()
    return first
log.clear()
it = paused()
print(next(it), log, first is not None)
saved = first
items = ['replacement']
try:
    it.send(1)
except StopIteration as error:
    print(error.value is saved, original_items[1] is saved, items, log)
