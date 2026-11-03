# 最终own namespace隐式静态化，类后monkeypatch不包装；super保定义owner。
log = []
def raw(cls, *args, **kwargs):
    return object.__new__(cls)
def observe(cls):
    assert type(cls.__dict__['__new__']).__name__ == 'staticmethod'
    return cls
def replace(function):
    return raw
@observe
class FromAlias:
    __new__ = raw
@observe
class FromDecorator:
    @replace
    def __new__(cls):
        raise AssertionError('decorated original')
@observe
class Conditional:
    __new__ = None
    if True:
        __new__ = raw
for cls in [FromAlias, FromDecorator, Conditional]:
    instance = cls()
    assert type(instance) is cls
    assert instance.__new__ is raw
class Late:
    pass
Late.__new__ = raw
assert type(Late.__dict__['__new__']).__name__ == 'function'
assert type(object.__new__(Late).__new__).__name__ == 'method'
assert type(Late()) is Late
class Base:
    def __new__(cls, *args):
        log.append(cls.__name__)
        return super().__new__(cls)
    def __init__(self, *args):
        super().__init__()
class Leaf(Base):
    pass
assert type(Leaf(1)) is Leaf and log == ['Leaf']
class ClassNew:
    @classmethod
    def __new__(bound, explicit, value):
        assert bound is explicit
        log.append(value)
        return object.__new__(explicit)
assert type(ClassNew(2)) is ClassNew and log[-1] == 2
class StaticInit:
    @staticmethod
    def __init__(value):
        log.append(value)
assert type(StaticInit(3)) is StaticInit and log[-1] == 3
class ClassInit:
    @classmethod
    def __init__(cls, value):
        assert cls is ClassInit
        log.append(value)
assert type(ClassInit(4)) is ClassInit and log[-1] == 4
print('binding-ok')
