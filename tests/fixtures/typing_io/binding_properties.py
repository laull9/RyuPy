"""I2：签名错误、原始只读 property 与根零槽不变量；不把 NI 当作 CP 成功。"""
from typing import IO, BinaryIO, TextIO


def reject(operation, expected_type, message):
    try:
        operation()
    except Exception as error:
        assert type(error) is expected_type
        assert str(error) == message and error.args == (message,)
    else:
        raise AssertionError('expected error')


def delete_known_property(value, name):
    # builtin delattr 尚未实现；有限显式 del 保同一接收者、属性名及完整 deleter 检查。
    if name == 'mode':
        del value.mode
    elif name == 'name':
        del value.name
    elif name == 'closed':
        del value.closed
    elif name == 'buffer':
        del value.buffer
    elif name == 'encoding':
        del value.encoding
    elif name == 'errors':
        del value.errors
    elif name == 'line_buffering':
        del value.line_buffering
    elif name == 'newlines':
        del value.newlines
    else:
        raise AssertionError('unlisted property deletion')


for cls in (IO, BinaryIO, TextIO):
    name = cls.__name__
    reject(lambda: cls(1), TypeError, name + '() takes no arguments')
    reject(lambda: cls(flag=True), TypeError, name + '() takes no arguments')
    reject(lambda: cls(1, flag=True), TypeError, name + '() takes no arguments')
print('I2.constructors', True)

for cls in (IO, BinaryIO, TextIO):
    value = cls()
    write_owner = 'BinaryIO' if cls is BinaryIO else 'IO'
    reject(lambda: value.close(1), TypeError,
           'IO.close() takes 1 positional argument but 2 were given')
    reject(lambda: value.close(flag=True), TypeError,
           "IO.close() got an unexpected keyword argument 'flag'")
    reject(lambda: value.read(1, 2), TypeError,
           'IO.read() takes from 1 to 2 positional arguments but 3 were given')
    reject(lambda: value.read(size=1), TypeError,
           "IO.read() got an unexpected keyword argument 'size'")
    reject(lambda: value.read(1, n=2), TypeError,
           "IO.read() got multiple values for argument 'n'")
    reject(lambda: value.readline(n=1), TypeError,
           "IO.readline() got an unexpected keyword argument 'n'")
    reject(lambda: value.readlines(n=1), TypeError,
           "IO.readlines() got an unexpected keyword argument 'n'")
    reject(lambda: value.seek(), TypeError,
           "IO.seek() missing 1 required positional argument: 'offset'")
    reject(lambda: value.seek(0, offset=1), TypeError,
           "IO.seek() got multiple values for argument 'offset'")
    reject(lambda: value.truncate(n=1), TypeError,
           "IO.truncate() got an unexpected keyword argument 'n'")
    reject(lambda: value.write(), TypeError,
           write_owner + ".write() missing 1 required positional argument: 's'")
    reject(lambda: value.write(data=b'x'), TypeError,
           write_owner + ".write() got an unexpected keyword argument 'data'")
    reject(lambda: value.writelines(), TypeError,
           "IO.writelines() missing 1 required positional argument: 'lines'")
    reject(lambda: value.__exit__(), TypeError,
           "IO.__exit__() missing 3 required positional arguments: 'type', 'value', and 'traceback'")
    reject(lambda: cls.read(), TypeError,
           "IO.read() missing 1 required positional argument: 'self'")
    assert cls.read(123) is None
print('I2.bindings', 45)

for cls in (IO, BinaryIO, TextIO):
    value = cls()
    names = ('mode', 'name', 'closed')
    if cls is TextIO:
        names += ('buffer', 'encoding', 'errors', 'line_buffering', 'newlines')
    for name in names:
        descriptor = getattr(cls, name)
        assert getattr(value, name) is None and descriptor.fset is None and descriptor.fdel is None
        reject(lambda: setattr(value, name, 1), AttributeError,
               "property '" + name + "' of '" + cls.__name__ + "' object has no setter")
        reject(lambda: delete_known_property(value, name), AttributeError,
               "property '" + name + "' of '" + cls.__name__ + "' object has no deleter")
    if cls is not TextIO:
        reject(lambda: value.buffer, AttributeError,
               "'" + cls.__name__ + "' object has no attribute 'buffer'")
print('I2.properties', True)

for cls in (IO, BinaryIO, TextIO):
    value = cls()
    for name in ('extra', '__orig_class__'):
        reject(lambda: setattr(value, name, 1), AttributeError,
               "'" + cls.__name__ + "' object has no attribute '" + name + "'")
        assert not hasattr(value, name)
    reject(lambda: setattr(value, 'read', 1), AttributeError,
           "'" + cls.__name__ + "' object attribute 'read' is read-only")
    assert value.read() is None and not hasattr(value, '__dict__')
print('I2.root-layout', True)

# 同一真实语法站点跨根/普通子类写入，不能用宿主污染造不可达槽。
def write_extra(value, item):
    value.extra = item

class Child(BinaryIO):
    pass

root = BinaryIO()
child = Child()
for item in (1, 2, 3):
    reject(lambda: write_extra(root, item), AttributeError,
           "'BinaryIO' object has no attribute 'extra'")
    write_extra(child, item)
    write_extra(child, item + 1)
    assert child.extra == item + 1 and not hasattr(root, 'extra')
print('I2.shared-site', child.extra)

# 根 late property setter 优先于无字典拒绝；setter内部的新字段写仍失败。
events = []
def probe_get(self):
    return 'visible'
def probe_set(self, item):
    events.append(('set', item))
def probe_delete(self):
    events.append(('delete',))
def failing_set(self, item):
    events.append(('fail', item))
    self.inner = item

try:
    BinaryIO.probe = property(probe_get).setter(probe_set).deleter(probe_delete)
    root.probe = 7
    assert root.probe == 'visible'
    del root.probe
    BinaryIO.probe = property(probe_get).setter(failing_set)
    reject(lambda: setattr(root, 'probe', 8), AttributeError,
           "'BinaryIO' object has no attribute 'inner'")
    assert not hasattr(root, 'inner')
finally:
    del BinaryIO.probe
assert events == [('set', 7), ('delete',), ('fail', 8)]
assert not hasattr(root, 'probe')
print('I2.property-order', events)

# 已闭合的两个类订阅是 CP 真 TypeError，不代表实现了 IO 泛型。
reject(lambda: BinaryIO[bytes], TypeError,
       "<class 'typing.BinaryIO'> is not a generic class")
reject(lambda: TextIO[str], TypeError,
       "<class 'typing.TextIO'> is not a generic class")
print('I2.closed-subscription', True)
print('I2.complete', True)
