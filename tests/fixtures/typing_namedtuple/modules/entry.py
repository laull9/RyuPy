# 尚未执行；所有typing模块改绑finally恢复。
import typing
import provider
from typing import NamedTuple as Root

Alias = Root
class Direct(typing.NamedTuple):
    x: int
class Imported(Root):
    x: int
class Assigned(Alias):
    x: int
class Cross(provider.Root):
    x: int
assert Direct(1) == Imported(1) == Assigned(1) == Cross(1) == (1,)
assert Direct.__orig_bases__ == Imported.__orig_bases__ == Assigned.__orig_bases__ == Cross.__orig_bases__ == (Root,)
print('N4.aliases', Direct._fields, Cross._fields, Direct.__orig_bases__ == (Root,))

class Ordinary:
    pass
Shadow = Root
Shadow = Ordinary
ordinary_module = "ordinary_override"
ordinary_qualname = "OrdinaryQualified"
class Normal(Shadow):
    initial_module = __module__
    initial_qualname = __qualname__
    __module__ = ordinary_module
    __qualname__ = ordinary_qualname
    if True:
        constant = 9
    def __init__(self, value):
        self.value = value
assert Normal(7).value == 7 and Normal.constant == 9
assert isinstance(Normal(8), Ordinary)
assert Normal.initial_module == __name__
assert Normal.initial_qualname == "Normal"
assert Normal.__module__ is ordinary_module
assert Normal.__qualname__ is ordinary_qualname
print('N4.ordinary', Normal(7).value, Normal.constant,
      Normal.initial_module, Normal.initial_qualname,
      Normal.__module__, Normal.__qualname__,
      Normal.__module__ is ordinary_module, Normal.__qualname__ is ordinary_qualname)

trace = []
try:
    typing.NamedTuple = Ordinary
    Rebound = Root('Rebound', [('x', int)])
    assert Rebound.__orig_bases__ == (Ordinary,)
    try:
        class OldAlias(Root):
            trace.append('must-not-run')
    except AssertionError as error:
        assert str(error) == '' and trace == []
        print('N4.rebound-root', Rebound.__orig_bases__[0] is Ordinary, str(error), trace)
    else:
        raise AssertionError('old alias class unexpectedly succeeded')
    class Current(typing.NamedTuple):
        def __init__(self, value):
            self.value = value
    assert Current(6).value == 6
finally:
    typing.NamedTuple = Root
class Restored(Root):
    x: int
assert Restored(3) == (3,)
print('N4.restored', Restored._fields)

Existing = 'old'
class Existing(Root):
    x: str = Existing
    trace.append(Existing)
assert trace == ['old'] and Existing().x == 'old'
print('N4.definition-binding', trace, Existing().x)

BodyFailure = 'old-body'
def body_error():
    trace.append(BodyFailure)
    raise ValueError('body-failure')
try:
    class BodyFailure(Root):
        x: int
        body_error()
except ValueError as error:
    assert str(error) == 'body-failure' and BodyFailure == 'old-body'
    print('N4.body-error', str(error), BodyFailure)
else:
    raise AssertionError('body did not fail')
DefaultFailure = 'old-default'
try:
    class DefaultFailure(Root):
        x: int = 1
        y: int
except TypeError as error:
    assert DefaultFailure == 'old-default'
    print('N4.default-error', str(error), DefaultFailure)
else:
    raise AssertionError('defaults did not fail')
FinishFailure = 'old-finish'
try:
    class FinishFailure(Root):
        x: int
        _asdict = 4
except AttributeError as error:
    assert FinishFailure == 'old-finish'
    print('N4.finish-error', str(error), FinishFailure)
else:
    raise AssertionError('finish did not fail')

seen = []
Decorated = 'old-decorator'
def rejecting(cls):
    seen.append(cls)
    trace.append(Decorated)
    assert cls(2) == (2,)
    raise RuntimeError('decorator-failure')
try:
    @rejecting
    class Decorated(Root):
        x: int
except RuntimeError as error:
    assert str(error) == 'decorator-failure' and Decorated == 'old-decorator'
    assert seen[0](4) == (4,)
    print('N4.decorator-error', str(error), Decorated, seen[0](4))
else:
    raise AssertionError('decorator did not fail')
Replaced = 'old-replacement'
def replacing(cls):
    assert Replaced == 'old-replacement'
    assert cls(5) == (5,)
    return 'replacement'
@replacing
class Replaced(Root):
    x: int
assert Replaced == 'replacement'
assert trace == ['old', 'old-body', 'old-decorator']
print('N4.decorator-replace', Replaced, trace)

provider.make.__module__ = 'changed_function_origin'
Made = provider.make()
assert Made.__module__ == 'changed_function_origin'
assert provider.ClassOrigin.__module__ == 'late_class_origin'
assert provider.ClassOrigin.__qualname__ == 'ClassOrigin'
print('N4.origins', Made.__module__, provider.ClassOrigin.__module__, provider.ClassOrigin.__qualname__)
provider.make.__module__ = ''
assert provider.make().__module__ == '__main__'
provider.make.__module__ = None
assert provider.make().__module__ == '__main__'
print('N4.falsey-origin', provider.make().__module__)
print('N4.complete', True)
