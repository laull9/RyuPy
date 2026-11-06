# 类后hash槽变更与既有数据类合同；late变更不重演创建，数据类只覆盖既有frozen选项。
def rejected(value, label):
    try:
        hash(value)
    except TypeError as error:
        assert str(error) == "unhashable type: '" + type(value).__name__ + "'"
        print(label, str(error))
    else:
        raise AssertionError(label)
def equal(self, other):
    return True
def first_hash(self):
    return 31
def later_hash(self):
    return 47

class Late:
    pass
late = Late()
before = hash(late)
Late.__eq__ = equal
assert '__hash__' not in Late.__dict__
assert hash(late) == before
Late.__hash__ = None
rejected(late, 'late-none')
Late.__hash__ = later_hash
assert hash(late) == 47
del Late.__hash__
assert hash(late) == before
assert '__hash__' not in Late.__dict__

class Parent:
    __hash__ = first_hash
class Child(Parent):
    pass
class Alias(Parent):
    __hash__ = Parent.__hash__
child = Child()
alias = Alias()
assert hash(child) == 31
Parent.__hash__ = later_hash
assert hash(child) == 47
assert hash(alias) == 31
Parent.__hash__ = None
rejected(child, 'late-base-none')
del Parent.__hash__
assert hash(child) == object.__hash__(child)
assert hash(alias) == 31

class Equality:
    __eq__ = equal
same = Equality()
rejected(same, 'implicit-none')
del Equality.__eq__
assert Equality.__dict__['__hash__'] is None
rejected(same, 'delete-eq-keeps-none')
del Equality.__hash__
assert '__hash__' not in Equality.__dict__
assert hash(same) == object.__hash__(same)

# 显式实例属性仍可读，但隐式hash只查类型槽。
late.__hash__ = lambda: 999
late.__eq__ = lambda other: False
assert late.__hash__() == 999
assert hash(late) == before
class Disabled:
    __hash__ = None
disabled = Disabled()
disabled.__hash__ = lambda: 999
assert disabled.__hash__() == 999
rejected(disabled, 'instance-does-not-enable')

# dataclass在生成eq之前识别原own hash；不扩大eq=False、unsafe_hash或继承范围。
from dataclasses import dataclass
@dataclass
class Mutable:
    value: int
rejected(Mutable(2), 'mutable-data')
@dataclass(frozen=True)
class Frozen:
    value: int
assert Frozen.__hash__ is not None
assert hash(Frozen(2)) == hash((2,))
@dataclass(frozen=True)
class FrozenEquality:
    value: int
    def __eq__(self, other):
        return True
assert FrozenEquality.__hash__ is not None
assert hash(FrozenEquality(2)) == hash((2,))
@dataclass(frozen=True)
class FrozenExplicitNoneWithEquality:
    value: int
    __hash__ = None
    def __eq__(self, other):
        return True
assert FrozenExplicitNoneWithEquality.__hash__ is not None
assert hash(FrozenExplicitNoneWithEquality(2)) == hash((2,))
@dataclass(frozen=True)
class FrozenExplicitNone:
    value: int
    __hash__ = None
assert FrozenExplicitNone.__hash__ is None
rejected(FrozenExplicitNone(2), 'explicit-none-without-eq')
@dataclass(frozen=True)
class FrozenExplicitHash:
    value: int
    __hash__ = later_hash
assert hash(FrozenExplicitHash(2)) == 47

class LaterEquality:
    value: int
    __eq__ = equal
LaterEquality = dataclass(LaterEquality, frozen=True)
assert LaterEquality.__hash__ is not None
assert hash(LaterEquality(2)) == hash((2,))

# 已有mutable-default守卫会自然看到新的None槽；禁止用调用hash来探测。
class UnhashableDefault:
    __eq__ = equal
default = UnhashableDefault()
try:
    @dataclass
    class RejectDefault:
        value: object = default
except ValueError:
    print('implicit-none-default-rejected')
else:
    raise AssertionError('mutable-default')
default_calls = []
class HashDefault:
    def __hash__(self):
        default_calls.append('must-not-call')
        raise RuntimeError('default hash should not run')
safe_default = HashDefault()
@dataclass
class KeepDefault:
    value: object = safe_default
assert KeepDefault().value is safe_default
assert default_calls == []
print('mutation-dataclass-ok')
