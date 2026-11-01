# 字段开关、默认对象别名、工厂失败、冻结删除和用户方法保留。
from dataclasses import dataclass, field, FrozenInstanceError, MISSING

calls = []
def factory():
    calls.append('factory')
    return []

@dataclass
class Config:
    x: int
    hidden: int = field(default=4, repr=False, compare=False)
    values: list = field(default_factory=factory, init=False)
    shared: object = None

    def __post_init__(self):
        calls.append(self.x)

a = Config(1)
b = Config(1, hidden=8)
print(a, a == b, calls)
a.values.append(2)
print(a == b, a.values, b.values)
try:
    Config(1, values=[])
except TypeError:
    print('init-false')
print(calls)

@dataclass
class Explicit:
    x: object = field(default_factory=factory)

print(Explicit(MISSING).x is MISSING, calls)

def failed():
    print('failed-factory')
    raise ValueError('stop')

@dataclass
class Failure:
    x: object = field(default_factory=failed)

try:
    Failure()
except ValueError:
    print('factory-propagated')

@dataclass(frozen=True)
class Frozen:
    x: int

f = Frozen(1)
print(hash(f) == hash(Frozen(1)), f is f)
try:
    del f.x
except FrozenInstanceError:
    print('cannot-delete')

@dataclass
class Custom:
    x: int
    def __repr__(self):
        return 'custom'
    def __eq__(self, other):
        return False

print(Custom(1), Custom(1) == Custom(1))
try:
    @dataclass
    class BadOrder:
        x: int = 1
        y: int
except TypeError:
    print('order-error')

# 自定义不可哈希默认值拒绝但不能调用用户hash。
class Unhashable:
    __hash__ = None

try:
    @dataclass
    class BadCustomDefault:
        x: object = Unhashable()
except ValueError:
    print('custom-mutable-default')

# 旧静态构造ABI不再阻止参数跨yield，提前绑定当前类对象。
def suspended():
    return Frozen((yield 'value'))

pending = suspended()
print(next(pending))
try:
    pending.send(8)
except StopIteration as result:
    print(result.value)

Old = 'previous'
try:
    @dataclass
    class Old:
        values: list = []
except ValueError:
    print(Old)

from dataclasses import fields, is_dataclass
print(is_dataclass(Config), is_dataclass(a), is_dataclass(1))
print([(f.name, f.init, f.repr, f.compare) for f in fields(Config)])
print(fields(Config)[0].type is int)
print(fields(Explicit)[0].default_factory is factory)

# eq必须先保存左右全部字段，首字段的用户比较不能改变后续待比较值。
other_pair = None
class Trigger:
    def __eq__(self, other):
        other_pair.right = 9
        return True

@dataclass
class Pair:
    left: object
    right: int

first_pair = Pair(Trigger(), 1)
other_pair = Pair(Trigger(), 1)
print(first_pair == other_pair, other_pair.right)

# 生成方法仍是可替换的普通方法，优化不得绕过后续类写入。
@dataclass
class Rebound:
    x: int

def replacement(self, x):
    self.x = x + 10

Rebound.__init__ = replacement
print(Rebound(2).x)

# 是否生成post-init调用在装饰时决定，已有调用仍动态查找重绑后的方法。
@dataclass
class LateHook:
    x: int

def late_hook(self):
    print('late-hook')

LateHook.__post_init__ = late_hook
print(LateHook(1))

@dataclass
class ExistingHook:
    x: int
    def __post_init__(self):
        print('original-hook')

ExistingHook.__post_init__ = late_hook
print(ExistingHook(2))

# 原生可变容器也没有可接受的字段默认哈希语义。
from collections import deque, Counter, defaultdict
for mutable_default in [deque(), Counter(), defaultdict(list)]:
    try:
        @dataclass
        class MutableDefault:
            item: object = mutable_default
    except ValueError:
        print('mutable-default')
