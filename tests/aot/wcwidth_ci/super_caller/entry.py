# Legacy入口验证Instance caller本身的构造/预捕获；不是完整原fixture的Instance替代证明。
import caller
import model
import other_model

assert model.OriginalMiddle is model.Middle
assert model.OriginalMiddle is not other_model.Middle
assert model.OriginalMiddle.__name__ == 'Middle'
assert other_model.Middle.__name__ == 'Middle'

assert caller.canonical() is super
x = model.Leaf('x')
y = model.Leaf('y')
p = caller.construct(model.OriginalMiddle, x)
q = caller.direct(model.OriginalMiddle, y)
assert type(p) is super and isinstance(p, super)
assert caller.invoke_read(caller.direct(model.Leaf, x)) == 'middle:x'
assert caller.invoke_read(p) == 'root:x'
assert caller.invoke_read(q) == 'root:y'
assert caller.invoke_read(p) == 'root:x'
assert p.value == 'x' and p.cls() is model.Leaf and p.static() == 'static'
x.read = 'shadow'
assert caller.invoke_read(p) == 'root:x'
try:
    caller.construct(other_model.Middle, x)
except TypeError as error:
    assert str(error) == 'super(type, obj): obj must be an instance or subtype of type'
else:
    assert False
try:
    caller.direct(1, x)
except TypeError as error:
    assert str(error) == 'super() argument 1 must be a type, not int'
else:
    assert False
saved = p.read
model.Root.read = model.changed
assert caller.invoke_read(p) == 'changed:x'
assert caller.invoke_read(q) == 'changed:y'
assert saved() == 'root:x'
del model.Root.read
try:
    caller.invoke_read(p)
except AttributeError:
    pass
else:
    assert False
model.Root.read = model.changed
model.Root.call = model.old
assert caller.invoke_callback(p, model.replace) == 'old:x:arg'
assert model.events == ['arg']
model.events.clear()
assert caller.invoke_callback(p, model.arg) == 'new:x:arg'
assert model.events == ['arg']
model.events.clear()
assert caller.invoke_callback(p, model.remove) == 'new:x:arg'
assert model.events == ['arg']
model.events.clear()
try:
    caller.invoke_callback(p, model.arg)
except AttributeError:
    assert model.events == []
else:
    assert False
model.Root.call = property(model.getter)
assert caller.invoke_callback(p, model.arg) == 'arg'
assert model.events == ['get', 'arg']
model.events.clear()
model.Root.call = property(model.broken)
try:
    caller.invoke_callback(p, model.arg)
except ValueError:
    assert model.events == ['get']
else:
    assert False
model.events.clear()
model.Root.call = 42
try:
    caller.invoke_callback(p, model.arg)
except TypeError:
    assert model.events == ['arg']
else:
    assert False
print('instance caller explicit super construction capture; legacy model')
