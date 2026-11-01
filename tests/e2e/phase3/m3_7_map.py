# map 构造不消费、逐项左到右、最短输入及异常后恢复。
print(list(map(int, ['1', '2'])))
print(''.join(map(str, [1, 2, 3])))
log = []
class Source:
    def __init__(self, name, values):
        self.name = name
        self.values = iter(values)
    def __iter__(self):
        log.append('iter ' + self.name)
        return self
    def __next__(self):
        log.append('next ' + self.name)
        return next(self.values)
m = map(lambda a, b: a + b, Source('a', [1, 2, 3]), Source('b', [10]))
print(log)
print(iter(m) is m, type(m).__name__, isinstance(m, map), type(m) is map)
print(next(m))
print(next(m, 'end'))
print(next(m, 'end'))
print(log)

def fail_once(x):
    if x == 1:
        raise ValueError('first')
    if x == 3:
        raise StopIteration('pause')
    return x
m = map(fail_once, [1, 2, 3, 4])
for _ in range(4):
    try:
        print(next(m))
    except ValueError:
        print('value')
    except StopIteration:
        print('stop')
print(list(map(None, [])))
m = map(None, [1, 2])
for _ in range(2):
    try:
        next(m)
    except TypeError:
        print('call error')
item = []
print(next(map(lambda x: x, [item])) is item)
# 同一 map 重入仍使用各自参数帧，外层余下实参不能被内层覆盖。
m = None
def recursive(x):
    if x == 1:
        return (x, next(m))
    return x
m = map(recursive, [1, 2, 3])
print(next(m), next(m))
for args in [(), (str,), (str, 42)]:
    try:
        map(*args)
    except TypeError:
        print('construct error')
try:
    map(str, [1], strict=True)
except TypeError:
    print('keyword error')
class Resumable:
    def __init__(self):
        self.n = 0
    def __iter__(self):
        return self
    def __next__(self):
        self.n += 1
        if self.n == 1:
            raise StopIteration('source pause')
        return self.n
m = map(lambda x: x, Resumable())
try:
    next(m)
except StopIteration as error:
    print(error.value)
print(next(m))
m = map(fail_once, [3, 4])
try:
    next(m)
except StopIteration as error:
    print(error.value)
print(next(m))
# 输入端异常可恢复，生成器终值也不能在 bool 适配层被吞掉。
class Faulty:
    def __init__(self):
        self.n = 0
    def __iter__(self):
        return self
    def __next__(self):
        self.n += 1
        if self.n == 1:
            raise ValueError('input')
        return self.n
m = map(str, Faulty())
try:
    next(m)
except ValueError:
    print('input error')
print(next(m))
def source():
    yield 1
    return 'source result'
m = map(str, source())
print(next(m))
try:
    m.__next__()
except StopIteration as error:
    print(error.value)
# 构造即按顺序取得输入迭代器，失败后不访问后续输入。
log.clear()
try:
    map(str, Source('first', []), 42, Source('unreached', []))
except TypeError:
    print(log)
# 实参帧清理不影响逃逸的 *args 元组；嵌套 map 保留内层终值。
m = map(lambda *args: args, [1, 2], [3, 4])
first = next(m)
second = next(m)
print(first, second)
m = map(str, map(fail_once, [3, 4]))
try:
    next(m)
except StopIteration as error:
    print(error.value)
print(next(m))
from collections.abc import Iterable, Iterator, Collection
print(issubclass(map, Iterator), issubclass(map, Iterable), issubclass(map, Collection))
print(isinstance(map(str, []), Iterator))
# 后一个输入的 __next__ 重入，不能覆盖外层已准备的前一个参数。
class ReentrantSource:
    def __init__(self):
        self.n = 0
    def __iter__(self):
        return self
    def __next__(self):
        self.n += 1
        value = self.n
        if value == 1:
            print('inner', next(m))
        return value
m = map(lambda a, b: a + b, [10, 20, 30], ReentrantSource())
print('outer', next(m), next(m))
