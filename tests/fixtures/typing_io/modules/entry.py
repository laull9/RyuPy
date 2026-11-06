"""I4：真实跨模块绑定及定义时global __name__来源快照，不借调用者来源替代。"""
import typing
import io_provider as provider
from io_provider import Saved as Imported
from typing import BinaryIO

assert Imported is BinaryIO and provider.Base is BinaryIO and provider.Saved is BinaryIO
assert provider.native_base() is BinaryIO
assert typing.BinaryIO.__module__ == 'typing' and typing.BinaryIO.read.__module__ == 'typing'
stream = provider.Stream()
assert stream.read(2) == ('provider', 2)
assert isinstance(stream, BinaryIO) and issubclass(provider.Stream, BinaryIO)
assert not isinstance(provider.BinaryIO(), BinaryIO)
assert not issubclass(provider.BinaryIO, BinaryIO)
print('I4.imports', True)

# 类来源取定义时本模块global槽，不能取调用者或模块对象的初始注册名。
assert provider.Stream.__module__ == 'io_provider'
assert provider.Stream.__qualname__ == 'Stream'
assert repr(provider.Stream) == "<class 'io_provider.Stream'>"
assert repr(stream).startswith('<io_provider.Stream object at 0x')
assert provider.RenamedStream.__module__ == 'provider-before-definition'
assert provider.RenamedStream.__qualname__ == 'RenamedStream'
assert repr(provider.RenamedStream) == "<class 'provider-before-definition.RenamedStream'>"
assert repr(provider.RenamedStream()).startswith('<provider-before-definition.RenamedStream object at 0x')
entry_name = __name__
try:
    __name__ = 'entry-before-definition'
    class RenamedEntry(BinaryIO):
        pass
finally:
    __name__ = entry_name
assert RenamedEntry.__module__ == 'entry-before-definition'
assert RenamedEntry.__qualname__ == 'RenamedEntry'
assert repr(RenamedEntry) == "<class 'entry-before-definition.RenamedEntry'>"
assert repr(RenamedEntry()).startswith('<entry-before-definition.RenamedEntry object at 0x')
# 后改globals不能回写已经完成的类来源；外部模块属性只写，不依赖其__name__读旧债。
try:
    provider.__name__ = 'provider-after-definition'
    __name__ = 'entry-after-definition'
    assert provider.Stream.__module__ == 'io_provider'
    assert provider.RenamedStream.__module__ == 'provider-before-definition'
    assert RenamedEntry.__module__ == 'entry-before-definition'
finally:
    provider.__name__ = provider.initial_name
    __name__ = entry_name
print('I4.definition-origin', True)

class Existing(provider.Base):
    pass
saved = Existing().read
old = provider.Base
try:
    provider.Base = provider.Replacement
    class Changed(provider.Base):
        pass
    assert provider.current_base() is provider.Replacement
    assert provider.Saved is old and Imported is old and BinaryIO is old
    assert Changed().read(3) == ('replacement', 3)
    assert not issubclass(Changed, BinaryIO)
    assert isinstance(Existing(), BinaryIO) and saved() is None
finally:
    provider.Base = old
print('I4.provider-rebind', True)

original = typing.BinaryIO
try:
    typing.BinaryIO = provider.Replacement
    from typing import BinaryIO as Current
    assert Current is provider.Replacement and provider.native_base() is provider.Replacement
    assert Imported is original and provider.Saved is original
    assert isinstance(provider.Stream(), original)
finally:
    typing.BinaryIO = original
assert typing.BinaryIO is BinaryIO
print('I4.native-rebind', True)

# 已保存原生绑定保真实函数和实例；不会因模块槽重绑改目标。
assert saved.__func__ is BinaryIO.read and saved.__self__.__class__ is Existing
print('I4.bound-origin', True)
print('I4.complete', True)
