# TYPE_CHECKING是运行时可重绑的值，不能按导入拼写无条件删除分支。
import typing
from typing import TYPE_CHECKING
print(TYPE_CHECKING, typing.TYPE_CHECKING)
typing.TYPE_CHECKING = True
print(TYPE_CHECKING, typing.TYPE_CHECKING)
if typing.TYPE_CHECKING:
    print('module rebound')
TYPE_CHECKING = True
if TYPE_CHECKING:
    print('alias rebound')
typing.TYPE_CHECKING = False
print(TYPE_CHECKING, typing.TYPE_CHECKING)
# 原项目形状：真正false的条件不执行类型定义，随后重绑True则执行。
from typing import TYPE_CHECKING as flag
if flag:
    from typing import TypedDict
    class Conditional(TypedDict, total=False):
        field: int
else:
    Conditional = dict
print(Conditional is dict)
flag = True
if flag:
    from typing import TypedDict
    class Conditional(TypedDict, total=False):
        field: int
print(Conditional.__name__, Conditional.__total__, Conditional(field=3))
