# 真实list继承的根iter与用户next共同满足Iterator结构检查。
from collections.abc import Iterator, MutableSequence, Callable
class NextList(list):
    def __next__(self):
        return 7
    def __call__(self):
        return 9
value = NextList([1])
print(isinstance(value, Iterator))
print(issubclass(NextList, Iterator))
print(next(value))
print(isinstance(value, MutableSequence), issubclass(NextList, MutableSequence))
print(isinstance(value, Callable), value())
