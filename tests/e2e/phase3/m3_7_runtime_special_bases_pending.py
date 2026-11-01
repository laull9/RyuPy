# ryupy: pending
# 特殊布局动态继承尚未实现：这些 CPython 正例必须保 pending，不算原生通过。
from dataclasses import dataclass
from enum import Enum
class Holder:
    pass
holder = Holder()
@dataclass
class Data:
    value: int
holder.base = Data
class DerivedData(holder.base):
    pass
print(DerivedData(3).value)
holder.base = ValueError
class DerivedError(holder.base):
    pass
print(isinstance(DerivedError('x'), ValueError))
holder.base = Enum
class DerivedEnum(holder.base):
    VALUE = 1
print(DerivedEnum.VALUE.value)
# 不只检查直接flag：特殊身份能经普通派生类传递到更深祖先。
class ErrorParent(ValueError):
    pass
holder.base = ErrorParent
class ErrorChild(holder.base):
    pass
print(isinstance(ErrorChild(), ValueError))
class DataParent(Data):
    pass
holder.base = DataParent
class DataChild(holder.base):
    pass
print(DataChild(4).value)
