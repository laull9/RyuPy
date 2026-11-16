# 数量探测、失败后游标、目标副作用与暂停恢复都由真实差分约束。
events = []
class Cursor:
    def __init__(self, values):
        self.values = values
        self.index = 0
    def __iter__(self):
        events.append('iter')
        return self
    def __len__(self):
        raise AssertionError('len called')
    def __length_hint__(self):
        raise AssertionError('hint called')
    def __next__(self):
        events.append(self.index)
        if self.index == len(self.values):
            raise StopIteration
        value = self.values[self.index]
        self.index += 1
        return value

for values in [[], [1], [1, 2], [1, 2, 3, 4]]:
    events.clear()
    cursor = Cursor(values)
    try:
        a, b = cursor
        print('exact', a, b)
    except ValueError as error:
        print('count', str(error))
    print('events', events)
    print('remaining', next(cursor, 'done'))

empty = Cursor([])
[] = empty
print('empty target', next(empty, 'done'))
try:
    [] = Cursor([9, 10])
except ValueError as error:
    print('empty extra', str(error))

class Poison:
    def __init__(self):
        self.index = 0
    def __iter__(self):
        return self
    def __next__(self):
        self.index += 1
        if self.index == 4:
            raise RuntimeError('tail remains')
        return self.index

poison = Poison()
try:
    a, b = poison
except ValueError as error:
    print('bounded', str(error), poison.index)
try:
    next(poison)
except RuntimeError as error:
    print('tail', str(error), poison.index)

class Box:
    pass
box = Box()
box.first = -1
items = [0]
def target():
    events.append('target')
    return box
def rhs():
    events.append('rhs')
    return Cursor([7, 8])
events.clear()
target().first, items[0] = rhs()
print('ordered', box.first, items, events)
events.clear()
try:
    target().first, items[0] = Cursor([9])
except ValueError:
    print('prepare failure', box.first, items, events)
try:
    box.first, items[99] = Cursor([11, 12])
except IndexError:
    print('partial', box.first, items)
source = [21, 22]
source[1], result = iter(source)
print('frozen', source, result)
source = [31, 32]
def paused(values):
    box.first, items[(yield 'index')] = values
    yield box.first, items[0]
iterator = paused(iter(source))
print('pause', next(iterator), box.first)
source[1] = 99
print('resume', iterator.send(0), source)
def live():
    yield 1
    yield 2
    yield 3
    yield 4
stream = live()
try:
    a, b = stream
except ValueError:
    print('not closed', next(stream))

# 显式禁用迭代不能偷偷走getitem后备；旧PyIter完整错误文案仍待单独对齐。
fallback_calls = []
class Disabled:
    __iter__ = None
    def __getitem__(self, key):
        fallback_calls.append(key)
        return key

try:
    a, b = Disabled()
except TypeError as error:
    assert type(error) is TypeError
    assert fallback_calls == []
    print('disabled iter', type(error) is TypeError, fallback_calls)
