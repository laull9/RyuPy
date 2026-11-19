# 尚未执行；只用普通成员，不以__call__等magic扩大范围。
from typing import NamedTuple

trace = []
def default_value():
    trace.append('method-default')
    return 5

class Members(NamedTuple):
    "Members documentation."
    x: int
    y: int = 2
    constant = 'kept'
    def total(self, extra=default_value()):
        return self.x + self.y + extra
    @staticmethod
    def add(a, b):
        return a + b
    @classmethod
    def owner(cls):
        return cls
    @property
    def value(self):
        return self.x
    @value.setter
    def value(self, item):
        trace.append(('set', self.x, item))
    @value.deleter
    def value(self):
        trace.append(('del', self.x))

m = Members(3)
bound = m.total
assert bound() == 10 and bound(1) == 6
assert m.total() == 10 and trace == ['method-default']
assert Members.add(4, 5) == m.add(4, 5) == 9
assert Members.owner() is Members and m.owner() is Members
assert m.constant == Members.constant == 'kept'
assert Members.__doc__ == 'Members documentation.'
print('N3.methods', bound(), bound(1), m.add(4, 5), m.owner() is Members, trace)
assert m.value == 3
m.value = 7
del m.value
assert m.value == 3 and m.x == 3
assert trace == ['method-default', ('set', 3, 7), ('del', 3)]
print('N3.property', m.value, trace)
print('N3.complete', True)
