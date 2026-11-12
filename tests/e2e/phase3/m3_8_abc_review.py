# ryupy: pending
# ABC审查回归：冻结抽象集合、mixin返回与生成器异常终态。
from collections.abc import Iterable, Sequence, MutableSequence, MutableSet, ItemsView

class Concrete(Iterable):
    def __iter__(self):
        return iter(())
Concrete.__iter__ = Iterable.__iter__
class Child(Concrete):
    pass
print(sorted(Concrete.__abstractmethods__), sorted(Child.__abstractmethods__))
print(list(Child()))
class Extra(Concrete):
    extra = Iterable.__iter__
print(sorted(Extra.__abstractmethods__))
class Ordinary:
    extra = Iterable.__iter__
print(hasattr(Ordinary, '__abstractmethods__'))

class MutableNumbers(MutableSequence):
    def __len__(self): return 0
    def __getitem__(self, i): raise IndexError(i)
    def __setitem__(self, i, value): pass
    def __delitem__(self, i): pass
    def insert(self, i, value): return 123
print(MutableNumbers().append(1))
class MutableNumbersSet(MutableSet):
    def __len__(self): return 1
    def __iter__(self): return iter((1,))
    def __contains__(self, value): return True
    def add(self, value): pass
    def discard(self, value): return 123
print(MutableNumbersSet().remove(1))

class BrokenLength(Sequence):
    def __len__(self): raise ValueError('length')
    def __getitem__(self, i): return i
rev = reversed(BrokenLength())
try:
    next(rev)
except ValueError as e:
    print(type(e).__name__, str(e))
print(next(rev, 'closed'))

class StopItems(Sequence):
    def __len__(self): return 1
    def __getitem__(self, i): raise StopIteration('user')
class StopMapping:
    def __len__(self): return 1
    def __iter__(self): return iter(('a',))
    def __getitem__(self, key): raise StopIteration('user')
for gen in (iter(StopItems()), iter(ItemsView(StopMapping()))):
    try:
        next(gen)
    except RuntimeError as e:
        print(str(e), type(e.__cause__).__name__, str(e.__cause__))
        print(e.__context__ is e.__cause__, e.__suppress_context__)
    print(next(gen, 'closed'))
