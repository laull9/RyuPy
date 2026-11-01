# ryupy: pending
# 通用兼容欠账：类定义时的隐式 __hash__ 槽未完整建模，Hashable 暂不导出。
# 保留原新增正向验收，并加动态/继承反例，不能凭当前 __eq__ 属性存在性猜测。
from collections import deque
from collections.abc import Hashable

print(isinstance(([],), Hashable), isinstance([], Hashable), isinstance(deque(), Hashable))
class Ordinary:
    pass
class Equality:
    def __eq__(self, other):
        return True
print(isinstance(Ordinary(), Hashable), isinstance(Equality(), Hashable))
Ordinary.__eq__ = lambda self, other: True
print(isinstance(Ordinary(), Hashable))
class HasHash:
    def __hash__(self):
        return 1
class OverridesEquality(HasHash):
    def __eq__(self, other):
        return True
print(isinstance(OverridesEquality(), Hashable))
