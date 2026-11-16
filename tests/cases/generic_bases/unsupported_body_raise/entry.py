# 全新有限裸根终结失败合同：原异常、副作用与旧公开绑定均不能丢。
from typing import Generic, TypeVar
T = TypeVar('T')
A = Generic[T]
Old = 'old'
try:
    class Old(Generic):
        print('bare-body')
except TypeError as error:
    print('bare-error', str(error))
assert Old == 'old'
try:
    class Old(Generic):
        print('raising-body')
        raise ValueError('body wins')
except ValueError as error:
    print('body-error', str(error))
assert Old == 'old'
for value in [(Generic,), None, 1, (A, A)]:
    try:
        class Bad(Generic):
            __orig_bases__ = value
            print('metadata-body')
    except TypeError as error:
        print('metadata-error', str(error))
class Parent(A):
    pass
class Empty(Parent):
    __orig_bases__ = ()
    __parameters__ = ('discard',)
assert Empty.__parameters__ == () and Empty.__orig_bases__ == ()
try:
    class BadChild(Parent):
        __orig_bases__ = (Generic,)
        print('child-body')
except TypeError as error:
    print('child-error', str(error))
print('old-binding-empty', Old, True)
