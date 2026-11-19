# ryupy: pending
# 普通类 MI 原始可观察合同；不依赖 assert，不输出地址或时间。
def emit(name, value):
    print(name, repr(value))

class A:
    def chain(self):
        return ['A']
    def __init__(self, events):
        events.append('A')
    @classmethod
    def owner(cls):
        return cls.__name__
    @staticmethod
    def stable():
        return 'static-A'
    @property
    def value(self):
        return 'property-A'

class B(A):
    def chain(self):
        return ['B'] + super().chain()
    def __init__(self, events):
        events.append('B')
        super().__init__(events)
    @classmethod
    def owner(cls):
        return ['B', super().owner()]

class C(A):
    def chain(self):
        return ['C'] + super().chain()
    def __init__(self, events):
        events.append('C')
        super().__init__(events)
    @property
    def value(self):
        return 'property-C'

class D(B, C):
    pass

class E(D):
    pass

events = []
d = D(events)
e = E([])
emit('diamond_lookup', d.chain())
emit('single_over_mi', e.chain())
emit('constructor_once', events)
emit('explicit_super_receiver', super(B, d).chain())
emit('explicit_super_subclass', super(B, e).chain())
emit('classmethod_receiver', [D.owner(), E.owner()])
emit('staticmethod', [d.stable(), E.stable()])
emit('property_precedence', d.value)
emit('isinstance', [isinstance(d, A), isinstance(d, B), isinstance(d, C), isinstance(d, E)])
emit('issubclass', [issubclass(E, C), issubclass(C, B), issubclass(D, A)])
emit('mro_order', [x.__name__ for x in E.__mro__])
# 实例遮蔽非 data descriptor；property 仍有 data descriptor 优先级。
d.chain = lambda: ['instance']
emit('instance_shadow', d.chain())
d.__dict__['value'] = 'shadow'
emit('property_data_descriptor', d.value)
del d.chain

def replacement(self):
    return ['C-new'] + super(C, self).chain()

C.chain = replacement
emit('method_rebind_invalidation', [d.chain(), e.chain()])
Base = B
class Frozen(Base, C):
    pass
Base = A
emit('base_rebind_fixed', Frozen([]).chain())
seen = []
def choose(label, cls):
    seen.append(label)
    return cls
class Ordered(choose('left', B), choose('right', C)):
    seen.append('body')
emit('base_expression_order', seen)
emit('ordered_lookup', Ordered([]).chain())
# 内建非data descriptor classmethod/staticmethod 在 MI 中按同一个 MRO 选择。
class Left:
    @staticmethod
    def f():
        return 'left'
class Right:
    @classmethod
    def f(cls):
        return cls.__name__
class Mixed(Left, Right):
    pass
emit('descriptor_leftmost', Mixed.f())
emit('descriptor_super_binding', super(Left, Mixed).f())
