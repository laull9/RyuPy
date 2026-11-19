# F0反射分类与真正立即求值的注解；不声明Any完整元类协议。
import typing as t
assert type(t.Union) is type(t.Optional)
assert type(t.Literal) is not type(t.Union)
for value, name in [(t.Union, '_SpecialForm'), (t.Optional, '_SpecialForm'), (t.Literal, '_LiteralSpecialForm'), (t.List, '_SpecialGenericAlias'), (t.Any, '_AnyMeta')]:
    assert type(value).__name__ == name
    assert type(value).__module__ == 'typing'
    assert value.__class__ is type(value)
    print('type', repr(type(value)))
T = t.TypeVar('T')
for alias in [t.Union[T, int], t.Optional[T], t.List[T], t.Literal[1], list[T], t.Generic[T]]:
    assert alias.__args__ is alias.__args__
    assert t.get_args(alias) is alias.__args__
    assert alias.__parameters__ is alias.__parameters__
assert type(t.Generic[T]) is type(t.List[T])
assert type(t.Generic[T]) is not type(t.Union[T, int])
log = []
def argument():
    log.append('evaluated')
    return int
class Record:
    value: t.Optional[argument()] = None
assert log == ['evaluated']
assert Record.__annotations__['value'] is t.Optional[int]
record = Record()
record.value = 7
assert record.value == 7
print('metadata-eager', True)
