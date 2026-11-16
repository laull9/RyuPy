# 值准备与目标求值分离：失败不写目标，目标失败不回滚，暂停只保存真实引用。
events = []
class Cursor:
    def __init__(self, values):
        self.values = values
        self.index = 0
    def __iter__(self):
        events.append('iter')
        return self
    def __length_hint__(self):
        events.append('hint')
        return 0
    def __next__(self):
        index = self.index
        self.index += 1
        events.append(['next', index])
        if index >= len(self.values):
            raise StopIteration
        return self.values[index]

source = [10, 20, 30, 40]
class Target:
    def __init__(self):
        self.saved = []
    def __setitem__(self, key, value):
        events.append(['store', key, value[:] if isinstance(value, list) else value])
        if key == 'first':
            source[:] = [99]
        if key == 'middle':
            value.clear()
        if key == 'fail':
            raise RuntimeError('setter failed')
        self.saved.append([key, value])
target = Target()
def destination():
    events.append('target')
    return target
def key(value):
    events.append(['key', value])
    return value
def rhs():
    events.append('rhs')
    return Cursor(source)
destination()[key('first')], *destination()[key('middle')], destination()[key('last')] = rhs()
print('frozen', target.saved, source, events)
events.clear()
try:
    destination()[key('first')], *destination()[key('fail')], destination()[key('never')] = Cursor([1, 2, 3, 4])
except RuntimeError as error:
    print('partial', str(error), target.saved, events)
events.clear()
try:
    destination()[key('never')], *destination()[key('never')], destination()[key('never')] = Cursor([1])
except ValueError as error:
    print('prepare failure', str(error), target.saved, events)

class Box:
    pass
box = Box()
box.first = 'old first'
box.rest = ['old rest']
items = ['old last']
try:
    box.first, *box.rest, items[0] = Cursor([])
except ValueError:
    print('untouched', box.first, box.rest, items)

# 最后目标的base在index暂停前已求值；切换外部绑定不能重选base。
first_leaf, second_leaf, third_leaf, last_leaf = [10], [20], [30], [40]
source = [first_leaf, second_leaf, third_leaf, last_leaf]
left = [None]
right = [None]
selected = left
def choose():
    events.append('choose')
    return selected
def paused(values):
    head, *box.rest, choose()[(yield 'index')] = values
    yield head, box.rest
cursor = Cursor(source)
generator = paused(cursor)
events.clear()
print('pause', next(generator), cursor.index, box.rest, events)
saved_middle = box.rest
saved_middle.append(['added'])
source[3] = ['replacement']
selected = right
resumed = generator.send(0)
print('resume', resumed[0] is first_leaf, resumed[1] is saved_middle,
      box.rest is saved_middle, saved_middle[0] is second_leaf,
      saved_middle[1] is third_leaf, saved_middle,
      left[0] is last_leaf, right, cursor.index, events)
generator.close()

# 星号目标自己暂停，发布middle后再暂停suffix；清空middle不能改变已冻结后项。
source = [first_leaf, second_leaf, third_leaf, last_leaf]
saved = {}
def paused_middle(values):
    box.first, *saved[(yield 'middle')], saved[(yield 'last')] = values
    yield 'done'
cursor = Cursor(source)
generator = paused_middle(cursor)
events.clear()
print('middle pause', next(generator), cursor.index, box.first is first_leaf, saved, events)
source[:] = [['new'], ['source']]
print('last pause', generator.send('middle'), cursor.index, events)
saved_middle = saved['middle']
print('middle identity', type(saved_middle) is list, saved_middle[0] is second_leaf,
      saved_middle[1] is third_leaf)
saved_middle.clear()
print('middle resume', generator.send('last'), saved['middle'] is saved_middle,
      saved['last'] is last_leaf, saved, cursor.index, events)
generator.close()

# 原来的直接字面量求值和非星号swap仍保持其顺序。
events.clear()
def value(number):
    events.append(number)
    return number
first, *middle, last = (value(1), value(2), value(3), value(4))
a, b = value(5), value(6)
a, b = b, a
print('literal', first, middle, last, a, b, events)
