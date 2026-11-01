# 混合dataclass保持装饰位置、动态字段访问与对象身份，不借静态构造绕过替换结果。
from dataclasses import dataclass


def wrap(cls):
    print("wrap", cls.__name__)
    def factory(value):
        return cls(value)
    return factory


@wrap
@dataclass
class Point:
    x: int


def read(value):
    return value.x


point = Point(7)
print(read(point), repr(point), point == Point(7))


def inspect(cls):
    print("inspect", cls.__name__, "__init__" in cls.__dict__)
    return cls


@dataclass
@inspect
class Other:
    y: int


print(Other(8).y)


def same(cls):
    return cls


@same
@dataclass(frozen=True)
class Frozen:
    z: int


frozen = Frozen(z=9)
alias = frozen
print(alias is frozen, alias.z)
try:
    frozen.z = 10
except AttributeError:
    print("frozen")


class Replacement:
    pass


def replace(cls):
    return Replacement


@replace
class Original:
    pass


print(Original is Replacement, isinstance(Original(), Replacement))


@same
class Named:
    pass


first = Named


@same
class Named:
    pass


print(first is Named, isinstance(first(), Named))

# 被替换的类作为基类必须读取公开绑定。
class Child(Original):
    pass


print(isinstance(Child(), Replacement))

# dataclass选项属于装饰器自身，字段来自被替换后的输入类。
@dataclass(frozen=True)
@replace
class FrozenReplacement:
    ignored: int


replacement = FrozenReplacement()
try:
    replacement.extra = 1
except AttributeError:
    print("replacement frozen")

# 外层dataclass必须读取替代类自己的字段声明，不能强套原类ignored字段。
class DeclaredReplacement:
    actual: int


def replace_declared(cls):
    return DeclaredReplacement


@dataclass
@replace_declared
class ReplacedFields:
    ignored: int


print(ReplacedFields(actual=12).actual, repr(ReplacedFields(13)))
