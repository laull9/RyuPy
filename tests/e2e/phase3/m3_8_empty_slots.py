class Empty:
    __slots__ = ()
e = Empty()
assert not hasattr(e, '__dict__')
try:
    e.value = 1
    assert False
except AttributeError:
    pass
class Child(Empty):
    def method(self):
        return 1
c = Child()
d = c.__dict__
c.value = 2
assert d['value'] == 2
d['value'] = 3
assert c.value == 3
del d['value']
assert not hasattr(c, 'value')
d['__dict__'] = 'shadow'
assert c.__dict__ is d
for _ in range(10):
    assert c.method() == 1
d['method'] = lambda: 9
assert c.method() == 9
del d['method']
assert c.method() == 1
new = {'value': 7}
c.__dict__ = new
assert c.__dict__ is new
assert c.value == 7
assert d is not c.__dict__
del c.__dict__
assert c.__dict__ == {}
assert new['value'] == 7
class WithDict:
    pass
class KeepsDict(WithDict):
    __slots__ = ()
k = KeepsDict()
k.value = 4
assert k.__dict__['value'] == 4
Empty.__slots__ = ('late',)
assert not hasattr(e, '__dict__')
class StillEmpty(Empty):
    __slots__ = ()
assert not hasattr(StillEmpty(), '__dict__')
print('empty slots: ok')
