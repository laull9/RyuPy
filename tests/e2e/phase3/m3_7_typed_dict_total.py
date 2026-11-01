# 首切只使用total规则；Required/NotRequired完整矩阵保留另一pending，不削验收。
from typing import TypedDict, Optional, Tuple, is_typeddict, get_args
class Base(TypedDict):
    identity: int
    shared: str
class Child(Base, total=False):
    shared: int
    values: Tuple[int, ...]
    nullable: Optional[int]
for cls in [Base, Child]:
    print(cls.__name__, cls.__module__, cls.__total__, type(cls).__name__)
    print(list(cls.__annotations__), sorted(cls.__required_keys__), sorted(cls.__optional_keys__))
    print(cls.__bases__ == (dict,), is_typeddict(cls), issubclass(cls, dict))
    value = cls(identity='not checked', extra=7)
    print(type(value) is dict, cls(), value)
    try:
        isinstance({}, cls)
    except TypeError:
        print('no instance checks')
    try:
        issubclass(dict, cls)
    except TypeError:
        print('no subclass checks')
print(Child.__annotations__['shared'] is int)
print(get_args(Child.__annotations__['values'])[1] is Ellipsis)
Functional = TypedDict('Functional', {'field': int}, total=False)
print(Functional.__name__, sorted(Functional.__optional_keys__), Functional(field='untyped'))
# 根factory与产物都保留真实元数据；注解表可改，key快照不随之重算。
print(TypedDict.__module__, TypedDict.__qualname__, callable(TypedDict))
class Documented(TypedDict):
    "字段文档"
    field: int
print(Documented.__doc__, Base.__doc__ is None)
Documented.__annotations__['later'] = str
print(list(Documented.__annotations__), sorted(Documented.__required_keys__))
# 装饰器失败只能保留旧公开绑定；基类值先求值，字段注解之后求值。
events = []
def annotation():
    events.append('annotation')
    return int
def fail(cls):
    events.append('decorate')
    raise ValueError('stop')
Saved = Documented
try:
    @fail
    class Saved(TypedDict):
        x: annotation()
except ValueError:
    print(Saved is Documented, events)
