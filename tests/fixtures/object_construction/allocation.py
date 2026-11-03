# 普通分配恰使用new返回对象；成功后才读取实际实例类的init。
log = []
foreign = object()
class Foreign:
    def __new__(cls, value):
        log.append(value)
        return foreign
    def __init__(self, value):
        raise AssertionError('foreign init')
assert Foreign(1) is foreign and log == [1]
class Parent:
    def __new__(cls, value):
        return object.__new__(Child)
    def __init__(self, value):
        raise AssertionError('wrong requested-class init')
class Child(Parent):
    def __init__(self, value):
        self.value = value
result = Parent(7)
assert type(result) is Child and result.value == 7
class Cached:
    def __new__(cls, value):
        return saved
    def __init__(self, value):
        log.append(value)
saved = object.__new__(Cached)
assert Cached(2) is saved and Cached(3) is saved
assert log == [1, 2, 3]
def later(self, value):
    self.value = value + 10
class Mutable:
    def __new__(cls, value):
        cls.__init__ = later
        return object.__new__(cls)
    def __init__(self, value):
        raise AssertionError('stale init')
assert Mutable(4).value == 14
class Nothing:
    def __new__(cls):
        return None
    def __init__(self):
        raise AssertionError('none init')
assert Nothing() is None
Alias = Mutable
assert Alias(5).value == 15
def through(factory):
    return factory(6)
assert through(Alias).value == 16
def typed(n: int) -> int:
    value = Mutable(n)
    return value.value
assert typed(8) == 18
def numeric(n: int) -> int:
    return n + 1
assert numeric(8) == 9
print('allocation-ok')
