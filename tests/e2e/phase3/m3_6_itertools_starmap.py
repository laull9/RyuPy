# starmap 真实惰性调用、逐行错误恢复及重入，不把空输入的 callable 提前验证。
from itertools import starmap

def add(a, b):
    return a + b


print(list(starmap(add, [(1, 2), [3, 4]])))
print(list(starmap(None, [])))
rows = [[1, 2]]
s = starmap(add, rows)
rows.append([3, 4])
print(iter(s) is s, type(s) is starmap, isinstance(s, starmap))
print(next(s), next(s), next(s, 'end'))

def work(x):
    if x == 1:
        raise ValueError('row')
    if x == 3:
        raise StopIteration('stop-row')
    return x * 10

s = starmap(work, [(1,), (2,), (3,), (4,), 9, (5,)])
try:
    next(s)
except ValueError as error:
    print(str(error))
print(next(s))
try:
    next(s)
except StopIteration as error:
    print(error.value)
print(next(s))
try:
    next(s)
except TypeError:
    print('bad row')
print(next(s), next(s, 'end'))

s = starmap(work, [(3,), (2,)])
print(list(s), list(s))

def recursive(a, b):
    if a == 1:
        print('nested', next(nested))
    return a + b

nested = starmap(recursive, [(1, 2), (3, 4)])
print(next(nested), next(nested, 'end'))
for args in [(), (add,), (add, [], 3)]:
    try:
        starmap(*args)
    except TypeError:
        print('arity')
try:
    starmap(add, iterable=[])
except TypeError:
    print('keyword')

# 非永久耗尽的用户游标：starmap 不额外缓存用户 StopIteration。
class Rows:
    def __init__(self):
        self.i = 0
    def __iter__(self):
        return self
    def __next__(self):
        self.i += 1
        if self.i == 1:
            raise StopIteration('source-stop')
        return (self.i,)

s = starmap(work, Rows())
try:
    next(s)
except StopIteration as error:
    print(error.value)
print(next(s))
