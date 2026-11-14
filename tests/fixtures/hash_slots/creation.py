# 普通类创建期hash合同：只按最终own字典补None，保继承、根绑定及装饰顺序。
def rejected(value, label):
    try:
        hash(value)
    except TypeError as error:
        assert str(error) == "unhashable type: '" + type(value).__name__ + "'"
        print(label, str(error))
    else:
        raise AssertionError(label)

class Ordinary:
    pass
ordinary = Ordinary()
assert '__hash__' not in Ordinary.__dict__
assert Ordinary.__hash__ is object.__hash__
assert hash(ordinary) == object.__hash__(ordinary)
assert Ordinary.__hash__(ordinary) == hash(ordinary)
assert ordinary.__hash__() == hash(ordinary)

class Equality:
    def __eq__(self, other):
        return True
assert Equality.__dict__['__hash__'] is None
rejected(Equality(), 'own-eq')

class InheritedNone(Equality):
    pass
assert '__hash__' not in InheritedNone.__dict__
assert InheritedNone.__hash__ is None
rejected(InheritedNone(), 'inherited-none')

class HasHash:
    def __hash__(self):
        return 31
class InheritedHash(HasHash):
    pass
assert '__hash__' not in InheritedHash.__dict__
assert hash(InheritedHash()) == 31
class OverridesEquality(HasHash):
    def __eq__(self, other):
        return True
assert OverridesEquality.__dict__['__hash__'] is None
rejected(OverridesEquality(), 'overrides-inherited-hash')

class RestoredHash(HasHash):
    def __eq__(self, other):
        return True
    __hash__ = HasHash.__hash__
assert hash(RestoredHash()) == 31
class RootHash:
    def __eq__(self, other):
        return True
    __hash__ = object.__hash__
root_hash = RootHash()
assert RootHash.__dict__['__hash__'] is object.__hash__
assert hash(root_hash) == object.__hash__(root_hash)
assert RootHash.__hash__(root_hash) == hash(root_hash)
assert root_hash.__hash__() == hash(root_hash)
class SuperHash:
    def __hash__(self):
        return super().__hash__()
super_hash = SuperHash()
assert hash(super_hash) == object.__hash__(super_hash)
class SuperParentHash(HasHash):
    def __hash__(self):
        return super().__hash__()
assert hash(SuperParentHash()) == 31

bindings = []
class StaticHash:
    @staticmethod
    def __hash__():
        bindings.append('static')
        return 37
class ClassHash:
    @classmethod
    def __hash__(cls):
        bindings.append(cls.__name__)
        return 41
class ChildClassHash(ClassHash):
    pass
assert hash(StaticHash()) == 37
assert hash(ChildClassHash()) == 41
assert bindings == ['static', 'ChildClassHash']

class EqualityIsNone:
    __eq__ = None
assert EqualityIsNone.__dict__['__hash__'] is None
rejected(EqualityIsNone(), 'noncallable-eq-still-own')
class InequalityOnly:
    def __ne__(self, other):
        return True
assert '__hash__' not in InequalityOnly.__dict__
inequality = InequalityOnly()
assert hash(inequality) == object.__hash__(inequality)

class BranchAbsent:
    if False:
        def __eq__(self, other):
            return True
class BranchPresent:
    if True:
        def __eq__(self, other):
            return True
assert '__eq__' not in BranchAbsent.__dict__
assert '__hash__' not in BranchAbsent.__dict__
branch_absent = BranchAbsent()
assert hash(branch_absent) == object.__hash__(branch_absent)
assert BranchPresent.__dict__['__hash__'] is None
rejected(BranchPresent(), 'selected-eq')

class FinalHash:
    __hash__ = None
    def __eq__(self, other):
        return True
    if True:
        __hash__ = HasHash.__hash__
assert hash(FinalHash()) == 31
class FinalNone:
    __hash__ = HasHash.__hash__
    if True:
        __hash__ = None
rejected(FinalNone(), 'final-none')

class NonCallableHash:
    __hash__ = 7
try:
    hash(NonCallableHash())
except TypeError as error:
    assert str(error) == "'int' object is not callable"
else:
    raise AssertionError('noncallable-hash')

seen = []
def observe(cls):
    seen.append(cls.__dict__['__hash__'] is None)
    return cls
@observe
class DecoratedEquality:
    def __eq__(self, other):
        return True
assert seen == [True]
def late_equality(cls):
    cls.__eq__ = lambda self, other: True
    return cls
@late_equality
class DecoratorAddsEquality:
    pass
decorated = DecoratorAddsEquality()
assert '__hash__' not in DecoratorAddsEquality.__dict__
assert hash(decorated) == object.__hash__(decorated)
print('creation-ok')
