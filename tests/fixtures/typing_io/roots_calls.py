"""I1：三根真实身份、完整 pass 调用与有限函数元数据；不读取泛型或完整反射。"""
import typing
from typing import IO, BinaryIO, TextIO, get_args, get_origin, List

ROOTS = (IO, BinaryIO, TextIO)
NAMES = ('IO', 'BinaryIO', 'TextIO')
METHODS = ('close', 'fileno', 'flush', 'isatty', 'read', 'readable',
           'readline', 'readlines', 'seek', 'seekable', 'tell', 'truncate',
           'writable', 'write', 'writelines', '__enter__', '__exit__')
DEFAULTS = {'read': (-1,), 'readline': (-1,), 'readlines': (-1,),
            'seek': (0,), 'truncate': (None,)}
PROPERTIES = ((IO, ('mode', 'name', 'closed')),
              (TextIO, ('buffer', 'encoding', 'errors', 'line_buffering', 'newlines')))
assert IO is typing.IO and BinaryIO is typing.BinaryIO and TextIO is typing.TextIO
assert IO is not BinaryIO and IO is not TextIO and BinaryIO is not TextIO
for cls, name in zip(ROOTS, NAMES):
    value = cls()
    assert type(cls) is type and cls.__class__ is type
    assert type(value) is cls and value.__class__ is cls
    assert cls.__name__ == name and cls.__qualname__ == name
    assert cls.__module__ == 'typing' and value.__module__ == 'typing'
    assert repr(cls) == "<class 'typing." + name + "'>"
    assert repr(value).startswith('<typing.' + name + ' object at 0x')
    assert get_origin(cls) is None and get_args(cls) == ()
    assert cls.__slots__ == () and cls.__slots__ is cls.__slots__
    assert not hasattr(value, '__dict__')
for cls in (BinaryIO, TextIO):
    assert cls.__base__ is IO and cls.__bases__ == (IO,)
    assert cls.__bases__ is cls.__bases__ and cls.__parameters__ == ()
print('I1.identity', True)

assert BinaryIO.read is IO.read and TextIO.read is IO.read
assert BinaryIO.write is not IO.write and TextIO.write is IO.write
assert BinaryIO.__enter__ is not IO.__enter__ and TextIO.__enter__ is not IO.__enter__
assert BinaryIO.mode is IO.mode and TextIO.mode is IO.mode
assert repr(List[BinaryIO]) == 'typing.List[typing.BinaryIO]'
print('I1.inheritance', True)

# 原生方法没有实参类型检查或隐式转换；类型不符合注解仍执行 pass。
events = []
class Untouched:
    def __index__(self):
        events.append('index')
        raise AssertionError('unexpected conversion')
    def __iter__(self):
        events.append('iter')
        raise AssertionError('unexpected iteration')
    def __bytes__(self):
        events.append('bytes')
        raise AssertionError('unexpected bytes')
token = Untouched()
for cls in ROOTS:
    value = cls()
    results = (value.close(), value.fileno(), value.flush(), value.isatty(),
               value.read(), value.readable(), value.readline(), value.readlines(),
               value.seek(0), value.seekable(), value.tell(), value.truncate(),
               value.writable(), value.write(token), value.writelines(token),
               value.__enter__(), value.__exit__(None, None, None))
    assert len(results) == 17 and all(item is None for item in results)
    keywords = (value.read(n=token), value.readline(limit=token),
                value.readlines(hint=token), value.seek(offset=token, whence=token),
                value.truncate(size=token), value.write(s=token),
                value.writelines(lines=token),
                value.__exit__(type=token, value=token, traceback=token),
                cls.read(self=123, n=token), cls.write(self=None, s=token))
    assert all(item is None for item in keywords)
assert events == []
print('I1.pass', 51, events)

# 限定名使用声明方；只覆盖合同列明的真实元数据，不依赖 inspect。
for cls in ROOTS:
    value = cls()
    for name in METHODS:
        function = getattr(cls, name)
        bound = getattr(value, name)
        owner = cls.__name__ if name == '__enter__' or (cls is BinaryIO and name == 'write') else 'IO'
        assert type(function).__name__ == 'function' and function.__class__ is type(function)
        assert type(bound).__name__ == 'method' and bound.__class__ is type(bound)
        assert bound.__self__ is value and bound.__func__ is function
        assert function.__name__ == name and function.__qualname__ == owner + '.' + name
        assert function.__module__ == 'typing' and function.__doc__ is None
        assert function.__defaults__ == DEFAULTS.get(name)
        assert function.__kwdefaults__ is None and function.__isabstractmethod__ is True
print('I1.methods', True)

count = 0
for owner, names in PROPERTIES:
    for name in names:
        descriptor = getattr(owner, name)
        assert type(descriptor).__name__ == 'property'
        assert descriptor.fset is None and descriptor.fdel is None
        assert descriptor.fget.__name__ == name
        assert descriptor.fget.__qualname__ == owner.__name__ + '.' + name
        assert descriptor.fget.__module__ == 'typing'
        assert descriptor.fget.__defaults__ is None and descriptor.fget.__kwdefaults__ is None
        assert descriptor.fget.__doc__ is None and descriptor.fget.__isabstractmethod__ is True
        assert descriptor.fget(123) is None and getattr(owner(), name) is None
        count += 1
assert count == 8
print('I1.getters', count)
print('I1.complete', True)
