# ryupy: pending
# 精确消费：自定义 orig_bases 相等/迭代及 generic helper 改绑不提前拒绝。
import typing
from typing import Generic, TypeVar
T = TypeVar('T')
U = TypeVar('U')
A = Generic[T]
class Pair(Generic[T, U]):
    pass
print('pair', Pair.__parameters__ == (T, U))
class Item:
    def __eq__(self, other):
        print('orig-eq', other is Generic)
        return False
    @property
    def __parameters__(self):
        print('orig-parameters')
        return (U,)
class CustomOrig(Generic):
    print('custom-orig-body')
    __orig_bases__ = (Item(),)
print('custom-orig-result', CustomOrig.__parameters__)
class Iterable:
    def __iter__(self):
        print('orig-iter')
        return iter((A,))
class IterableOrig(Generic):
    print('iterable-body')
    __orig_bases__ = Iterable()
print('iterable-result', IterableOrig.__parameters__)
class Descriptor:
    def __set_name__(self, owner, name):
        print('WRONG set-name after failed body')
try:
    class Failed(A):
        field = Descriptor()
        print('failed-body')
        raise ValueError('before finalize')
except ValueError as error:
    print('failed', str(error))
old_collect = typing._collect_parameters
try:
    def replacement(args):
        print('collect-replacement', args[0] is A)
        return (T,)
    typing._collect_parameters = replacement
    print('collect-stored')
    class Changed(A):
        print('collect-body')
    print('collect-result', Changed.__parameters__ == (T,))
finally:
    typing._collect_parameters = old_collect
# body 改绑模块 Generic：alias 解析已完成，hook 的 super(Generic, cls) 才消费新导出。
try:
    class ExportChanged(A):
        print('export-body')
        typing.Generic = object
except Exception as error:
    print('export-error', type(error).__name__, str(error))
finally:
    typing.Generic = Generic
