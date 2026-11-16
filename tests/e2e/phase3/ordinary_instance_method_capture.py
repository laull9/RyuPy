# ryupy: pending
# 普通实例方法提前捕获：即时CPython oracle，保留绑定、求值顺序、异常、展开及重入原语句。

events = []

class Receiver:
    def method(self, *args, **kwargs):
        events.append('old')
        return len(args) + len(kwargs)

r = Receiver()
def replacement(*args, **kwargs):
    events.append('new')
    return 99

def rebind_instance():
    events.append('argument')
    r.method = replacement
    return 1

print('instance-rebind', r.method(rebind_instance()), events)
print('later', r.method(1))
del r.method
saved = Receiver.method

def rebind_class():
    events.append('class-argument')
    Receiver.method = replacement
    return 2

print('class-rebind', r.method(rebind_class()), events)
Receiver.method = saved

def delete_class():
    events.append('delete-argument')
    del Receiver.method
    return 3

print('class-delete', r.method(delete_class()), events)
Receiver.method = saved

# 各固定参数数量及数组/关键字入口均保留正常绑定，重复调用覆盖站点复用。
for repeat in range(2):
    print('arity', r.method(), r.method(1), r.method(1, 2), r.method(1, 2, 3), r.method(1, 2, 3, 4))
    print('keyword', r.method(a=1), r.method(1, b=2), r.method(*[1, 2], **{'c': 3}))

# 属性getter和__getattr__只执行一次，且在实参之前；属性返回的是未绑定的一等函数。
events = []
current = replacement
def old_callable(value):
    events.append('property-old')
    return value + 4
current = old_callable

class PropertyReceiver:
    @property
    def method(self):
        events.append('getter')
        return current

class FallbackReceiver:
    def __getattr__(self, name):
        events.append('getattr:' + name)
        return current

p = PropertyReceiver()
f = FallbackReceiver()
def change_target():
    global current
    events.append('change-target')
    current = replacement
    return 5

print('property', p.method(change_target()), events)
current = old_callable
events = []
print('getattr', f.method(change_target()), events)

# 缺属性/用户getter失败不得执行实参；非callable则须先执行实参。
events = []
def evaluated():
    events.append('evaluated')
    return 1

try:
    r.absent(evaluated())
except AttributeError:
    print('missing', events)

sentinel = ValueError('getter-marker')
class Raising:
    @property
    def method(self):
        events.append('raise-getter')
        raise sentinel

try:
    Raising().method(evaluated())
except ValueError as error:
    print('getter-error', error is sentinel, events)

r.method = 42
events = []
try:
    r.method(evaluated())
except TypeError:
    print('noncallable', events)

def argument_error():
    events.append('argument-error')
    raise KeyError('argument-marker')

try:
    r.method(argument_error())
except KeyError as error:
    print('argument-wins', str(error), events)

# *和**展开中的回调可以改绑属性，但不得替换已经捕获的函数。
class Star:
    def __iter__(self):
        events.append('star')
        r.method = replacement
        yield 7

class Keywords:
    def keys(self):
        events.append('keys')
        r.method = replacement
        return ['x']
    def __getitem__(self, key):
        events.append('item:' + key)
        return 8

r.method = old_callable
events = []
print('star', r.method(*Star()), events)
del r.method
events = []
print('double-star', r.method(**Keywords()), events)

# 同站点重入不得用子调用的参数覆盖外层参数。
class Reentrant:
    def method(self, value):
        if value == 1:
            inner = self.method(99)
            return value + inner
        return value

print('reentry', Reentrant().method(1))

# staticmethod和classmethod从实例访问时保留绑定对象，而不是回调后的类字典。
class Descriptors:
    @staticmethod
    def static(value):
        return value + 10
    @classmethod
    def bound(cls, value):
        return cls.__name__ + ':' + str(value)

d = Descriptors()
def change_static():
    Descriptors.static = replacement
    return 2

def change_bound():
    Descriptors.bound = replacement
    return 3

print('static', d.static(change_static()))
print('classmethod', d.bound(change_bound()))

# slots实例同样先绑定类方法；测试不依赖实例拥有__dict__。
class Slotted:
    __slots__ = ()
    value = 4
    def method(self, value):
        return self.value + value

slot = Slotted()
def change_slot_class():
    Slotted.method = replacement
    return 6

print('slots', slot.method(change_slot_class()))

# 仅验证现有empty slots能力；非空用户__slots__仍NI，不用此候选扩展布局支持。
# 活动handler下，缺__dict__的属性失败必须先于实参，并保留原上下文。
events = []
try:
    raise ValueError('slot-context')
except ValueError as active:
    try:
        slot.__dict__(evaluated())
    except AttributeError as error:
        print('slot-dict-missing', error.__context__ is active, events)
