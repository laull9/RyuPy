# ryupy: pending
# ABC公开身份、普通名义子类、结构检查与PEP585不得在迁移时拆成多个代理身份。
import collections.abc as abc
from collections.abc import Sequence, Iterable, Sized, MutableSequence

class Example(Sequence):
    def __len__(self):
        return 2
    def __getitem__(self, index):
        return (10, 20)[index]

obj = Example()
print(Example.__bases__[0] is Sequence, Sequence in Example.__mro__)
print(isinstance(obj, Sequence), isinstance([], Sequence), isinstance([], Example))
print(issubclass(Example, Sequence), issubclass(list, Sequence), issubclass(list, Example))
print(issubclass(MutableSequence, Sequence), issubclass(MutableSequence, Sized))
print(Sequence.__module__, Sequence.__name__, Sequence.__qualname__)
print(Sequence, Sequence[int])
print(Sequence[int].__origin__ is Sequence, Sequence[int].__args__ == (int,))
print(Iterable[str].__origin__ is Iterable)

class Protocol:
    def __iter__(self):
        print('must not call')
        return iter([])
    def __len__(self):
        print('must not call')
        return 0
print(isinstance(Protocol(), Iterable), isinstance(Protocol(), Sized), isinstance(Protocol(), Sequence))

class Fake:
    pass
saved = abc.Sequence
abc.Sequence = Fake
print(isinstance([], saved), isinstance([], abc.Sequence), Example.__bases__[0] is saved)
abc.Sequence = saved

class Incomplete(Sequence):
    def __len__(self):
        return 0
try:
    Incomplete()
except TypeError:
    print('inherited abstract')
