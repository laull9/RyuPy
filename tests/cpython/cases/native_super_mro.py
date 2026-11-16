# ryupy: pending
"""C1 跨域 super 的完整 C3、Mixin 镜像关系与特殊属性遮蔽；普通 MI 保留 JIT 专项。"""
from collections import UserDict
from bridge_fixture.super_contract import make_subclass


class MixinBase:
    def chain(self):
        return 'MixinBase'


class Mixin(MixinBase):
    def chain(self):
        return 'Mixin:' + super().chain()


class RightMixin(MixinBase):
    __self__ = 'right-shadow'

    def chain(self):
        return 'RightMixin'


class Child(Mixin, RightMixin, UserDict):
    pass


class ReversedChild(UserDict, Mixin, RightMixin):
    pass


class SubChild(Child):
    pass


class SubReversed(ReversedChild):
    pass


class Unrelated:
    pass


for cls in (Child, ReversedChild, SubChild, SubReversed):
    child = cls()
    assert child.chain() == 'Mixin:RightMixin'
    assert isinstance(child, Mixin)
    assert isinstance(child, RightMixin)
    assert issubclass(cls, Mixin)
    assert issubclass(cls, RightMixin)
    assert not issubclass(cls, Unrelated)
    assert super(RightMixin, child).chain() == 'MixinBase'
    assert super(RightMixin, cls).__thisclass__ is RightMixin
    assert super(Mixin, child).__self__ == 'right-shadow'
    assert super(Mixin, child).__thisclass__ is Mixin
    saved = super(Mixin, child)
    RightMixin.__self__ = 'changed-shadow'
    assert saved.__self__ == 'changed-shadow'
    del RightMixin.__self__
    assert saved.__self__ is child
    RightMixin.__self__ = 'right-shadow'
    for proxy in (super(Mixin, child), super(cls), super(UserDict, child)):
        assert type(proxy) is super
        assert isinstance(proxy, super)
        assert proxy.__class__ is super
    try:
        super(Unrelated, child)
    except TypeError:
        pass
    else:
        raise AssertionError('super accepted an unrelated Mixin')
assert Child.__mro__[1] is Mixin
assert Mixin in ReversedChild.__mro__
assert not issubclass(Mixin, UserDict)
for base in (Child, ReversedChild, SubChild, SubReversed):
    opaque = make_subclass(base)
    value = opaque()
    assert issubclass(opaque, Mixin)
    assert issubclass(opaque, RightMixin)
    assert isinstance(value, Mixin)
    assert isinstance(value, RightMixin)
    assert not issubclass(opaque, Unrelated)
    assert not isinstance(value, Unrelated)
    assert not isinstance(opaque, Mixin)
print('native super C3 passed')
