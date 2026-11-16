
# 真实list派生存储：默认分配、二级继承、根初始化与现有核心协议/方法。

class Plain(list):
    pass

class Child(Plain):
    pass

class WorkItems(list):
    def __init__(self, values, label='untitled'):
        self += values
        self.label = label

    def take(self):
        return self.pop(0) if len(self) else None

    def first(self):
        return self[0] if len(self) else None

class NamedItems(WorkItems):
    def __init__(self, values, label):
        super().__init__(values, label=label)
        self.ready = True

class RootAccess(Plain):
    def __init__(self, values):
        super().__init__(values)
        self.initialized = True

    def empty_peer(self):
        return super().__new__(type(self), [99])

    def reset(self, values):
        return super().__init__(values)

empty = Plain()
first = Plain((1, 2))
second = Child(first)
assert empty == [] and type(empty) is Plain
assert first == [1, 2] and second == first and second is not first
assert type(second) is Child and second.__class__ is Child
assert isinstance(second, (Plain, list)) and isinstance(second, Child)
assert not isinstance(first, Child) and not isinstance([], Plain)
assert list.__hash__ is None and Plain.__hash__ is None
second.note = 'mutable'
assert second.note == 'mutable'
print('list-subclass.default-and-types', first, second, second.note)

items = NamedItems(('alpha', 'beta'), label='work')
alias = items
assert type(items) is NamedItems and isinstance(items, WorkItems)
assert items.label == 'work' and items.ready
assert items.first() == 'alpha' and items.take() == 'alpha'
assert items.first() == 'beta' and items.take() == 'beta'
assert items.take() is None and items.first() is None
assert items is alias and items == []
print('list-subclass.custom-init', items.label, items.ready, items is alias)

# 显式根new仅分配空payload，不消费/校验传给未来init的参数，不触发用户init。
raw = list.__new__(NamedItems, 1, 2, label='ignored')
assert type(raw) is NamedItems and raw == []
assert not hasattr(raw, 'label') and not hasattr(raw, 'ready')
assert list.__init__(raw, (3, 4)) is None
assert raw == [3, 4] and type(raw) is NamedItems
assert list.__init__(raw) is None and raw == []
rooted = RootAccess([5, 6])
assert rooted == [5, 6] and rooted.initialized
peer = rooted.empty_peer()
assert type(peer) is RootAccess and peer == [] and not hasattr(peer, 'initialized')
assert rooted.reset(rooted) is None and rooted == [] and rooted.initialized
print('list-subclass.root-allocation', type(raw).__name__, raw)

values = Child([3, 1, 2])
alias = values
assert values.append(4) is None
saved_extend = values.extend
assert saved_extend((5, 6)) is None
assert list.insert(values, 0, 0) is None
assert values == [0, 3, 1, 2, 4, 5, 6]
assert values.index(4) == 4 and values.count(3) == 1
assert list.remove(values, 3) is None
assert values.pop() == 6 and list.pop(values, 0) == 0
assert values.reverse() is None and values == [5, 4, 2, 1]
assert values.sort() is None and values == [1, 2, 4, 5]
assert values is alias
print('list-subclass.methods', values, values is alias)

child = [9]
values = Child([child, child])
for copied in (values.copy(), list.copy(values), values[:], values + [], [] + values,
               values * 1, 1 * values, list(values)):
    assert type(copied) is list and copied == values and copied is not values
    assert copied[0] is child and copied[0] is copied[1]
    copied.append('only-copy')
    assert len(values) == 2
print('list-subclass.fresh-base-results', values, values[0] is values[1])

values = Child([10, 20, 30, 40])
values[1] = 21
values[2:4] = (31, 41, 51)
del values[0]
del values[1:3]
assert values == [21, 51] and values[-1] == 51
assert values[::2] == [21] and type(values[::2]) is list
assert 21 in values and 99 not in values
assert len(values) == 2 and bool(values)
assert list(iter(values)) == [21, 51]
assert list(reversed(values)) == [51, 21]
assert values == [21, 51] and values < [22] and values > [20, 99]
assert repr(values) == '[21, 51]' and str(values) == '[21, 51]'
print('list-subclass.core-protocols', values, list(reversed(values)))

alias = values
values += (61,)
assert values is alias and type(values) is Child and values == [21, 51, 61]
values *= 2
assert values is alias and values == [21, 51, 61, 21, 51, 61]
assert list.__iadd__(values, [71]) is values
assert list.__imul__(values, 1) is values
assert list.__len__(values) == 7
assert list.__getitem__(values, 0) == 21
assert list.clear(values) is None and not values
print('list-subclass.in-place-identity', values, values is alias)

# 另一操作数的普通反射方法仍先于list的序列退路；显式根调用只执行原始list槽。
EVENTS = []
class Reflected:
    def __radd__(self, other):
        EVENTS.append('radd')
        return 'reflected-add'

    def __rmul__(self, other):
        EVENTS.append('rmul')
        return 'reflected-multiply'

    def __index__(self):
        EVENTS.append('index')
        return 2

value = Child([1, 2])
other = Reflected()
assert value + other == 'reflected-add'
assert value * other == 'reflected-multiply'
assert EVENTS == ['radd', 'rmul']
try:
    list.__add__(value, other)
except TypeError as error:
    assert str(error) == 'can only concatenate list (not "Reflected") to list'
else:
    raise AssertionError('root add must reject foreign operand')
repeated = list.__mul__(value, other)
assert repeated == [1, 2, 1, 2] and type(repeated) is list and repeated is not value
assert EVENTS == ['radd', 'rmul', 'index'] and value == [1, 2]
print('list-subclass.foreign-reflection', EVENTS, repeated)
