"""C1 原生 super 竖切：单继承、零参上下文、显式/未绑定代理及错误参数。"""
from collections import UserDict
from bridge_fixture.super_contract import check_super, get_held, make_super, make_super_subclass, rebind


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
for proxy in (bound, unbound, super(UserDict, value)):
    assert isinstance(proxy, super)
    assert type(proxy) is super
    assert proxy.__class__ is super
    assert check_super(proxy) is proxy
incoming = make_super(Leaf, value)
assert get_held() is incoming
assert isinstance(incoming, super)
assert type(incoming) is super
incoming_unbound = make_super(Leaf)
rebound = incoming_unbound.__get__(value, Leaf)
assert type(rebound) is super
assert isinstance(rebound, super)
assert rebound.__self__ is value
assert rebound.__thisclass__ is Leaf
assert type(rebind(incoming_unbound, value, Leaf)) is super
subtype, subtype_value = make_super_subclass(Leaf, value)
assert type(subtype_value) is subtype
assert isinstance(subtype_value, super)
assert issubclass(subtype, super)


class CompiledSuperSubclass(subtype):
    pass


compiled_subtype_value = CompiledSuperSubclass(Leaf, value)
assert type(compiled_subtype_value) is CompiledSuperSubclass
assert isinstance(compiled_subtype_value, super)
assert issubclass(CompiledSuperSubclass, super)
assert not isinstance(subtype, super)
assert not isinstance(CompiledSuperSubclass, super)
assert not isinstance(value, super)
assert not isinstance(UserDict(), super)
assert not issubclass(Leaf, super)
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
