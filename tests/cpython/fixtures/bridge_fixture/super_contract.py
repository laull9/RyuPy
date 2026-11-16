"""跨域 super 消费者：在 CPython 构造、保存、重新绑定并返回真实 super 值。"""
held = None


def make_super(start, receiver=None):
    global held
    held = super(start) if receiver is None else super(start, receiver)
    return held


def get_held():
    return held


def check_super(value):
    assert isinstance(value, super)
    assert type(value) is super
    return value


def rebind(value, receiver, owner):
    return super.__get__(value, receiver, owner)


def make_subclass(base):
    class CPSub(base):
        pass
    return CPSub


class SuperSubclass(super):
    pass


def make_super_subclass(start, receiver):
    return SuperSubclass, SuperSubclass(start, receiver)
