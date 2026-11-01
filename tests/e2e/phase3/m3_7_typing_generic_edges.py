# ryupy: pending
# 类体与别名调用均有可观察副作用；不能只记录参数而略过创建错误。
from typing import Generic, TypeVar

T = TypeVar('T')
try:
    class Bare(Generic):
        print('裸 Generic 类体已执行')
except TypeError:
    print('裸 Generic 创建失败')

for value in [int, (T, T)]:
    try:
        Generic[value]
    except TypeError:
        print('非法 Generic 参数')
try:
    Generic[T][int]
except TypeError:
    print('Generic 别名不能再次订阅')


class Box(Generic[T]):
    pass


print(Box[T][int] == Box[int])
try:
    Box[int][str]
except TypeError:
    print('具体别名不能再次订阅')


class Plain:
    pass


try:
    Plain[int]
except TypeError:
    print('普通类不能订阅')


class MetadataFailure(Generic[T]):
    @property
    def __orig_class__(self):
        return None

    @__orig_class__.setter
    def __orig_class__(self, value):
        raise ValueError('metadata')


outer = ValueError('outer')
try:
    raise outer
except ValueError:
    instance = MetadataFailure[int]()
    print(type(instance) is MetadataFailure)
    try:
        raise
    except ValueError as active:
        print(active is outer)
    try:
        raise RuntimeError('after')
    except RuntimeError as after:
        print(after.__context__ is outer)
