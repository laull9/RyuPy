# ryupy: pending
# 父任务要求的有界 own orig 条目矩阵，特别记录旧 List[T] 与 TypeVar 身份。
from typing import Generic, TypeVar, List
T = TypeVar('T')
A = Generic[T]
values = [(), (A,), (int,), ('bad',), (T,), (List[T],)]
for index, value in enumerate(values):
    class Bare(Generic):
        __orig_bases__ = value
    print('entry', index, Bare.__orig_bases__ is value, len(Bare.__parameters__))
    if Bare.__parameters__:
        print('parameter-is-T', Bare.__parameters__[0] is T)
