# 数据类默认表达式只求值一次，工厂逐实例调用并保留装饰/构造顺序。
from dataclasses import dataclass, field, FrozenInstanceError

log = []
def factory():
    log.append('factory')
    return []

def initial():
    log.append('default')
    return 7

@dataclass
class Item:
    name: str
    count: int = initial()
    values: list = field(default_factory=factory)

print(log)
a = Item('a')
b = Item('a')
print(log, a == b, repr(a))
a.values.append(3)
print(a.values, b.values, a == b)
c = Item('c', values=[9])
print(log, c)

@dataclass(frozen=True)
class Frozen:
    count: int = 4

f = Frozen()
try:
    f.count = 8
except FrozenInstanceError:
    print('frozen', f.count)
try:
    @dataclass
    class Invalid:
        values: list = []
except ValueError:
    print('mutable-default')
