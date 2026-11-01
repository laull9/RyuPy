# ryupy: pending
# TypedDict是真实类型元数据，构造值仍是普通dict，不做键必填或值类型强制检查。
from typing import TypedDict, Required, NotRequired, Optional, Tuple, is_typeddict, get_args, get_origin

class Base(TypedDict):
    identity: int
    shared: str
    optional: NotRequired[int]

class Child(Base, total=False):
    shared: int
    values: Tuple[int, ...]
    required: Required[str]
    nullable: Optional[int]

for record in [Base, Child]:
    print(record.__name__, record.__module__, record.__total__)
    print(list(record.__annotations__), sorted(record.__required_keys__), sorted(record.__optional_keys__))
    print(type(record).__name__, record.__bases__ == (dict,), is_typeddict(record))
    print(get_origin(record) is None, get_args(record))
    empty = record()
    value = record({'identity': 'not checked'}, extra=7)
    print(type(empty) is dict, empty, value)
    print(issubclass(record, dict))
    try:
        print(isinstance({}, record))
    except TypeError:
        print('no instance checks')
    try:
        print(issubclass(dict, record))
    except TypeError:
        print('no subclass checks')
print(Child.__annotations__['shared'] is int)
print(Child.__annotations__['nullable'] == Optional[int])
print(is_typeddict(dict), type(TypedDict).__name__)
# 函数式创建复用同一元数据/字典构造协议，而不是空的占位函数。
Functional = TypedDict('Functional', {'key': int}, total=False)
print(Functional.__name__, Functional.__total__, sorted(Functional.__optional_keys__))
print(Functional(key='also not checked'))
# 注解字典可变；创建时算好的required/optional快照不随之后字典增删重算。
Base.__annotations__['later'] = str
print(list(Base.__annotations__), sorted(Base.__required_keys__))
