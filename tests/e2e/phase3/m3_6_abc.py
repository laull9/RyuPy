# ABC 检查不调用协议；仅有 __getitem__ 的序列后备不自动成为 Iterable/Sequence。
from collections import Counter, defaultdict, deque
from collections.abc import Iterable, Iterator, Sized, Container, Collection
from collections.abc import Mapping, MutableMapping, Sequence, MutableSequence, Set, Callable
from itertools import repeat

for value in [[], (), "a", b"a", range(2), deque()]:
    print(isinstance(value, Iterable), isinstance(value, Sized), isinstance(value, Sequence))
print(isinstance({}, Mapping), isinstance(defaultdict(list), MutableMapping), isinstance(Counter(), Mapping))
print(isinstance(set(), Set), isinstance(repeat(1), Iterator), isinstance(lambda: 1, Callable))
print(issubclass(list, Sequence), issubclass(dict, Mapping), issubclass(MutableSequence, Collection))

class Protocol:
    def __iter__(self):
        print("must not iterate")
        return iter([])
    def __len__(self):
        print("must not measure")
        return 0
    def __contains__(self, value):
        print("must not search")
        return False

value = Protocol()
print(isinstance(value, Iterable), isinstance(value, Sized), isinstance(value, Container), isinstance(value, Collection))
print(isinstance(value, Sequence), isinstance(value, Mapping), issubclass(Protocol, Iterable))
class Fallback:
    def __getitem__(self, key):
        raise IndexError
print(isinstance(Fallback(), Iterable), isinstance(Fallback(), Sequence))
class Disabled:
    __iter__ = None
print(isinstance(Disabled(), Iterable))
from collections.abc import Generator
def generate():
    yield 1
print(isinstance(generate(), Generator), isinstance(repeat(1), Generator))
