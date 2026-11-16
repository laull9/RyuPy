# ryupy: pending
"""C1 原生 super 竖切：单继承、零参上下文、显式/未绑定代理及错误参数。"""
from collections import UserDict


class Base(UserDict):
    def describe(self):
        return 'base:' + self['label']

    @classmethod
    def kind(cls):
        return cls.__name__

    @property
    def count(self):
        return len(self)


class Leaf(Base):
    def __init__(self, label):
        super().__init__({'label': label})

    def describe(self):
        return super().describe() + ':leaf'

    @classmethod
    def kind(cls):
        return 'leaf:' + super().kind()


value = Leaf('native')
assert value.describe() == 'base:native:leaf'
assert Leaf.kind() == 'leaf:Leaf'
assert super(Leaf, value).describe() == 'base:native'
assert super(Leaf, value).count == 1
bound = super(Leaf, value)
assert bound.__thisclass__ is Leaf
assert bound.__self__ is value
assert bound.__self_class__ is Leaf
unbound = super(Leaf)
assert unbound.__thisclass__ is Leaf
assert unbound.__self__ is None
try:
    super(Leaf, 1)
except TypeError:
    pass
else:
    raise AssertionError('super accepted an unrelated receiver')
try:
    super(Leaf, value).missing
except AttributeError:
    pass
else:
    raise AssertionError('super lost missing-attribute error')


class ShadowBase(UserDict):
    __self__ = 'base-shadow'


class ShadowLeaf(ShadowBase):
    pass


# CPython 优先搜索后继 MRO 成员，再回退 super 自身元数据。
assert super(ShadowLeaf, ShadowLeaf()).__self__ == 'base-shadow'
print('native super passed')
