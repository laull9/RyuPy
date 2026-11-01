# 平坦解包先收齐值，再从左到右求目标；失败不回滚已完成的写入。
class Box:
    pass

box = Box()
box.first, box.second = [1, 2]
print(box.first, box.second)
values = [10, 20]
values[1], result = values
print(values, result)
log = []

def target():
    log.append('target')
    return box

def index():
    log.append('index')
    return 0

items = [0]
target().third, items[index()] = [3, 4]
print(log, box.third, items)
log.clear()
try:
    target().third, items[index()] = [9]
except ValueError:
    print('unpack failed', log, box.third, items)
try:
    box.first, items[99], box.second = [7, 8, 9]
except IndexError:
    print('partial', box.first, box.second)
box.first, *box.middle, box.second = (1, 2, 3, 4)
print(box.first, box.middle, box.second)
*items[0], box.last = [5, 6, 7]
print(items, box.last)
for box.first, items[0] in [(11, 12), (13, 14)]:
    print(box.first, items)

def run():
    cell = Box()
    def assign():
        cell.value, other = [21, 22]
        return other
    print(assign(), cell.value)
run()

def suspended():
    box.first, items[(yield 'index')] = [31, 32]
    yield box.first, items
it = suspended()
print(next(it), box.first)
print(it.send(0))
# 名字目标先赋值，后续下标表达式必须看到该新值。
i = 0
items = [0, 0]
i, items[i] = [1, 41]
print(i, items)
# 星号列表同样在写入前快照，不能受前一个目标修改源序列影响。
source = [1, 2, 3]
source[1], *box.middle = source
print(source, box.middle)
# 切片叶目标沿用普通下标赋值的切片降级，不把 Slice 误当普通表达式。
items = [0, 0, 0]
items[:], box.last = [[1, 2], 9]
print(items, box.last)
*items[:], box.last = [3, 4, 5]
print(items, box.last)
log.clear()
def part(value):
    log.append(value)
    return value
items[part(0):part(2):part(1)], box.last = [[6, 7], 8]
print(items, box.last, log)
