
# 命名方法/属性覆盖与显式根raw绕过；别名初始化、增量变化及失败后的可见payload。

EVENTS = []

class Logged(list):
    def append(self, value):
        EVENTS.append(('append', value))
        return super().append(value + 10)

    def pop(self, index=-1):
        EVENTS.append(('pop', index))
        return super().pop(index) + 100

    def copy(self):
        EVENTS.append('copy')
        return 'overridden-copy'

    def __repr__(self):
        return 'Logged:' + list.__repr__(self)

    def __str__(self):
        return 'items=' + str(list.__len__(self))

class Child(Logged):
    def append(self, value):
        EVENTS.append(('child', value))
        return super().append(value)

values = Child([1, 2])
saved_append = values.append
assert saved_append(3) is None
assert values == [1, 2, 13]
assert list.append(values, 4) is None and values == [1, 2, 13, 4]
assert values.pop(0) == 101 and list.pop(values) == 4
assert values.copy() == 'overridden-copy'
assert list.copy(values) == [2, 13] and type(list.copy(values)) is list
assert list.__repr__(values) == '[2, 13]'
assert list.__str__(values) == 'Logged:[2, 13]'
assert repr(values) == 'Logged:[2, 13]' and str(values) == 'items=2'
assert EVENTS == [('child', 3), ('append', 3), ('pop', 0), 'copy']
print('list-subclass.named-overrides', list.copy(values), EVENTS, repr(values), str(values))

# 方法缓存必须随普通class写入与实例遮蔽失效；已保存绑定仍持原方法与原实例。
class Switchable(list):
    def pop(self):
        return 'original'

def invoke(value):
    return value.pop()

def replacement(self):
    return 'replacement'

value = Switchable([1, 2])
saved = value.pop
assert invoke(value) == 'original' and invoke(value) == 'original'
Switchable.pop = replacement
assert invoke(value) == 'replacement' and saved() == 'original'
value.pop = lambda: 'instance'
assert invoke(value) == 'instance' and saved() == 'original'
del value.pop
assert invoke(value) == 'replacement' and list.pop(value) == 2
print('list-subclass.live-lookup', invoke(value), saved(), list.copy(value))

class PropertyList(list):
    @property
    def copy(self):
        EVENTS.append('property')
        return lambda: 'property-copy'

property_value = PropertyList([7])
EVENTS.clear()
property_saved = property_value.copy
assert property_saved() == 'property-copy'
assert property_value.copy() == 'property-copy'
assert list.copy(property_value) == [7]
assert EVENTS == ['property', 'property']
print('list-subclass.property-root-bypass', EVENTS, list.copy(property_value))

class Plain(list):
    pass

value = Plain([1, 2])
alias = value
assert list.__init__(value, value) is None
assert value is alias and value == []
value.extend([3, 4])
assert value.extend(value) is None and value == [3, 4, 3, 4]
value += value
assert value is alias and value == [3, 4, 3, 4, 3, 4, 3, 4]
print('list-subclass.self-alias', value, value is alias)

# 参数先求值再进入init；进入有效形状的init后先清空，再请求迭代。
EVENTS.clear()
def mark(label, value):
    EVENTS.append(label)
    return value

class InspectSource:
    def __init__(self, target):
        self.target = target

    def __iter__(self):
        EVENTS.append(('iter-sees', list.copy(self.target)))
        return iter([8, 9])

value = Plain([1, 2])
assert list.__init__(mark('self', value), mark('source', InspectSource(value))) is None
assert value == [8, 9]
assert EVENTS == ['self', 'source', ('iter-sees', [])]
print('list-subclass.init-order', EVENTS, value)

class BrokenSource:
    def __iter__(self):
        EVENTS.append('iter-fails')
        raise ValueError('cannot-iterate')

def interrupted():
    EVENTS.append('yield-first')
    yield 7
    EVENTS.append('iteration-fails')
    raise RuntimeError('iteration-stopped')

EVENTS.clear()
value = Plain([1, 2])
alias = value
try:
    list.__init__(value, BrokenSource())
except ValueError as error:
    assert str(error) == 'cannot-iterate'
else:
    raise AssertionError('missing iterable error')
assert value == [] and value is alias
try:
    list.__init__(value, interrupted())
except RuntimeError as error:
    assert str(error) == 'iteration-stopped'
else:
    raise AssertionError('missing init iteration error')
assert value == [7] and value is alias
try:
    value.extend(interrupted())
except RuntimeError as error:
    assert str(error) == 'iteration-stopped'
else:
    raise AssertionError('missing extend iteration error')
assert value == [7, 7] and value is alias
assert EVENTS == ['iter-fails', 'yield-first', 'iteration-fails', 'yield-first', 'iteration-fails']
print('list-subclass.partial-mutation', EVENTS, value, value is alias)

# 保存方法或暂停调用仍操作捕获时的原实例，后续接收者变量变化不能重定向。
receiver = Plain([10, 20])
original = receiver
saved = receiver.pop
def suspended():
    yield receiver.pop((yield 'index'))

pending = suspended()
assert next(pending) == 'index'
receiver = Plain([30, 40])
assert saved() == 20 and pending.send(0) == 10
assert original == [] and receiver == [30, 40]
print('list-subclass.capture-before-yield', original, receiver)
